using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace IntegrationDevelopmentUtility.iPaaSModels
{
    public class CompanyInfoResponse
    {
        [JsonProperty("id", Order = 1)]
        public Guid Id { get; set; }

        [JsonProperty("name", Order = 10)]
        public string Name { get; set; }

        [JsonProperty("logo", Order = 15)]
        public string Logo { get; set; }

        [JsonProperty("address", Order = 20)]
        public string Address { get; set; }

        [JsonProperty("designation", Order = 25)]
        public string Designation { get; set; }

        [JsonProperty("isMISP", Order = 30)]
        public bool IsMISP { get; set; }

        [JsonProperty("userType", Order = 35)]
        public int? User_Type { get; set; }


        //The following properties are not part of the response, but we save them as we process each company
        public FullToken CompanySpecificFullToken;

        public long iPaaSSystemId { get; set; }

        public List<SettingGetAllResponse> Systems;

    }
}
