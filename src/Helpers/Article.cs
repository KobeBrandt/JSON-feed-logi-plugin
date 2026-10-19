namespace Loupedeck.JsonFeedPlugin.Helpers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Text.Json.Serialization;
    using System.Threading.Tasks;

    public class Article
    {
        [JsonPropertyName("id")]
        public String Id { get; set; }

        [JsonPropertyName("url")]
        public String Url { get; set; }

        [JsonPropertyName("title")]
        public String Title { get; set; }

        [JsonPropertyName("summary")]
        public String Summary { get; set; }

        [JsonPropertyName("image")]
        public String Image { get; set; }

        [JsonPropertyName("date_published")]
        public DateTime DatePublished { get; set; }

        [JsonPropertyName("date_modified")]
        public DateTime DateModified { get; set; }

        // Many feeds only set date_published, so sort on whichever date is newest.
        [JsonIgnore]
        public DateTime SortDate => this.DateModified > this.DatePublished ? this.DateModified : this.DatePublished;

        [JsonPropertyName("author")]
        public Author Author { get; set; }
    }
}
