using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace IntegrationDevelopmentUtility.Utilities
{
    /// <summary>
    /// Handle hook commands
    /// </summary>
    public class HookController
    {
        public enum HookStatus
        {
            InProgress,
            Completed,
            Cancelled_TimeOut,
            Cancelled_UserInput
        }

        /// <summary>
        /// Send the specified hook to R2 and listen to Kafka messages for a response
        /// </summary>
        /// <param name="externalSystemId"></param>
        /// <param name="scope"></param>
        /// <param name="externalId"></param>
        /// <param name="direction"></param>
        public static void SendHookAndListenForLogData(long externalSystemId, string scope, string externalId, string direction)
        {
            var trackingGuid = Guid.NewGuid();

            var companyToken = StandardUtilities.ApiTokenForSystem(externalSystemId);

            if (companyToken == null)
                throw new Exception($"Unable to find token for system {externalSystemId}. Ensure it is a system you have access to");

            //Create topic:
            iPaaSModels.TopicSubscriptionResponse topicSubscriptionResponse = null;
            try
            {
                topicSubscriptionResponse = iPaaSCallWrapper.TopicSubscriptionCreate(new iPaaSModels.TopicSubscriptionRequest() { TopicName = trackingGuid.ToString() }, companyToken);
            }
            catch (Exception ex)
            {
                StandardUtilities.WriteToConsole($"An error occurred while subscribing to the log output. The hook will be sent, but logging information will not appear. Error: {ex.Message}", StandardUtilities.Severity.LOCAL_ERROR);
            }

            var success = SendHook(trackingGuid, externalSystemId, scope, externalId, direction);
            //If the sendHook call was not completed, it should return false.
            if (!success)
                return;

            if (topicSubscriptionResponse != null)
            {
                var kafkaConsumer = new KafkaConsumer(topicSubscriptionResponse);
                CancellationTokenSource cts = new CancellationTokenSource();

                HookStatus status = HookStatus.InProgress;

                var t = new System.Threading.Thread(() => kafkaConsumer.ConsumeForever(cts, ref status));
                t.Start();

                cts.CancelAfter(1000 * 60 * 10); //Cancel the consumer after 10 minutes

                ConsoleKeyInfo? cki;
                do
                {
                    cki = StandardUtilities.ReadKey();
                    // do something with each key press until escape key is pressed
                    if (cki.HasValue && cki.Value.Key == ConsoleKey.Escape)
                    {
                        //Console.WriteLine("No longer listeing for escape");
                        status = HookStatus.Cancelled_UserInput;
                        cts.Cancel();
                        continue;
                    }
                    //Console.WriteLine("Listening for escape");
                } while (!cts.IsCancellationRequested);

                //Now that we are done, delete the subscription
                iPaaSCallWrapper.TopicSubscriptionDelete(trackingGuid.ToString(), companyToken);

                Program.OperationCompleted = true;
                if (status == HookStatus.Cancelled_TimeOut)
                    StandardUtilities.WriteToConsole($"Log listener timed out. This will not stop further processing of the hook, but no more logs will appear here.", StandardUtilities.Severity.LOCAL);
                else if (status == HookStatus.Cancelled_UserInput)
                    StandardUtilities.WriteToConsole($"Log listener was cancelled. This will not stop further processing of the hook, but no more logs will appear here.", StandardUtilities.Severity.LOCAL);
                else
                    StandardUtilities.WriteToConsole($"Transfer is complete", StandardUtilities.Severity.LOCAL);
            }
            else
            {
                //If we did not create a kafka subscription, just send the hook and return control
                Program.OperationCompleted = true;
                StandardUtilities.WriteToConsole($"Hook has been sent, but no log data will be available", StandardUtilities.Severity.LOCAL);
            }

            #region removed code
            //DateTimeOffset? cutoffDTO = DateTimeOffset.MinValue;
            //DateTimeOffset maxDateFound = DateTimeOffset.MinValue;

            //bool offrampKeyFound = false;

            //while (true)
            //{
            //    if(Program.OperationCancelled)
            //    {
            //        StandardUtilities.WriteToConsole($"Listener cancelled", StandardUtilities.Severity.LOCAL);
            //        return;
            //    }

            //    var response = iPaaSCallWrapper.RetrieveLogDetail(trackingGuid, cutoffDTO, companyToken);

            //    //StandardUtilities.WriteToConsole($"Finished search with DTO {cutoffDTO.Value.ToString("yyyy/MM/dd HH:mm:ss.fff zzz")}", StandardUtilities.Severity.LOCAL);
            //    if (response != null && response.Count > 0)
            //    {
            //        //Sort the list by act
            //        response.Sort((x, y) => x.ActivityTimestamp.CompareTo(y.ActivityTimestamp));

            //        foreach (var detail in response)
            //        {
            //            detail.PrintToConsole();
            //            if (detail.ActivityTimestamp > maxDateFound)
            //                maxDateFound = detail.ActivityTimestamp;
            //            if (detail.Details.Contains($"Reporting hook complete {trackingGuid}")) //Indicates the end of a normal transaction
            //                offrampKeyFound = true;
            //            else if(detail.Details.Contains("Completed loading meta data"))//Indicates the end of a metadata transaction
            //                offrampKeyFound = true;
            //        }
            //        cutoffDTO = maxDateFound;
            //        //StandardUtilities.WriteToConsole($"Setting search to {startReqDTO.ToString("yyyy/MM/dd HH:mm:ss.fff zzz")}", StandardUtilities.Severity.LOCAL);
            //    }
            //    //else
            //    //    StandardUtilities.WriteToConsole($"No results found", StandardUtilities.Severity.LOCAL);

            //    //If we found the magical phrase that tells us the transfer is complete, then exit
            //    if (offrampKeyFound)
            //        break;

            //    //If we go one minute without recieving any updated log data, exit.
            //    if (cutoffDTO.HasValue && cutoffDTO.Value != DateTimeOffset.MinValue && cutoffDTO.Value.AddMinutes(5) < DateTimeOffset.Now)
            //    {
            //        StandardUtilities.WriteToConsole($"No new log data found for 5 minutes. Transfer is likely complete. Aborting log listener.", StandardUtilities.Severity.LOCAL);
            //        break;
            //    }

            //    //If we have gone 5 minutes since launch, and there is no log data, we give up
            //    if (!cutoffDTO.HasValue && maxDateFound.AddMinutes(5) < DateTimeOffset.Now)
            //    {
            //        StandardUtilities.WriteToConsole($"No log data found after 5 mintues of waiting. Aborting log listener.", StandardUtilities.Severity.LOCAL);
            //        break;
            //    }

            //    Thread.Sleep(Settings.Instance.HookReadIntervalMS); //Sleep 5 seconds
            //}
            #endregion
        }

        /// <summary>
        /// Send the requested hook to R2
        /// </summary>
        /// <param name="trackingGuid"></param>
        /// <param name="externalSystemId"></param>
        /// <param name="scope"></param>
        /// <param name="externalId"></param>
        /// <param name="direction"></param>
        /// <returns></returns>
        public static bool SendHook(Guid trackingGuid, long externalSystemId, string scope, string externalId, string direction)
        {
            iPaaSModels.WebhookRequest webhookRequest = new iPaaSModels.WebhookRequest();
            webhookRequest.Notifications = new System.Collections.Generic.List<iPaaSModels.Notification>();

            //if (!scope.EndsWith("/debug"))
            //    scope += "/debug";

            var notification = new iPaaSModels.Notification();
            notification.Destination = Convert.ToString(externalSystemId);
            notification.TrackingGuid = trackingGuid;
            notification.Scope = scope;
            if (direction == "FROM")
                notification.Id = externalId;
            else
                notification.ExternalId = externalId;

            webhookRequest.Notifications.Add(notification);

            var hookToken = StandardUtilities.HookTokenForSystem(externalSystemId);

            if (string.IsNullOrEmpty(hookToken))
            {
                StandardUtilities.WriteToConsole($"Unable to retrieve token for system {externalSystemId}. Hook will not be sent. Ensure this is a system your account has access to.", StandardUtilities.Severity.LOCAL_ERROR);
                Program.OperationCancelled = true;
                Program.OperationCompleted = true;
                return false;
            }

            StandardUtilities.WriteToConsole($"Sending hook: ExternalSystemId: {externalSystemId}, Scope: {scope}, ExternalId: {externalId}, Direction: {direction}, TrackingId: {trackingGuid}", StandardUtilities.Severity.LOCAL);

            iPaaSCallWrapper.SendHook(webhookRequest, direction, hookToken);

            StandardUtilities.WriteToConsole($"Send complete. Log output will appear below", StandardUtilities.Severity.LOCAL);

            return true;
        }
    }
}
