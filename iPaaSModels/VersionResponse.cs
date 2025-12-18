using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace IntegrationDevelopmentUtility.iPaaSModels
{
    public class VersionResponse
    {
        #region Properties
        [JsonProperty("id", Order = 1)]
        public string Id { get; set; }

        [JsonProperty("name", Order = 5)]
        public string Name { get; set; }

        [JsonProperty("dll_name", Order = 10)]
        public string DllName { get; set; }

        [JsonProperty("dll_namespace", Order = 15)]
        public string DllNamespace { get; set; }

        [JsonProperty("integration_version_status_id", Order = 20)]
        public int ST_SystemTypeVersionStatusId { get; set; }

        [JsonProperty("release_notes", Order = 25)]
        public string ReleaseNotes { get; set; }

        [JsonProperty("version_major", Order = 30)]
        public int VersionMajor { get; set; }

        [JsonProperty("version_minor", Order = 35)]
        public int VersionMinor { get; set; }

        [JsonProperty("version_patch", Order = 40)]
        public int VersionPatch { get; set; }

        [JsonProperty("oauth_url_template", Order = 45)]
        public string OAuthUrlTemplate { get; set; }

        [JsonProperty("oauth_identifier_field", Order = 50)]
        public string OAuthIdentifierField { get; set; }

        [JsonProperty("oauth_success_callback_field", Order = 55)]
        public string OAuthSuccessCallbackField { get; set; }

        [JsonProperty("custom_fields", Order = 60)]
        public Dictionary<string, string> CustomFields { get; set; }
        #endregion
    }
}
