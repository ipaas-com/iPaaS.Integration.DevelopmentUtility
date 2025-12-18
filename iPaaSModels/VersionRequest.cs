using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace IntegrationDevelopmentUtility.iPaaSModels
{
    public class VersionRequest
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

        [JsonProperty("parent_id", Order = 15)]
        public long? ParentId{ get; set; }

        [JsonProperty("release_notes", Order = 15)]
        public string ReleaseNotes { get; set; }

        [JsonProperty("version_major", Order = 20)]
        public int VersionMajor { get; set; }

        [JsonProperty("version_minor", Order = 20)]
        public int VersionMinor { get; set; }

        [JsonProperty("version_patch", Order = 31)]
        public int VersionPatch { get; set; }

        [JsonProperty("oauth_url_template", Order = 35)]
        public string OAuthUrlTemplate { get; set; }

        [JsonProperty("oauth_identifier_field", Order = 40)]
        public string OAuthIdentifierField { get; set; }

        [JsonProperty("oauth_success_callback_field", Order = 42)]
        public string OAuthSuccessCallbackField { get; set; }

        [JsonProperty("custom_fields", Order = 45)]
        public Dictionary<string, string> CustomFields { get; set; }
        #endregion
    }
}
