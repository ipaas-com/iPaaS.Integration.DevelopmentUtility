using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace IntegrationDevelopmentUtility.iPaaSModels
{
    public class HeaderResponse
    {
        #region Properties
        [JsonProperty("id", Order = 1)]
        public Guid TrackingGuid { get; set; }

        [JsonProperty("name", Order = 5)]
        public string Name { get; set; }

        [JsonProperty("direction", Order = 10)]
        public string Direction { get; set; }

        [JsonProperty("system_id", Order = 15)]
        public long SystemId { get; set; }

        [JsonProperty("mapping_collection_type", Order = 2)]
        public int MappingCollectionType { get; set; }

        [JsonProperty("internal_id", Order = 25)]
        public long InternalId { get; set; }

        [JsonProperty("external_id", Order = 30)]
        public string ExternalId { get; set; }

        [JsonProperty("start_timestamp", Order = 35)]
        public string StartTimestamp { get; set; }

        [JsonProperty("status", Order = 40)]
        public string Status { get; set; }

        [JsonProperty("details", Order = 45)]
        public List<DetailResponse> Details { get; set; }
        #endregion

        public bool PrintNewDetails(DateTimeOffset? cutoffDTO)
        {
            List<DetailResponse> printDetails = null;
            if (cutoffDTO.HasValue)
                printDetails = Details.FindAll(x => x.ActivityTimestamp > cutoffDTO.Value);
            else
                printDetails = Details;

            if (printDetails == null || printDetails.Count == 0)
                return false;

            foreach (var detail in Details)
                detail.PrintToConsole();

            return true;
        }


    }
}
