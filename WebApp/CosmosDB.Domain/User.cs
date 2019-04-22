using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;
using Newtonsoft.Json;

namespace CosmosDB.Domain
{
    public class User : BaseDocument
    {
        [JsonProperty(PropertyName = "Name")]

        public string Name { get; set; }

        [JsonProperty(PropertyName = "Email")]

        public string Email { get; set; }
    }
}
