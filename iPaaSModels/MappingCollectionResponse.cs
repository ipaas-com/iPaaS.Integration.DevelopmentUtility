using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntegrationDevelopmentUtility.iPaaSModels
{
    public class MappingCollectionResponse
    {
        [JsonProperty("id")]
        public long Id { get; set; }
        [JsonProperty("name")]
        public string Name { get; set; }
        [JsonProperty("collection_type_id")]
        public int CollectionTypeId { get; set; }
        [JsonProperty("collection_type_name")]
        public string CollectionTypeName { get; set; }
        
        [JsonProperty("subscription_id")]
        public long SubscriptionId { get; set; }
        
        [JsonProperty("subscription_name")]
        public string SubscriptionName { get; set; }

        [JsonProperty("integration_id")]
        public int IntegrationId { get; set; }
        
        [JsonProperty("filter")]
        public string Filter { get; set; }
        [JsonProperty("error_filter")]
        public string ErrorFilter { get; set; }
        [JsonProperty("sync_type")]
        public string SyncType { get; set; }
        [JsonProperty("parent_id")]
        public long? ParentId { get; set; }
        [JsonProperty("direction_id")]
        public int DirectionId { get; set; }
        
        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("collision_handling_method_id")]
        public int? CollisionHandlingMethodId { get; set; }

        [JsonProperty("collision_handling_options")]
        public string CollisionHandlingOptions { get; set; }

        [JsonProperty("sequence_number")]
        public int? SequenceNumber { get; set; }
        [JsonProperty("mappings")]
        public List<MappingResponse> Mappings { get; set; }
        [JsonProperty("children")]
        public List<MappingCollectionGetAllResponse> Children { get; set; }
    }
}
