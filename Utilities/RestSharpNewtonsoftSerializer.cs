using Newtonsoft.Json;
using RestSharp;
using RestSharp.Serializers;
using System;
using System.Collections.Generic;

namespace IntegrationDevelopmentUtility.Utilities
{
    /// <summary>
    /// As of RestSharp 1.0.7, there is no official support for newtonsoft, nor is there a documented way to add default serialization settings. So we solve both problems
    /// with this class: it implements the newtonsoft serializer and sets default null handling to our standard style.
    /// </summary>

    public class RestSharpNewtonsoftSerializer : IRestSerializer
    {
        public string Serialize(object obj) => JsonConvert.SerializeObject(obj, new JsonSerializerSettings() { NullValueHandling = NullValueHandling.Ignore });

        public string Serialize(Parameter bodyParameter) => Serialize(bodyParameter.Value);

        public T Deserialize<T>(RestResponse response) => JsonConvert.DeserializeObject<T>(response.Content);

        public string[] SupportedContentTypes { get; } = {
        "application/json", "text/json", "text/x-json", "text/javascript", "*+json"};

        public string ContentType { get; set; } = "application/json";

        public DataFormat DataFormat { get; } = DataFormat.Json;

        ISerializer IRestSerializer.Serializer => throw new NotImplementedException();

        IDeserializer IRestSerializer.Deserializer => throw new NotImplementedException();

        public string[] AcceptedContentTypes { get; } = {
        "application/json", "text/json", "text/x-json", "text/javascript", "*+json"};

        SupportsContentType IRestSerializer.SupportsContentType => TestSupportsContentType;

        public static bool TestSupportsContentType(string name)
        {
            var testTypeList = new List<string>() { "application/json", "text/json", "text/x-json", "text/javascript", "*+json" };
            return testTypeList.Contains(name);
        }
    }
}
