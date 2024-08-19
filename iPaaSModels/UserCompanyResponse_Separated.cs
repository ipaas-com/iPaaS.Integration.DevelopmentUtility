using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace IntegrationDevelopmentUtility.iPaaSModels
{
    public class UserCompanyResponse
    {
        #region Properties
        [JsonProperty("id", Order = 1)]
        public Guid Id { get; set; }

        [JsonProperty("name", Order = 10)]
        public string Name { get; set; }

        [JsonProperty("logo", Order = 15)]
        public string Logo { get; set; }

        [JsonProperty("designation_id", Order = 25)]
        public long DesignationId { get; set; }

        [JsonProperty("user_type_id", Order = 30)]
        public int? UserTypeId { get; set; }

        [JsonProperty("designations", Order = 35)]
        public Dictionary<long, string> Designations { get; set; }
        #endregion

        #region Constructor(s)
        public UserCompanyResponse()
        {
            Designations = new Dictionary<long, string>();
        }
        #endregion
    }

    public class UserCompanyResponse_Separated
    {
        #region Properties
        [JsonProperty("admin_companies", Order = 10)]
        public List<CompanyInfoResponse> AdminCompanies { get; set; }

        [JsonProperty("companies", Order = 15)]
        public List<CompanyInfoResponse> Companies { get; set; }

        [JsonProperty("misps", Order = 20)]
        public List<CompanyInfoResponse> MISPs { get; set; }

        [JsonProperty("tech_partners", Order = 25)]
        public List<CompanyInfoResponse> TechPartners { get; set; }

        [JsonProperty("integrators", Order = 30)]
        public List<CompanyInfoResponse> Integrators { get; set; }
        #endregion

        public UserCompanyResponse_Separated()
        {
            AdminCompanies = new List<CompanyInfoResponse>();
            Companies = new List<CompanyInfoResponse>();
            MISPs = new List<CompanyInfoResponse>();
            TechPartners = new List<CompanyInfoResponse>();
            Integrators = new List<CompanyInfoResponse>();
        }
    }
}
