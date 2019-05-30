using Newtonsoft.Json;
using System;

namespace CosmosDB.Domain
{
    public abstract class BaseDocument
    {
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }

        [JsonProperty(PropertyName = "Type")]
        public string Type { get; set; }

        [JsonProperty(PropertyName = "CreatedAt")]
        public DateTime CreatedAt { get; set; }

        [JsonProperty(PropertyName = "ModifiedAt")]
        public DateTime? ModifiedAt { get; set; }

        [JsonProperty(PropertyName = "CreatedBy")]
        public User CreatedBy { get; set; }
    }
}
