using Confluent.Kafka;
using IntegrationDevelopmentUtility.iPaaSModels;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace IntegrationDevelopmentUtility.Utilities
{
    /// <summary>
    /// This class will handle listening to a given Kafka topic (with the topic name matching the tracking guid)
    /// </summary>
    public class KafkaConsumer
    {
        #region Properties
        private ClientConfig _config;
        private TopicSubscriptionResponse _topicSubscriptionResponse;
        #endregion

        #region Constructors
        public KafkaConsumer(TopicSubscriptionResponse topicSubscriptionResponse)
        {
            if (topicSubscriptionResponse == null)
                throw new ArgumentNullException("topicSubscriptionResponse", "topicSubscriptionResponse cannot be null. This may indicate a problem with the integration API");

            //login info will be in the format topic:username:password and will be base64 encoded. We must decrypt that.
            string authdata, saslusername, saslpassword;
            authdata = topicSubscriptionResponse.ListenerAuthentication;
            authdata = Encoding.UTF8.GetString(Convert.FromBase64String(authdata));
            authdata = authdata.Substring(authdata.IndexOf(":") + 1); //Remove the topic name
            saslusername = authdata.Substring(0, authdata.IndexOf(":"));
            saslpassword = authdata.Substring(authdata.IndexOf(":") + 1);

            _topicSubscriptionResponse = topicSubscriptionResponse;

            _config = new ClientConfig();
            _config.BootstrapServers = topicSubscriptionResponse.ListenerBoostrapServer;
            _config.BrokerAddressFamily = BrokerAddressFamily.V4;
            _config.SecurityProtocol = SecurityProtocol.SaslPlaintext;
            _config.SaslMechanism = SaslMechanism.Plain;

            _config.SaslUsername = saslusername; // "integration";
            _config.SaslPassword = saslpassword; //"";
        }
        #endregion

        /// <summary>
        /// Begin consuming the topic. It is expected that this method will run forever until the kill command is reached. It is the responsibility of the caller to
        /// initiante any other cancel (e.g. time out, or user input) via the supplied CancellationTokenSource)
        /// </summary>
        /// <param name="cts"></param>
        public void ConsumeForever(CancellationTokenSource cts, ref HookController.HookStatus hookStatus)
        {
            var consumerConfig = new ConsumerConfig(_config);
            consumerConfig.GroupId = Guid.NewGuid().ToString(); //Give this consumer a unique group id so that it will get every entry in the topic
            consumerConfig.AutoOffsetReset = AutoOffsetReset.Earliest;

            using (var consumer = new ConsumerBuilder<string, string>(consumerConfig)
                .SetLogHandler((producer, logMessage) => { HandleKafkaLogMessage(logMessage);})
                .SetErrorHandler((producer, error) => { HandleKafkaErrorMessage(error); })
                .Build())
            {
                consumer.Subscribe(_topicSubscriptionResponse.TopicName);
                var totalCount = 0;
                try
                {
                    while (true)
                    {
                        var cr = consumer.Consume(cts.Token);
                        totalCount += 1;

                        var detail = JsonConvert.DeserializeObject<iPaaSModels.DetailResponse>(cr.Message.Value);

                        bool offrampKeyFound = false;
                        detail.PrintToConsole();
                        if (detail.Details.Contains($"All Processing Complete")) //Indicates the end of a normal transaction
                            offrampKeyFound = true;
                        else if (detail.Details.Contains($"Reporting hook complete {_topicSubscriptionResponse.TopicName}")) //Indicates the end of a normal transaction (this is the older version)
                            offrampKeyFound = true;
                        else if (detail.Details.Contains("Completed loading meta data"))//Indicates the end of a metadata transaction
                            offrampKeyFound = true;

                        //If we found the magical phrase that tells us the transfer is complete, then exit
                        if (offrampKeyFound)
                        {
                            hookStatus = HookController.HookStatus.Completed;
                            break;
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    // Ctrl-C was pressed. Set the status if it hasn't been set already
                    if(hookStatus == HookController.HookStatus.InProgress)
                        hookStatus = HookController.HookStatus.Cancelled_TimeOut;
                }
                finally
                {
                    consumer.Close();
                    cts.Cancel();
                }
            }
        }

        private void HandleKafkaLogMessage(LogMessage logMessage)
        {
            //Because of the way Kafka works, when you want to get messages, you send a message two the main ip and it replies with 2 ips you can listen
            //to: one external and one internal. The integration tool tries each one until it finds one that works. If it happens to try the incorrect one
            //first, you see one log message and one error message indicating that it can't connect. This is confusing to users, who think it means it could
            //not connect at all. Especially if the hook processes slowly, users may think the service is the problem
            if (logMessage.Message.Contains("Connect to ipv4#") && logMessage.Message.Contains("failed: Unknown error"))
                return;

            StandardUtilities.WriteToConsole("KafkaConsumer:" + logMessage.Message, StandardUtilities.Severity.VERBOSE);
        }

        private void HandleKafkaErrorMessage(Error error)
        {
            //Because of the way Kafka works, when you want to get messages, you send a message two the main ip and it replies with 2 ips you can listen
            //to: one external and one internal. The integration tool tries each one until it finds one that works. If it happens to try the incorrect one
            //first, you see one log message and one error message indicating that it can't connect. This is confusing to users, who think it means it could
            //not connect at all. Especially if the hook processes slowly, users may think the service is the problem
            if (error.Reason.Contains("Connect to ipv4#") && error.Reason.Contains("failed: Unknown error"))
                return;

            StandardUtilities.WriteToConsole("KafkaConsumer:" + error.Reason, StandardUtilities.Severity.ERROR);
        }
    }
}
