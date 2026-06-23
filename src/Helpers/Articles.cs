namespace Loupedeck.LttlabsArticlesPlugin.Helpers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Text.Json;
    using System.Text.Json.Serialization;
    using System.Threading.Tasks;

    public class Articles
    {
            [JsonPropertyName("version")]
            public string Version { get; set; }

            [JsonPropertyName("title")]
            public string Title { get; set; }

            [JsonPropertyName("home_page_url")]
            public string HomePageUrl { get; set; }

            [JsonPropertyName("feed_url")]
            public string FeedUrl { get; set; }

            [JsonPropertyName("description")]
            public string Description { get; set; }

            [JsonPropertyName("items")]
            public List<Article> Article { get; set; }
    }
    }
