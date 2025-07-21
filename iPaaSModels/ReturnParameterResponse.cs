using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntegrationDevelopmentUtility.iPaaSModels
{
    public class ReturnParameterResponse
    {
        [JsonProperty("id", Order = 10)]
        public string Id { get; set; }

        [JsonProperty("description", Order = 15)]
        public string Description { get; set; }

        [JsonProperty("data_type", Order = 20)]
        public string DataType { get; set; }
    }
}
