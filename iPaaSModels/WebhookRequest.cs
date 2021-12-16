using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace IntegrationDevelopmentUtility.iPaaSModels
{
    public class WebhookRequest
    {
        [JsonProperty("id", Order = 10)]
        public string Id { get; set; }

        [JsonProperty("attempt", Order = 15)]
        public long Attempt { get; set; }

        [JsonProperty("notifications", Order = 20)]
        public List<Notification> Notifications { get; set; }
    }

    public class Notification
    {
        [JsonProperty("action", Order = 10)]
        public string Action { get; set; }

        [JsonProperty("id", Order = 15)]
        public string Id { get; set; }

        [JsonProperty("external_id", Order = 20)]
        public string ExternalId { get; set; }

        //[JsonProperty("companyId")]
        //public Guid CompanyId { get; set; }

        [JsonProperty("destination", Order = 25)]
        public string Destination { get; set; }

        [JsonProperty("scope", Order = 30)]
        public string Scope { get; set; }

        [JsonProperty("type", Order = 35)]
        public string Type { get; set; }

        [JsonProperty("tracking_guid", Order = 40)]
        public Guid TrackingGuid { get; set; }
    }
}
