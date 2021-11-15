using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace IntegrationDevelopmentUtility.iPaaSModels
{
    public class UserCompanyResponse
    {
        #region Properties
        [JsonProperty("admin_companies", Order = 15)]
        public List<CompanyInfoResponse> AdminCompanies { get; set; }


        [JsonProperty("companies", Order = 15)]
        public List<CompanyInfoResponse> Companies { get; set; }
        #endregion
    }
}
