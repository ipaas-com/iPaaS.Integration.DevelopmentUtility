using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace IntegrationDevelopmentUtility.iPaaSModels
{
    public class TopicSubscriptionRequest
    {
        #region Properties
        [JsonProperty("topic_name", Order = 10)]
        public string TopicName { get; set; }
        #endregion
    }
}
