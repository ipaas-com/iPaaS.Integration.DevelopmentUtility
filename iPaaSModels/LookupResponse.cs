using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntegrationDevelopmentUtility.iPaaSModels
{
    public class LookupResponse
    {
        #region Properties
        [JsonProperty("id", Order = 10)]
        public string Id { get; set; }

        [JsonProperty("name", Order = 15)]
        public string Name { get; set; }

        [JsonProperty("description", Order = 20)]
        public string Description { get; set; }

        [JsonProperty("mapping_collection_type_id", Order = 25)]
        public int? TM_MappingCollectionTypeId { get; set; }

        [JsonProperty("image_url", Order = 30)]
        public string ImageUrl { get; set; }
        #endregion
    }
}
