using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntegrationDevelopmentUtility.iPaaSModels
{
    public class ReturnParameterRequest
    {
        [JsonProperty("description", Order = 10)]
        public string Description { get; set; }

        [JsonProperty("data_type", Order = 15)]
        [Required]
        public string DataType { get; set; }
    }
}
