using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntegrationDevelopmentUtility.iPaaSModels
{
    public class DynamicFormulaRequest
    {
        [JsonProperty("tracking_guid", Order = 5)]
        public Guid? TrackingGuid { get; set; }

        [JsonProperty("name", Order = 10)]
        public string Name { get; set; }

        [JsonProperty("description", Order = 15)]
        public string Description { get; set; }

        [JsonProperty("formula", Order = 20)]
        public string Formula { get; set; }

        [JsonProperty("example", Order = 22)]
        public string Example { get; set; }

        [JsonProperty("is_async", Order = 23)]
        public bool? IsAsync { get; set; }

        //Represents the value from TM_DynamicFormulaStatus (NONE=0, ACTIVE=1,DEPRECATED=2,REMOVED=3)
        [JsonProperty("status", Order = 24)]
        public int? Status { get; set; }

        [JsonProperty("integration_version_id", Order = 25)]
        public string SystemTypeVersionId { get; set; }

        [JsonProperty("return_parameter", Order = 30)]
        public ReturnParameterRequest ReturnParameter { get; set; }

        [JsonProperty("parameters", Order = 35)]
        public List<DynamicFormulaParameterRequest> Parameters { get; set; }
    }
}
