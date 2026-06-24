namespace Loupedeck.JsonFeedPlugin.Helpers
{
    using System;
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    public class NamedUrl
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }
        [JsonPropertyName("url")]
        public string Url { get; set; }

        public NamedUrl() { }

        public NamedUrl(string name, string url)
        {
            Name = name;
            Url = url;
        }
    }

    public class NamedUrlSettings
    {
        public List<NamedUrl> NamedUrls { get; set; } = new List<NamedUrl>();

        public NamedUrlSettings() { }
    }

    public class ArticleSettings
    {
        public List<string> ArticleUrls { get; set; } = new List<string>();

         public string DefaultUrl { get; set; } = String.Empty;

        public ArticleSettings()
        {
        }

         public ArticleSettings(List<string> urls, string defaultUrl)
         {
             ArticleUrls = urls ?? new List<string>();
             DefaultUrl = defaultUrl ?? String.Empty;
         }
    }
}
