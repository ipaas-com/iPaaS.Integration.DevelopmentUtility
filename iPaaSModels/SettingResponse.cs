using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace IntegrationDevelopmentUtility.iPaaSModels
{
    public class SettingResponse
    {
        #region Properties
        [JsonProperty("id", Order = 10)]
        public long Id { get; set; }

        [JsonProperty("system_type_id", Order = 15)]
        public int SystemTypeId { get; set; }

        [JsonProperty("system_type_version_id", Order = 16)]
        public string SystemTypeVersionId { get; set; }

        [JsonProperty("name", Order = 20)]
        public string Name { get; set; }

        [JsonProperty("webhook_api_key", Order = 25)]
        public string WebhookApiKey { get; set; }

        [JsonProperty("tracking_guid", Order = 30)]
        public Guid? TrackingGuid { get; set; }

        [JsonProperty("settings", Order = 35)]
        public Dictionary<string, string> Settings { get; set; }

        [JsonProperty("persistent_data", Order = 40)]
        public List<PersistentDataResponse> PersistentData { get; set; }
        #endregion

        #region Constructor(s)
        public SettingResponse() { Settings = new Dictionary<string, string>(); }
        #endregion
    }
}
