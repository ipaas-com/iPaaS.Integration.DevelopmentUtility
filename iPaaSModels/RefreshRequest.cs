using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace IntegrationDevelopmentUtility.iPaaSModels
{
    public class RefreshRequest
    {
        [JsonProperty("access_token", Order = 10)]
        public string AccessToken { get; set; }

        [JsonProperty("refresh_token", Order = 15)]
        public string RefreshToken { get; set; }
    }
}
