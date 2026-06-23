namespace Loupedeck.LttlabsArticlesPlugin.Helpers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Http;
    using System.Text;
    using System.Text.Json;
    using System.Text.Json.Serialization;
    using System.Threading.Tasks;

    public static class ArticleConnector
    {
        private static readonly HttpClient client = new HttpClient();
        
        public static async Task<List<Article>> GetArticles()
        {
            return await GetArticlesFromAllUrls();
        }
        
         public static async Task<List<Article>> GetArticles(List<String> urls)
         {
             var settings = new ArticleSettings(urls, String.Empty);
             return await GetArticlesFromUrls(urls);
         }
        
        public static async Task<List<Article>> GetArticlesFromUrls(List<String> urls)
        {
            var allArticles = new List<Article>();
            
            foreach (var url in urls)
            {
                try
                {
                    var articles = await GetArticlesFromUrl(url);
                    if (articles != null)
                    {
                        allArticles.AddRange(articles);
                    }
                }
                catch (Exception ex)
                {
                    PluginLog.Error($"Exception fetching from {url}: {ex.Message}");
                }
            }
            
            return allArticles;
        }
        
        public static async Task<List<Article>> GetArticlesFromAllUrls()
        {
            var allArticles = new List<Article>();
            var settings = PluginSettingsManager.GetSetting<ArticleSettings>("ArticleSettings", new ArticleSettings());
            
            foreach (var url in settings.ArticleUrls)
            {
                try
                {
                    var articles = await GetArticlesFromUrl(url);
                    if (articles != null)
                    {
                        allArticles.AddRange(articles);
                    }
                }
                catch (Exception ex)
                {
                    PluginLog.Error($"Exception fetching from {url}: {ex.Message}");
                }
            }
            
            return allArticles;
        }
        
        public static async Task<List<Article>> GetArticlesFromUrl(String url)
        {
            try
            {
                HttpResponseMessage response = await client.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    string responseBody = await response.Content.ReadAsStringAsync();
                    PluginLog.Info($"Response from {url}: {responseBody}");
                    var json = JsonSerializer.Deserialize<Articles>(responseBody);
                    return json?.Article;
                }
                else
                {
                    PluginLog.Error($"Error fetching {url}: {response.StatusCode}");
                    return null;
                }
            }
            catch (Exception ex)
            {
                PluginLog.Error($"Exception fetching {url}: {ex.Message}");
                return null;
            }
        }
    }
}
