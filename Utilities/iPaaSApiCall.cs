using IntegrationDevelopmentUtility.iPaaSModels;
using Newtonsoft.Json;
using RestSharp;
using RestSharp.Serializers.NewtonsoftJson;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace IntegrationDevelopmentUtility.Utilities
{
    public class iPaaSApiCall
    {
        public enum ApiType
        {
            Hook,
            Subscription, //Formerly called Integration
            Logger, //AKA listener, LT319
            Product,
            SSO,
            Integrator,
            GiftCard,
            Customer,
            Transaction
        }

        private string _url;
        private FullToken _fullToken;
        private ApiType _api;
        private Type _responseType;
        private Method _requestMethod;
        private List<RestSharpParameterHolder> _parameters;
        private List<RestSharpFileParameterHolder> _files;
        private bool _isFileUpload = false;

        public bool SuppressError = false;


        public iPaaSApiCall(string url, FullToken fullToken, ApiType api, Type responseType, Method requestMethod)
        {
            _url = url;
            _fullToken = fullToken;
            _api = api;
            _responseType = responseType;
            _requestMethod = requestMethod;
        }

        public void AddBodyParameter(object value)
        {
            AddParameter("application/json", value, ParameterType.RequestBody);

            // Serializing the object
            //string bodyJSON = JsonConvert.SerializeObject(value, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });

            // Add the serialize object to the body of the request
            //AddParameter("application/json", bodyJSON, ParameterType.RequestBody);
        }

        /// <summary>
        /// Add a parameter to the api call
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <param name="type"></param>
        public void AddParameter(string name, object value, ParameterType type)
        {
            // If the parameter list does not exist initialize it
            if (_parameters == null)
                _parameters = new List<RestSharpParameterHolder>();

            // Add the parameter to the list
            _parameters.Add(new RestSharpParameterHolder() { Name = name, Value = value, Type = type });
        }

        /// <summary>
        /// Serialize the value param before adding it as a parameter
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <param name="type"></param>
        public void AddSerializedParameter(string name, object value, ParameterType type)
        {
            //AddParameter(name, value, type);

            // Serializing the object
            string bodyJSON = JsonConvert.SerializeObject(value, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });

            // Add the parameter to the list
            AddParameter(name, bodyJSON, type);
        }


        public void AddFile(string name, string fileLocation)
        {
            // If the file list does not exist initialize it
            if (_files == null)
                _files = new List<RestSharpFileParameterHolder>();

            // Add the file to the list
            _files.Add(new RestSharpFileParameterHolder() { FileName = fileLocation, Name = name });

            //Flag this request as a file upload, which will change several values in Proc
            _isFileUpload = true;
        }

        public async Task<object> ProcessRequest()
        {
            string baseUrl;
            if (_api == ApiType.Hook)
                baseUrl = Settings.Instance.HookUrl;
            else if (_api == ApiType.Logger)
                baseUrl = Settings.Instance.LoggerUrl;
            else if (_api == ApiType.Integrator)
                baseUrl = Settings.Instance.IntegratorUrl;
            else if (_api == ApiType.Subscription)
                baseUrl = Settings.Instance.SubscriptionUrl;
            else if (_api == ApiType.Product)
                baseUrl = Settings.Instance.ProductUrl;
            else if (_api == ApiType.Customer)
                baseUrl = Settings.Instance.CustomerURL;
            else if (_api == ApiType.GiftCard)
                baseUrl = Settings.Instance.GiftCardUrl;
            else if (_api == ApiType.Transaction)
                baseUrl = Settings.Instance.TransactionUrl;
            else
                baseUrl = Settings.Instance.SSOUrl;

            // Create the client 
            //var client = new RestClient(baseUrl, configureSerialization: s => s.UseNewtonsoftJson(new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }));
            var client = new RestClient(baseUrl);
            if (!_isFileUpload)
            {
                client.AddDefaultHeader("Content-Type", "application/json");
                client.AddDefaultHeader("Content_Type", "application/json");
                client.UseSerializer(() => new RestSharpNewtonsoftSerializer()); //(new JsonSerializerSettings() { NullValueHandling = NullValueHandling.Ignore });
            }
            else
            {
                //client.Timeout = -1;
                ;
            }


            // Create the request
            var req = new RestRequest(_url, _requestMethod);
            if (!_isFileUpload)
                req.RequestFormat = DataFormat.Json;
            else
                req.AddHeader("Content-Type", "multipart/form-data");

            //Add the auth token, if there is one
            if (_fullToken != null)
            {
                ValidateFullToken(_fullToken);

                req.AddHeader("Authorization", "Bearer " + _fullToken.AcessToken);
            }

            //Attach any files, if this is a fileupload
            if (_isFileUpload)
            {
                foreach (var file in _files)
                    req.AddFile(file.Name, file.FileName);
            }
            //req.Files.AddRange(_files);

            // Add parmeters if the are any
            if (_parameters != null)
            {
                foreach (var param in _parameters)
                {
                    switch (param.Type)
                    {
                        case ParameterType.QueryString:
                            req.AddQueryParameter(param.Name, Convert.ToString(param.Value));
                            break;
                        case ParameterType.RequestBody:
                            req.AddJsonBody(param.Value);
                            break;
                        case ParameterType.UrlSegment:
                            req.AddUrlSegment(param.Name, Convert.ToString(param.Value));
                            break;
                        case ParameterType.GetOrPost:
                            req.AddParameter(param.Name, param.Value, ParameterType.GetOrPost);
                            break;
                        default:
                            throw new Exception("Unsupported parameter type: " + param.Type.ToString());
                    }
                }
            }

            RestSharp.RestResponse resp = null;

            // execute the request
            try
            {
                resp = await client.ExecuteAsync(req);
            }
            catch (Exception ex)
            {
                StandardUtilities.WriteToConsole($"An error occurred on the call to {_url}", StandardUtilities.Severity.ERROR);
                StandardUtilities.WriteToConsole(ex.Message, StandardUtilities.Severity.ERROR);
                if(ex.InnerException != null)
                    StandardUtilities.WriteToConsole($"   InnerException: {ex.InnerException.Message}", StandardUtilities.Severity.ERROR);
                return null;
            }

            bool succesfulResponse = HandleResponse(resp);

            if (!succesfulResponse)
                return null;

            if (_responseType == null)
                return null;

            //Convert the response to desired typed
            var retVal = JsonConvert.DeserializeObject(Convert.ToString(resp.Content), _responseType);
            return retVal;
        }

        private bool HandleResponse(RestSharp.RestResponse resp)
        {
            if (resp.StatusCode != System.Net.HttpStatusCode.OK && resp.StatusCode != System.Net.HttpStatusCode.Accepted && resp.StatusCode != System.Net.HttpStatusCode.NoContent)
            {
                //Do not display error messages if the SuppressError is set to true
                if (!SuppressError)
                {
                    var errorMessage = ProcessFullErrorMessage(resp);
                    StandardUtilities.WriteToConsole($"An error was returned after the call to {_url}", StandardUtilities.Severity.ERROR);
                    StandardUtilities.WriteToConsole(errorMessage, StandardUtilities.Severity.ERROR);
                }
                else
                    ;
                return false;
            }

            return true;
        }

        private string ProcessFullErrorMessage(RestSharp.RestResponse resp)
        {
            string errMsg = "Error: ";
            if (!string.IsNullOrEmpty(resp.ErrorMessage))
                errMsg += resp.ErrorMessage;

            if (!string.IsNullOrEmpty(resp.Content))
                errMsg += resp.Content;

            if (!string.IsNullOrEmpty(resp.StatusDescription))
                errMsg += resp.StatusDescription;

            errMsg += " (Http Code: " + resp.StatusCode.ToString() + ")";
            return errMsg;
        }


        public static void ValidateFullToken(FullToken fullToken)
        {
            //If the token is not expired, we are fine.
            if (fullToken.AccessTokenExpiration > DateTimeOffset.Now)
                return;

            //There are cases where we do not have a refreshtoken (e.g. the access token for hooks). In that case, we just exit here.
            if (fullToken.RefreshToken == null)
                return;

            var refreshResponse = iPaaSCallWrapper.RefreshToken(fullToken.AcessToken, fullToken.RefreshToken);
            if (refreshResponse == null)
                throw new Exception("Unable to refresh acess token, which has expired.");

            fullToken.AcessToken = refreshResponse.AccessToken;
            fullToken.AccessTokenExpiration = refreshResponse.AccessTokenExpiration;
        }

        private class RestSharpParameterHolder
        {
            public string Name;
            public object Value;
            public ParameterType Type;
        }

        private class RestSharpFileParameterHolder
        {
            public string FileName;
            public string Name;
        }

    }
}
