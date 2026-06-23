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
            
            var urls = settings.ArticleUrls ?? new List<string>();
            
            if (urls.Count == 0)
            {
                urls = LoadUrlsFromFile();
            }
            
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
        
        private static List<string> LoadUrlsFromFile()
        {
            try
            {
                var plugin = PluginSettingsManager.IsInitialized ? (Plugin)typeof(PluginSettingsManager).GetField("_pluginInstance", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)?.GetValue(null) : null;
                if (plugin != null)
                {
                    var pluginDataDir = plugin.GetPluginDataDirectory();
                    var filePath = System.IO.Path.Combine(pluginDataDir, "NamedUrlSettings.json");
                    if (System.IO.File.Exists(filePath))
                    {
                        var json = System.IO.File.ReadAllText(filePath);
                        var namedUrlSettings = System.Text.Json.JsonSerializer.Deserialize<NamedUrlSettings>(json);
                        if (namedUrlSettings?.NamedUrls != null)
                        {
                            PluginLog.Info("Loaded " + namedUrlSettings.NamedUrls.Count + " URLs from NamedUrlSettings.json file");
                            return namedUrlSettings.NamedUrls.Select(nu => nu.Url).Where(url => !string.IsNullOrEmpty(url)).ToList();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                PluginLog.Warning("Could not load URLs from file: " + ex.Message);
            }
            return new List<string>();
        }
        
         public static async Task<List<Article>> GetArticlesFromUrl(String url)
         {
             try
             {
                 HttpResponseMessage response = await client.GetAsync(url);
                 if (response.IsSuccessStatusCode)
                 {
                     string responseBody = await response.Content.ReadAsStringAsync();
                     PluginLog.Info($"Response from {url}");
                     
                     // Check if the response is valid JSON
                     if (string.IsNullOrWhiteSpace(responseBody) || !IsValidJson(responseBody))
                     {
                         PluginLog.Error($"Invalid JSON response from {url}");
                         return null;
                     }
                     
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
         
         private static bool IsValidJson(string jsonString)
         {
             try
             {
                 using (JsonDocument.Parse(jsonString)) { }
                 return true;
             }
             catch
             {
                 return false;
             }
         }
    }
}
