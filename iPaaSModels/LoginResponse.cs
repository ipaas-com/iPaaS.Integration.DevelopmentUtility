using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace IntegrationDevelopmentUtility.iPaaSModels
{
    public class LoginResponse
    {
        #region Properties
        [JsonProperty("id", Order = 5)]
        public long? Id { get; set; }

        [JsonProperty("name", Order = 10)]
        public string Name { get; set; }

        [JsonProperty("email_address", Order = 15)]
        public string EmailAddress { get; set; }

        [JsonProperty("access_token", Order = 20)]
        public string AccessToken { get; set; }

        [JsonProperty("access_token_expiration", Order = 25)]
        public DateTimeOffset AccessTokenExpiration { get; set; }

        [JsonProperty("refresh_token", Order = 30)]
        public string RefreshToken { get; set; }

        [JsonProperty("return_url", Order = 35)]
        public string ReturnUrl { get; set; }

        [JsonProperty("permissions", Order = 45)]
        public object Permissions { get; set; }

        [JsonProperty("tracking_guid", Order = 50)]
        public Guid? TrackingGuid { get; set; }
        #endregion
    }
}
