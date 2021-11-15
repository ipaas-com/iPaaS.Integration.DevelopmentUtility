using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace IntegrationDevelopmentUtility.iPaaSModels
{
    public class SystemTypeVersionResponse
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

        [JsonProperty("status", Order = 15)]
        public string Status { get; set; }

        [JsonProperty("release_notes", Order = 15)]
        public string ReleaseNotes { get; set; }

        [JsonProperty("version_major", Order = 20)]
        public int VersionMajor { get; set; }

        [JsonProperty("version_minor", Order = 20)]
        public int VersionMinor { get; set; }
        #endregion
    }
}
