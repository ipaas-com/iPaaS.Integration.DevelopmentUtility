using Integration.Abstract.Model;
using IntegrationDevelopmentUtility.iPaaSModels;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace IntegrationDevelopmentUtility.Utilities
{
    public class iPaaSCallWrapper
    {
        public static LoginResponse Login(string username, string password)
        {
            var apiCall = new iPaaSApiCall("/v2/Auth/Login", null, iPaaSApiCall.ApiType.SSO, typeof(LoginResponse), RestSharp.Method.Post);

            var loginRequest = new LoginRequest() { EmailAddress = username, Password = password };

            apiCall.AddBodyParameter(loginRequest);

            var taskLogin = Task.Run(async () => await apiCall.ProcessRequest());
            var response = (LoginResponse)taskLogin.GetAwaiter().GetResult();

            return response;
        }

        public static LoginResponse RefreshToken(string accessToken, string refreshToken)
        {
            var apiCall = new iPaaSApiCall("/v2/Auth/Refresh", null, iPaaSApiCall.ApiType.SSO, typeof(LoginResponse), RestSharp.Method.Post);

            var loginRequest = new RefreshRequest() { AccessToken = accessToken, RefreshToken = refreshToken };

            apiCall.AddBodyParameter(loginRequest);

            var taskLogin = Task.Run(async () => await apiCall.ProcessRequest());
            var response = (LoginResponse)taskLogin.GetAwaiter().GetResult();

            return response;
        }

        public static LoginResponse ChangeCompany(string companyId)
        {
            var apiCall = new iPaaSApiCall("/v2/User/ChangeCompany/{id}", Utilities.Settings.Instance.DefaultFullToken, iPaaSApiCall.ApiType.SSO, typeof(LoginResponse), RestSharp.Method.Get);

            apiCall.AddParameter("id", companyId, RestSharp.ParameterType.UrlSegment);

            var taskLogin = Task.Run(async () => await apiCall.ProcessRequest());
            var response = (LoginResponse)taskLogin.GetAwaiter().GetResult();

            return response;
        }

        public static UserCompanyResponse_Separated Companies(string user)
        {
            var apiCall = new iPaaSApiCall("/v2/User/{id}/Companies", Utilities.Settings.Instance.DefaultFullToken, iPaaSApiCall.ApiType.SSO, typeof(List<UserCompanyResponse>), RestSharp.Method.Get);

            apiCall.AddParameter("id", user, RestSharp.ParameterType.UrlSegment);

            var taskLogin = Task.Run(async () => await apiCall.ProcessRequest());
            var response = (List<UserCompanyResponse>)taskLogin.GetAwaiter().GetResult();

            //This is to handle a change in the K2SO API. Previously it was outputting in the format defined in UserCompanyResponse_Separated. Now it returns the format in UserCompanyResponse
            // as a list. The differnce is that the former had a separated set of companies by type. The latter has a single list with a Designations dictionary. To speed up the switch over,
            //  we preserve the old format and provide the code below to convert the old to new.
            var responseSeparated = new UserCompanyResponse_Separated();

            //Add everyone to the company list
            foreach (var company in response)
                responseSeparated.Companies.Add(new CompanyInfoResponse(company, "Company"));

            var adminCompanies = response.FindAll(x => x.Designations.ContainsValue("Admin"));
            foreach(var adminCompany in adminCompanies)
                responseSeparated.AdminCompanies.Add(new CompanyInfoResponse(adminCompany, "Admin"));

            var mispCompanies = response.FindAll(x => x.Designations.ContainsValue("MiSP"));
            foreach (var mispCompany in mispCompanies)
                responseSeparated.MISPs.Add(new CompanyInfoResponse(mispCompany, "MiSP"));

            var techCompanies = response.FindAll(x => x.Designations.ContainsValue("Tech Partner"));
            foreach (var techCompany in techCompanies)
                responseSeparated.TechPartners.Add(new CompanyInfoResponse(techCompany, "Tech Partner"));

            var integratorCompanies = response.FindAll(x => x.Designations.ContainsValue("Integrator"));
            foreach (var integratorCompany in integratorCompanies)
                responseSeparated.Integrators.Add(new CompanyInfoResponse(integratorCompany, "Integrator"));


            return responseSeparated;
        }


        public static List<DynamicFormulaResponse> DynamicFormulas(string systemTypeVersion, FullToken companyToken)
        {
            var filter = $"SystemTypeVersionId={systemTypeVersion}";

            //Note: these are v1 on the integrator api and v2 on Tarkin
            var apiCall = new iPaaSApiCall("/v1/DynamicFormulas", companyToken, iPaaSApiCall.ApiType.Integrator, typeof(List<DynamicFormulaResponse>), RestSharp.Method.Get);
            apiCall.AddParameter("filter", filter, RestSharp.ParameterType.QueryString); //Pass this as a param so that it will handle encoding for us.

            var taskLogin = Task.Run(async () => await apiCall.ProcessRequest());
            var response = (List<DynamicFormulaResponse>)taskLogin.GetAwaiter().GetResult();

            return response;
        }

        public static DynamicFormulaResponse DynamicFormulaCreate(DynamicFormulaRequest request, FullToken companyToken)
        {
            //Note: these are v1 on the integrator api and v2 on Tarkin
            var apiCall = new iPaaSApiCall("/v1/DynamicFormula", companyToken, iPaaSApiCall.ApiType.Integrator, typeof(DynamicFormulaResponse), RestSharp.Method.Post);

            apiCall.AddBodyParameter(request);

            var taskLogin = Task.Run(async () => await apiCall.ProcessRequest());
            var response = (DynamicFormulaResponse)taskLogin.GetAwaiter().GetResult();

            return response;
        }

        public static DynamicFormulaResponse DynamicFormulaUpdate(DynamicFormulaRequest request, FullToken companyToken, long id)
        {
            //Note: these are v1 on the integrator api and v2 on Tarkin
            var apiCall = new iPaaSApiCall("/v1/DynamicFormula/{id}", companyToken, iPaaSApiCall.ApiType.Integrator, typeof(DynamicFormulaResponse), 
                RestSharp.Method.Put);

            apiCall.AddParameter("id", id, RestSharp.ParameterType.UrlSegment);
            apiCall.AddBodyParameter(request);

            var taskLogin = Task.Run(async () => await apiCall.ProcessRequest());
            var response = (DynamicFormulaResponse)taskLogin.GetAwaiter().GetResult();

            return response;
        }

        /// <summary>
        /// Note that this method does not return the field description
        /// </summary>
        /// <param name="filters"></param>
        /// <param name="systemToken"></param>
        /// <returns></returns>
        public static List<FieldGetAllResponse> FieldsGetAll(string filters, FullToken systemToken)
        {
            var url = "/v1/Integration/Fields";
            if (!string.IsNullOrEmpty(filters))
                url += $"?filter={System.Net.WebUtility.UrlEncode(filters)}";

            var apiCall = new iPaaSApiCall(url, systemToken, iPaaSApiCall.ApiType.Integrator, typeof(List<FieldGetAllResponse>), RestSharp.Method.Get);

            var taskLogger = Task.Run(async () => await apiCall.ProcessRequest());
            var response = (List<FieldGetAllResponse>)taskLogger.GetAwaiter().GetResult();

            return response;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="filters"></param>
        /// <param name="systemToken"></param>
        /// <returns></returns>
        public static List<LookupResponse> FieldsLookup(string filters, FullToken systemToken)
        {
            var url = "/v2/Lookup/Field";
            if (!string.IsNullOrEmpty(filters))
                url += $"?filter={System.Net.WebUtility.UrlEncode(filters)}";

            var apiCall = new iPaaSApiCall(url, systemToken, iPaaSApiCall.ApiType.Subscription, typeof(List<LookupResponse>), RestSharp.Method.Get);

            var taskLogger = Task.Run(async () => await apiCall.ProcessRequest(true));
            var response = (List<LookupResponse>)taskLogger.GetAwaiter().GetResult();

            return response;
        }

        public static List<LookupResponse> Integrations(FullToken companyToken)
        {
            var apiCall = new iPaaSApiCall("/v2/Lookup/Integration?sortBy=Name", companyToken, iPaaSApiCall.ApiType.Subscription, typeof(List<LookupResponse>), RestSharp.Method.Get);

            //Sometimes this call does returns 401s (e.g. in the event we are an admin but do not have specific access to the given company)
            //Do not display those error messages
            apiCall.SuppressError = true;

            var taskLogin = Task.Run(async () => await apiCall.ProcessRequest());
            var response = (List<LookupResponse>)taskLogin.GetAwaiter().GetResult();

            return response;
        }

        public static MappingCollectionResponse MappingCollectionGet(long mappingCollectionId, FullToken systemToken)
        {
            var apiCall = new iPaaSApiCall("/v2/MappingCollection/{id}", systemToken, iPaaSApiCall.ApiType.Subscription, typeof(MappingCollectionResponse), RestSharp.Method.Get);

            apiCall.AddParameter("id", mappingCollectionId, RestSharp.ParameterType.UrlSegment);

            var taskLogger = Task.Run(async () => await apiCall.ProcessRequest());
            var response = (MappingCollectionResponse)taskLogger.GetAwaiter().GetResult();

            return response;
        }


        //Save persistent data
        public static List<PersistentDataResponse> PersistentData(long systemId, List<Integration.Abstract.Model.PersistentData> persistentData, FullToken companyToken)
        {
            var persistentDataRequest = new List<PersistentDataRequest>();
            foreach (var persistentDatum in persistentData)
                persistentDataRequest.Add(new PersistentDataRequest() { Name = persistentDatum.Name, Value = persistentDatum.Value, ExpirationDateTime = persistentDatum.ExpirationDateTime });

            var apiCall = new iPaaSApiCall("/v2/Subscription/PersistentData/{id}", companyToken, iPaaSApiCall.ApiType.Subscription, typeof(List<PersistentDataResponse>), RestSharp.Method.Post);

            apiCall.AddParameter("id", systemId, RestSharp.ParameterType.UrlSegment);
            apiCall.AddBodyParameter(persistentDataRequest);

            var taskLogin = Task.Run(async () => await apiCall.ProcessRequest());
            var response = (List<PersistentDataResponse>)taskLogin.GetAwaiter().GetResult();

            return response;
        }



        public static List<SubscriptionGetAllResponse> Subscriptions(FullToken companyToken)
        {
            var apiCall = new iPaaSApiCall("/v2/Subscriptions", companyToken, iPaaSApiCall.ApiType.Subscription, typeof(List<SubscriptionGetAllResponse>), RestSharp.Method.Get);

            //Sometimes this call does returns 401s (e.g. in the event we are an admin but do not have specific access to the given company)
            //Do not display those error messages
            apiCall.SuppressError = true;

            var taskLogin = Task.Run(async () => await apiCall.ProcessRequest());
            var response = (List<SubscriptionGetAllResponse>)taskLogin.GetAwaiter().GetResult();

            return response;
        }

        public static SubscriptionResponse Subscription(string systemId, FullToken companyToken)
        {
            var apiCall = new iPaaSApiCall("/v2/Subscription/{id}?include=persistentdata", companyToken, iPaaSApiCall.ApiType.Subscription, typeof(SubscriptionResponse), RestSharp.Method.Get);

            apiCall.AddParameter("id", systemId, RestSharp.ParameterType.UrlSegment);

            var taskLogin = Task.Run(async () => await apiCall.ProcessRequest());
            var response = (SubscriptionResponse)taskLogin.GetAwaiter().GetResult();

            return response;
        }


        public static HeaderResponse RetrieveLog(Guid trackingGuid, FullToken systemToken)
        {
            var apiCall = new iPaaSApiCall("/v2/Header/{id}", systemToken, iPaaSApiCall.ApiType.Logger, typeof(HeaderResponse), RestSharp.Method.Get);

            apiCall.AddParameter("id", trackingGuid, RestSharp.ParameterType.UrlSegment);

            var taskLogger = Task.Run(async () => await apiCall.ProcessRequest());
            var response = (HeaderResponse)taskLogger.GetAwaiter().GetResult();

            return response;
        }

        public static List<DetailResponse> RetrieveLogDetail(Guid trackingGuid, DateTimeOffset? dateTimeOffset, FullToken systemToken)
        {
            if (!dateTimeOffset.HasValue)
                dateTimeOffset = DateTimeOffset.MinValue;

            //Format the DTO so that it is in the sortable format used in the MonboDB
            var formattedDTO = dateTimeOffset.Value.ToString("yyyy/MM/dd HH:mm:ss.fff zzz");

            string filter = $"parent_id={trackingGuid}&activity_timestamp={formattedDTO}";
            filter = WebUtility.UrlEncode(filter);

            var apiCall = new iPaaSApiCall($"/v2/Details?filters={filter}", systemToken, iPaaSApiCall.ApiType.Logger, typeof(List<DetailResponse>), RestSharp.Method.Get);

            //apiCall.AddParameter("filter", filter, RestSharp.ParameterType.UrlSegment);

            var taskLogger = Task.Run(async () => await apiCall.ProcessRequest());
            var response = (List<DetailResponse>)taskLogger.GetAwaiter().GetResult();

            return response;
        }


        public static void SendHook(WebhookRequest webhook, string direction, string webhookApiKey)
        {
            string url;
            if (direction == "FROM")
                url = "/v2/iPaaS";
            else
                url = "/v2/IntegrationDevelopmentUtility";

            var apiCall = new iPaaSApiCall(url, new FullToken() { AcessToken = webhookApiKey }, 
                iPaaSApiCall.ApiType.Hook, null, RestSharp.Method.Post);

            apiCall.AddBodyParameter(webhook);
            apiCall.AddParameter("x-correlation-id", webhook.Notifications[0].TrackingGuid.ToString(), RestSharp.ParameterType.HttpHeader);

            var taskLogger = Task.Run(async () => await apiCall.ProcessRequest());
            taskLogger.GetAwaiter().GetResult();
        }

        //Note that this endpoint does NOT return fields
        public static TableResponse TableGet(long tableId, FullToken systemToken)
        {
            var apiCall = new iPaaSApiCall("/v1/Integration/Table/{id}", systemToken, iPaaSApiCall.ApiType.Integrator, typeof(TableResponse), RestSharp.Method.Get);

            apiCall.AddParameter("id", tableId, RestSharp.ParameterType.UrlSegment);

            var taskLogger = Task.Run(async () => await apiCall.ProcessRequest());
            var response = (TableResponse)taskLogger.GetAwaiter().GetResult();

            return response;
        }

        /// <summary>
        /// This will NOT return a value for system type 1
        /// </summary>
        /// <param name="filters"></param>
        /// <param name="systemToken"></param>
        /// <returns></returns>
        public static List<TableGetAllResponse> TablesGetAll(string filters, FullToken systemToken)
        {
            var url = "/v1/Integration/Tables";
            if (!string.IsNullOrEmpty(filters))
                url += $"?filter={System.Net.WebUtility.UrlEncode(filters)}";

            var apiCall = new iPaaSApiCall(url, systemToken, iPaaSApiCall.ApiType.Integrator, typeof(List<TableGetAllResponse>), RestSharp.Method.Get);

            var taskLogger = Task.Run(async () => await apiCall.ProcessRequest());
            var response = (List<TableGetAllResponse>)taskLogger.GetAwaiter().GetResult();

            return response;
        }

        public static List<LookupResponse> TablesLookup(string filters, FullToken systemToken)
        {
            var url = "/v2/Lookup/Tables";
            if (!string.IsNullOrEmpty(filters))
                url += $"?filter={System.Net.WebUtility.UrlEncode(filters)}";

            var apiCall = new iPaaSApiCall(url, systemToken, iPaaSApiCall.ApiType.Subscription, typeof(List<LookupResponse>), RestSharp.Method.Get);

            var taskLogger = Task.Run(async () => await apiCall.ProcessRequest());
            var response = (List<LookupResponse>)taskLogger.GetAwaiter().GetResult();

            return response;
        }

        public static TopicSubscriptionResponse TopicSubscriptionCreate(TopicSubscriptionRequest request, FullToken companyToken)
        {
            //TODO: Change this to  iPaaSApiCall.ApiType.Logger once that is pushed to staging
            var apiCall = new iPaaSApiCall("v2/TopicSubscription", companyToken, iPaaSApiCall.ApiType.Subscription, typeof(TopicSubscriptionResponse), RestSharp.Method.Post);

            apiCall.AddBodyParameter(request);

            var taskLogger = Task.Run(async () => await apiCall.ProcessRequest());
            var retVal = (TopicSubscriptionResponse)taskLogger.GetAwaiter().GetResult();
            return retVal;
        }

        public static void TopicSubscriptionDelete(string topicName, FullToken companyToken)
        {
            //TODO: Change this to  iPaaSApiCall.ApiType.Logger once that is pushed to staging
            var apiCall = new iPaaSApiCall("v2/TopicSubscription/{topicName}", companyToken, iPaaSApiCall.ApiType.Subscription, null, RestSharp.Method.Delete);
            apiCall.AddParameter("topicName", topicName, RestSharp.ParameterType.UrlSegment);

            var taskLogger = Task.Run(async () => await apiCall.ProcessRequest());
            taskLogger.GetAwaiter().GetResult();
        }

        public static VersionResponse UploadFile(long integrationId, string fileName, VersionRequest request, string webhookApiKey)
        {
            string url = "/v1/Integration/FileUpload/{id}";

            var apiCall = new iPaaSApiCall(url, new FullToken() { AcessToken = webhookApiKey }, iPaaSApiCall.ApiType.Integrator, typeof(VersionResponse), RestSharp.Method.Post);

            apiCall.AddParameter("id", integrationId, RestSharp.ParameterType.UrlSegment);
            apiCall.AddFile("integrationFile", fileName);
            //Since this endpoint accepts a file, we have to send the request as a serialized JSON string.
            apiCall.AddSerializedParameter("integrationVersionRequestJSON", request, RestSharp.ParameterType.GetOrPost);

            var taskLogger = Task.Run(async () => await apiCall.ProcessRequest());
            var response = (VersionResponse)taskLogger.GetAwaiter().GetResult();
            return response;
        }
    }
}
