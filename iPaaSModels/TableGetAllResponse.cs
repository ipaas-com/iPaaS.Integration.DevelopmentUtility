using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntegrationDevelopmentUtility.iPaaSModels
{
    public class TableGetAllResponse
    {
        [JsonProperty("id")]
        public long Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("integration_id")]
        public long IntegrationId { get; set; }

        [JsonProperty("integration_name")]
        public string IntegrationName { get; set; }

        [JsonProperty("field_count")]
        public long FieldCount { get; set; }
    }
}
