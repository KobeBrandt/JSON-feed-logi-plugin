namespace Loupedeck.JsonFeedPlugin.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading.Tasks;

    using Loupedeck.JsonFeedPlugin.Helpers;

    public class ListOfArticles : PluginDynamicFolder
    {
        // Action parameter for the button shown when there are no articles to list.
        private const String OpenSettingsParameter = "open-settings";

        private List<Article> articles = new List<Article>();
        private ArticleSettings settings = new ArticleSettings();
        private static ListOfArticles _instance;

        public static ListOfArticles Instance => _instance;

        public ListOfArticles()
        {
            this.DisplayName = "JSON Feed";
            this.GroupName = "JSON Feed";
            _instance = this;
        }

        public override PluginDynamicFolderNavigation GetNavigationArea(DeviceType _)
        {
            return PluginDynamicFolderNavigation.EncoderArea;
        }

        public override Boolean Load()
        {
            // Fetch the feeds in the background so slow feeds do not block the plugin from loading.
            Task.Run(this.ReloadArticles);
            return true;
        }

        public void ReloadArticles()
        {
            try
            {
                this.settings = PluginSettingsManager.GetSetting<ArticleSettings>("ArticleSettings", new ArticleSettings());
                this.articles = ArticleConnector.GetArticlesFromAllUrls().GetAwaiter().GetResult()
                    .Where(a => IsWebUrl(a?.Url))
                    .GroupBy(a => a.Url)
                    .Select(g => g.First())
                    .OrderByDescending(a => a.SortDate)
                    .ToList();
            }
            catch (Exception ex)
            {
                PluginLog.Error("Error reloading articles: " + ex.Message);
                this.articles = new List<Article>();
            }

            if (this.articles.Count == 0)
            {
                this.Plugin.OnPluginStatusChanged(PluginStatus.Warning, "No articles found. Open the JSON Feed settings to add a feed URL.");
            }
            else
            {
                this.Plugin.OnPluginStatusChanged(PluginStatus.Normal, null);
            }

            // Tell the device to redraw the folder with the new list of articles.
            this.ButtonActionNamesChanged();
        }

        public override IEnumerable<String> GetButtonPressActionNames(DeviceType _)
        {
            var result = new List<String> { PluginDynamicFolder.NavigateUpActionName };
            var currentArticles = this.articles;

            if (currentArticles.Count == 0)
            {
                result.Add(this.CreateCommandName(OpenSettingsParameter));
                return result;
            }

            // The article URL is the action parameter, because titles are not unique across feeds.
            foreach (var article in currentArticles)
            {
                result.Add(this.CreateCommandName(article.Url));
            }
            return result;
        }

        public override String GetCommandDisplayName(String actionParameter, PluginImageSize imageSize)
        {
            if (actionParameter == OpenSettingsParameter)
            {
                return "No articles - Open settings";
            }

            var article = this.articles.FirstOrDefault(a => a.Url == actionParameter);
            if (article == null || String.IsNullOrWhiteSpace(article.Title))
            {
                return actionParameter;
            }
            return article.Title;
        }

        public override void RunCommand(String actionParameter)
        {
            if (actionParameter == OpenSettingsParameter)
            {
                (this.Plugin as JsonFeedPlugin)?.OpenSettingsUrl();
                return;
            }

            var url = IsWebUrl(actionParameter) ? actionParameter : this.settings.DefaultUrl;

            // Only open web links. A feed must not be able to start local programs or files.
            if (!IsWebUrl(url))
            {
                PluginLog.Warning("Ignored article link that is not an http or https URL: " + url);
                return;
            }

            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            this.Close();
        }

        private static Boolean IsWebUrl(String url) => WebUrl.IsValid(url);
    }
}
