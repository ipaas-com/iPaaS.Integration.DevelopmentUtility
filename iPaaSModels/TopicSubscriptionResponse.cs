using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace IntegrationDevelopmentUtility.iPaaSModels
{
    public class TopicSubscriptionResponse
    {
        #region Properties
        [JsonProperty("topic_name", Order = 10)]
        public string TopicName { get; set; }

        [JsonProperty("listener_bootstrap_server", Order = 20)]
        public string ListenerBoostrapServer { get; set; }

        [JsonProperty("listener_authentication", Order = 30)]
        public string ListenerAuthentication { get; set; }
        #endregion
    }
}
