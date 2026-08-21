using Newtonsoft.Json;
using System.Collections.Generic;

namespace IntegrationDevelopmentUtility.iPaaSModels
{
    /// <summary>
    /// A trimmed view of the integrator API's integration response. We only map the fields we need in
    /// order to describe the available versions of an integration to the user.
    /// </summary>
    public class IntegrationResponse
    {
        [JsonProperty("id", Order = 1)]
        public string Id { get; set; }

        [JsonProperty("name", Order = 5)]
        public string Name { get; set; }

        [JsonProperty("versions", Order = 10)]
        public List<VersionResponse> Versions { get; set; }
    }
}
