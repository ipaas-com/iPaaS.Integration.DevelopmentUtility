using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntegrationDevelopmentUtility.iPaaSModels
{
    public class FieldResponse
    {
        [JsonProperty("id")]
        public long Id { get; set; }
        [JsonProperty("table_id")]
        public long TableId { get; set; }
        [JsonProperty("name")]
        public string Name { get; set; }
        [JsonProperty("description")]
        public string Description { get; set; }
        [JsonProperty("type")]
        public string Type { get; set; }
        public bool Required { get; set; }
        [JsonProperty("example")]
        public string Example { get; set; }
        [JsonProperty("field_values")]
        public List<FieldValueResponse> FieldValues { get; set; }
    }
}
