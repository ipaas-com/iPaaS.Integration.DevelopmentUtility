using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace IntegrationDevelopmentUtility.iPaaSModels
{
    public class VersionCustomFieldRequest
    {
        //Note we use Key here, rather than Name, as this used to be represented by a dictionary rather than a list of objects
        [JsonProperty("key", Order = 5)]
        [JsonPropertyName("key"), JsonPropertyOrder(5)]
        public string Key { get; set; }

        [JsonProperty("value", Order = 10)]
        [JsonPropertyName("value"), JsonPropertyOrder(10)]
        public string Value { get; set; }

        [JsonProperty("available_in_api", Order = 15)]
        [JsonPropertyName("available_in_api"), JsonPropertyOrder(15)]
        public bool? AvailableInApi { get; set; }
    }
}
