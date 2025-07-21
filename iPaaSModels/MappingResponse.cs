using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntegrationDevelopmentUtility.iPaaSModels
{
    public class MappingResponse
    {
        [JsonProperty("id")]
        public string Id { get; set; }
        [JsonProperty("mapping_type_id")]
        public int MappingTypeId { get; set; }
        [JsonProperty("mapping_type_name")]
        public string MappingTypeName { get; set; }
        [JsonProperty("destination_table")]
        public string DestinationTable { get; set; }
        [JsonProperty("destination_field")]
        public string DestinationField { get; set; }
        [JsonProperty("destination_field_id")]
        public long DestinationFieldId { get; set; }
        [JsonProperty("source_table")]
        public string SourceTable { get; set; }
        [JsonProperty("source_value_id")]
        public long? SourceValueId { get; set; }
        [JsonProperty("source_value")]
        public string SourceValue { get; set; }
        [JsonProperty("default_value")]
        public string DefaultValue { get; set; }
        [JsonProperty("translation_collection_id")]
        public long? TranslationCollectionId { get; set; }
        [JsonProperty("translation_name")]
        public string TranslationName { get; set; }
        [JsonProperty("description")]
        public string Description { get; set; }
        [JsonProperty("sequence_number")]
        public int? SequenceNumber { get; set; }
    }
}
