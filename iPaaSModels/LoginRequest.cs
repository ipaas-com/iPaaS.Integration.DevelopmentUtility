using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace IntegrationDevelopmentUtility.iPaaSModels
{
    public class LoginRequest
    {
        #region Properties
        //[Required]
        [JsonProperty("email_address", Order = 10, Required = Required.Always)]
        public string EmailAddress { get; set; }

        //[Required]
        [JsonProperty("password", Order = 15, Required = Required.Always)]
        public string Password { get; set; }

        [JsonProperty("return_url", Order = 20)]
        public string ReturnUrl { get; set; }

        [JsonProperty("remember_me", Order = 25)]
        public bool RememberMe { get; set; }

        [JsonProperty("tracking_guid", Order = 30)]
        public Guid? TrackingGuid { get; set; }
        #endregion
    }
}
