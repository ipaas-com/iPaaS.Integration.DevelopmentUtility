using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace IntegrationDevelopmentUtility.iPaaSModels
{
    public class SubscriptionGetAllResponse
    {
        #region Properties
        [JsonProperty("id", Order = 5)]
        public long Id { get; set; }

        [JsonProperty("type", Order = 10)]
        public long Type { get; set; }

        [JsonProperty("name", Order = 15)]
        public string Name { get; set; }

        [JsonProperty("description", Order = 20)]
        public string Description { get; set; }

        [JsonProperty("value", Order = 25)]
        public string Value { get; set; }

        [JsonProperty("attached_items_count", Order = 30)]
        public int AttachedItemsCount { get; set; }

        [JsonProperty("image_url", Order = 35)]
        public string ImageURL { get; set; }
        #endregion
    }
}
