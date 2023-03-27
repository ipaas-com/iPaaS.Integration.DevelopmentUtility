using IntegrationDevelopmentUtility.iPaaSModels;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace IntegrationDevelopmentUtility.Utilities
{
    public class Settings
    {
        //Use the singleton patter to provide static access to the settings
        public static Settings Instance;

        [JsonProperty("hook_url")]
        public string HookUrl;
        [JsonProperty("integrator_url")]
        public string IntegratorUrl;
        [JsonProperty("logger_url")]
        public string LoggerUrl;
        [JsonProperty("product_url")]
        public string ProductUrl;
        [JsonProperty("giftcard_url")]
        public string GiftCardUrl;
        [JsonProperty("customer_url")]
        public string CustomerURL;
        [JsonProperty("transaction_url")]
        public string TransactionUrl;
        [JsonProperty("sso_url")]
        public string SSOUrl;
        [JsonProperty("subscription_url")]
        public string SubscriptionUrl;

        //If you are logging with a user that has access to multiple companies, the login process can be slow. This allows you to specify a single company to use, to speed up the login process.
        [JsonProperty("company_id")]
        public string CompanyId;

        [JsonProperty("azurefileshare_connection_string")]
        public string AzureFileShareConnectionString;

        [JsonProperty("system_settings")]
        public Dictionary<string, string> SystemSettings;

        //Optional parameters
        [JsonProperty("username")]
        public string Username;
        [JsonProperty("password")]
        public string Password;
        [JsonProperty("integration_file_location")]
        public string IntegrationFileLocation;
        [JsonProperty("integration_file_integration_id")]
        public long? IntegrationFileIntegrationId;


        [JsonProperty("hook_read_interval_ms")]
        public int HookReadIntervalMS = 5001;
        [JsonProperty("file_upload_delay_interval_secs")]
        public int FileUploadDelayIntervalSecs = 30;

        //Fields retrieved durng the login process
        public FullToken DefaultFullToken;

        public List<CompanyInfoResponse> AdminCompanies = new List<CompanyInfoResponse>();

        public List<CompanyInfoResponse> Companies = new List<CompanyInfoResponse>();

        public List<CompanyInfoResponse> IntegratorCompanies = new List<CompanyInfoResponse>();

        public List<SubscriptionResponse> Systems = new List<SubscriptionResponse>();

        public List<SubscriptionResponse> IntegratorSystems = new List<SubscriptionResponse>();
    }
}
