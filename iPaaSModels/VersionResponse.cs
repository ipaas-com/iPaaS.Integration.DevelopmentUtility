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
        #endregion
    }
}
