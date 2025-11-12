using IntegrationDevelopmentUtility.iPaaSModels;
using Microsoft.Extensions.Configuration;
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
        [JsonProperty("employee_url")]
        public string EmployeeUrl;
        [JsonProperty("message_url")]
        public string MessageUrl;

        //If you are logging with a user that has access to multiple companies, the login process can be slow. This allows you to specify a single company to use, to speed up the login process.
        [JsonProperty("company_id")]
        public string CompanyId;

        //This was user for direct upload, which is no longer supported.
        //[JsonProperty("azurefileshare_connection_string")]
        //public string AzureFileShareConnectionString;

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

        //Fields retrieved during the login process
        public FullToken DefaultFullToken;

        public List<CompanyInfoResponse> AdminCompanies = new List<CompanyInfoResponse>();

        public List<CompanyInfoResponse> Companies = new List<CompanyInfoResponse>();

        public List<CompanyInfoResponse> IntegratorCompanies = new List<CompanyInfoResponse>();

        public List<CompanyInfoResponse> MISPCompanies = new List<CompanyInfoResponse>();

        public List<SubscriptionResponse> Systems = new List<SubscriptionResponse>();

        public List<SubscriptionResponse> IntegratorSystems = new List<SubscriptionResponse>();

        public void ConsumeSettingsFile(IConfigurationRoot config)
        {
            Username = config.GetValue<string>("username");
            Password = config.GetValue<string>("password");


            //Some settings are loaded automatically in the config.Get call in Program.cs, but fields with different names (e.g. hook_url instead of HookUrl)
            //must be manually reconciled.
            HookUrl = config.GetValue<string>("hook_url");
            IntegratorUrl = config.GetValue<string>("integrator_url");
            LoggerUrl = config.GetValue<string>("logger_url");
            ProductUrl = config.GetValue<string>("product_url");
            GiftCardUrl = config.GetValue<string>("giftcard_url");
            CustomerURL = config.GetValue<string>("customer_url");
            TransactionUrl = config.GetValue<string>("transaction_url");
            SSOUrl = config.GetValue<string>("sso_url");
            SubscriptionUrl = config.GetValue<string>("subscription_url");
            EmployeeUrl = config.GetValue<string>("employee_url");
            MessageUrl = config.GetValue<string>("message_url");
            CompanyId = config.GetValue<string>("company_id");
            IntegrationFileLocation = config.GetValue<string>("integration_file_location");
            IntegrationFileIntegrationId = config.GetValue<long>("integration_file_integration_id");
            
            HookReadIntervalMS = config.GetValue<int>("hook_read_interval_ms");
            FileUploadDelayIntervalSecs = config.GetValue<int>("file_upload_delay_interval_secs");
        }
    }
}
