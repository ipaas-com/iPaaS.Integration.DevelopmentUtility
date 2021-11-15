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
            var apiCall = new iPaaSApiCall("/v2/Auth/Login", null, iPaaSApiCall.ApiType.SSO, typeof(LoginResponse), RestSharp.Method.POST);

            var loginRequest = new LoginRequest() { EmailAddress = username, Password = password };

            apiCall.AddBodyParameter(loginRequest);
            
            var taskLogin = Task.Run(async () => await apiCall.ProcessRequest());
            var response = (LoginResponse)taskLogin.GetAwaiter().GetResult();

            return response;
        }

        public static LoginResponse RefreshToken(string accessToken, string refreshToken)
        {
            var apiCall = new iPaaSApiCall("/v2/Auth/Refresh", null, iPaaSApiCall.ApiType.SSO, typeof(LoginResponse), RestSharp.Method.POST);

            var loginRequest = new RefreshRequest() { AccessToken = accessToken, RefreshToken = refreshToken };

            apiCall.AddBodyParameter(loginRequest);

            var taskLogin = Task.Run(async () => await apiCall.ProcessRequest());
            var response = (LoginResponse)taskLogin.GetAwaiter().GetResult();

            return response;
        }

        public static LoginResponse ChangeCompany(string companyId)
        {
            var apiCall = new iPaaSApiCall("/v2/User/ChangeCompany/{id}", Utilities.Settings.Instance.DefaultFullToken, iPaaSApiCall.ApiType.SSO, typeof(LoginResponse), RestSharp.Method.GET);

            apiCall.AddParameter("id", companyId, RestSharp.ParameterType.UrlSegment);

            var taskLogin = Task.Run(async () => await apiCall.ProcessRequest());
            var response = (LoginResponse)taskLogin.GetAwaiter().GetResult();

            return response;
        }

        public static UserCompanyResponse Companies(string user)
        {
            var apiCall = new iPaaSApiCall("/v2/User/{id}/Companies", Utilities.Settings.Instance.DefaultFullToken, iPaaSApiCall.ApiType.SSO, typeof(UserCompanyResponse), RestSharp.Method.GET);

            apiCall.AddParameter("id", user, RestSharp.ParameterType.UrlSegment);

            var taskLogin = Task.Run(async () => await apiCall.ProcessRequest());
            var response = (UserCompanyResponse)taskLogin.GetAwaiter().GetResult();

            return response;
        }

        public static List<PersistentDataResponse> PersistentData(long systemId, List<Integration.Abstract.Model.PersistentData> persistentData, FullToken companyToken)
        {
            var persistentDataRequest = new List<PersistentDataRequest>();
            foreach (var persistentDatum in persistentData)
                persistentDataRequest.Add(new PersistentDataRequest() { Name = persistentDatum.Name, Value = persistentDatum.Value, ExpirationDateTime = persistentDatum.ExpirationDateTime } );

            var apiCall = new iPaaSApiCall("/v2/Setting/PersistentData/{id}", companyToken, iPaaSApiCall.ApiType.Integration, typeof(List<PersistentDataResponse>), RestSharp.Method.POST);

            apiCall.AddParameter("id", systemId, RestSharp.ParameterType.UrlSegment);
            apiCall.AddBodyParameter(persistentDataRequest);

            var taskLogin = Task.Run(async () => await apiCall.ProcessRequest());
            var response = (List<PersistentDataResponse>)taskLogin.GetAwaiter().GetResult();

            return response;
        }


        public static List<SettingGetAllResponse> Settings(FullToken companyToken)
        {
            var apiCall = new iPaaSApiCall("/v2/Settings", companyToken, iPaaSApiCall.ApiType.Integration, typeof(List<SettingGetAllResponse>), RestSharp.Method.GET);

            //Sometimes this call does returns 401s (e.g. in the event we are an admin but do not have specific access to the given company)
            //Do not display those error messages
            apiCall.SuppressError = true;

            var taskLogin = Task.Run(async () => await apiCall.ProcessRequest());
            var response = (List<SettingGetAllResponse>)taskLogin.GetAwaiter().GetResult();

            return response;
        }

        public static SettingResponse Setting(string systemId, FullToken companyToken)
        {
            var apiCall = new iPaaSApiCall("/v2/Setting/{id}?include=persistentdata", companyToken, iPaaSApiCall.ApiType.Integration, typeof(SettingResponse), RestSharp.Method.GET);

            apiCall.AddParameter("id", systemId, RestSharp.ParameterType.UrlSegment);

            var taskLogin = Task.Run(async () => await apiCall.ProcessRequest());
            var response = (SettingResponse)taskLogin.GetAwaiter().GetResult();

            return response;
        }


        public static HeaderResponse RetrieveLog(Guid trackingGuid, FullToken systemToken)
        {
            var apiCall = new iPaaSApiCall("/v2/Header/{id}", systemToken, iPaaSApiCall.ApiType.Logger, typeof(HeaderResponse), RestSharp.Method.GET);

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

            var apiCall = new iPaaSApiCall($"/v2/Details?filters={filter}", systemToken, iPaaSApiCall.ApiType.Logger, typeof(List<DetailResponse>), RestSharp.Method.GET);

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

            var apiCall = new iPaaSApiCall(url, new FullToken() { AcessToken = webhookApiKey }, iPaaSApiCall.ApiType.Hook, null, RestSharp.Method.POST);

            apiCall.AddBodyParameter(webhook);

            var taskLogger = Task.Run(async () => await apiCall.ProcessRequest());
            taskLogger.GetAwaiter().GetResult();
        }

        public static SystemTypeVersionResponse UploadFile(long systemTypeId, string fileName, SystemTypeVersionRequest request, string webhookApiKey)
        {
            string url = "/v2/Service/SystemType/FileUpload/{id}";

            var apiCall = new iPaaSApiCall(url, new FullToken() { AcessToken = webhookApiKey }, iPaaSApiCall.ApiType.Integration, typeof(SystemTypeVersionResponse), RestSharp.Method.POST);

            apiCall.AddParameter("id", systemTypeId, RestSharp.ParameterType.UrlSegment);
            apiCall.AddFile("integrationFile", fileName);
            //Since this endpoint accepts a file, we have to send the request as a serialized JSON string.
            apiCall.AddSerializedParameter("integrationVersionRequestJSON", request, RestSharp.ParameterType.GetOrPost);

            var taskLogger = Task.Run(async () => await apiCall.ProcessRequest());
            var response = (SystemTypeVersionResponse)taskLogger.GetAwaiter().GetResult();
            return response;
        }
    }
}
