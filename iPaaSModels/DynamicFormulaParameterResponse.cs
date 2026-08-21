using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntegrationDevelopmentUtility.iPaaSModels
{
    public class DynamicFormulaParameterResponse
    {
        [JsonProperty("id", Order = 5)]
        public string Id { get; set; }

        [JsonProperty("name", Order = 15)]
        public string Name { get; set; }

        [JsonProperty("description", Order = 20)]
        public string Description { get; set; }

        [JsonProperty("data_type", Order = 25)]
        public string DataType { get; set; }

        [JsonProperty("format", Order = 30)]
        public string Format { get; set; }

        [JsonProperty("min_length", Order = 35)]
        public int? MinLength { get; set; }

        [JsonProperty("max_length", Order = 40)]
        public int? MaxLength { get; set; }

        /// <summary>
        /// Indicates whether a value must be supplied for this parameter when the formula is called.
        /// Null means the value has not been determined for this formula yet.
        /// </summary>
        [JsonProperty("is_required", Order = 45)]
        public bool? IsRequired { get; set; }
    }
}
