namespace Loupedeck.JsonFeedPlugin.Helpers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Text.Json.Serialization;
    using System.Threading.Tasks;

    public class Author
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }
    }
}
