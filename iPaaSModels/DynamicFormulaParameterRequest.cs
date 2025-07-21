using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntegrationDevelopmentUtility.iPaaSModels
{
    public class DynamicFormulaParameterRequest
    {
        [Required]
        [JsonProperty("name", Order = 10)]
        public string Name { get; set; }

        [JsonProperty("description", Order = 15)]
        public string Description { get; set; }

        [Required]
        [JsonProperty("data_type", Order = 20)]
        public string DataType { get; set; }

        [JsonProperty("format", Order = 25)]
        public string Format { get; set; }

        [JsonProperty("min_length", Order = 30)]
        public int? MinLength { get; set; }

        [JsonProperty("max_length", Order = 35)]
        public int? MaxLength { get; set; }
    }
}
