using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntegrationDevelopmentUtility.iPaaSModels
{
    public class MappingCollectionGetAllResponse
    {
        [JsonProperty("id")]
        public long Id { get; set; }
        [JsonProperty("name")]
        public string Name { get; set; }
        [JsonProperty("collection_type_id")]
        public int CollectionTypeId { get; set; }
        [JsonProperty("collection_type_name")]
        public string CollectionTypeName { get; set; }
        [JsonProperty("attached_items_count")]
        public int AttachedItemsCount { get; set; }
        [JsonProperty("system_id")]
        public long SystemId { get; set; }
        [JsonProperty("system_name")]
        public string SystemName { get; set; }
        [JsonProperty("system_type_id")]
        public int SystemTypeId { get; set; }
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
        [JsonProperty("sequence_number")]
        public int? SequenceNumber { get; set; }
        [JsonProperty("mappings")]
        public List<MappingResponse> Mappings { get; set; }
        [JsonProperty("children")]
        public List<MappingCollectionGetAllResponse> Children { get; set; }
    }
}
