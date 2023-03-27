using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace IntegrationDevelopmentUtility.iPaaSModels
{
    public class PersistentDataResponse
    {
        [JsonProperty("name", Order = 10)]
        public string Name { get; set; }

        [JsonProperty("value", Order = 20)]
        public object Value { get; set; }

        [JsonProperty("expiration_date_time", Order = 30)]
        public DateTimeOffset? ExpirationDateTime { get; set; }
    }
}
