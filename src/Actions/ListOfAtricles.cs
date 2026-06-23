namespace Loupedeck.LttlabsArticlesPlugin.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    using Loupedeck.LttlabsArticlesPlugin.Helpers;

    public class ListOfAtricles : PluginDynamicFolder
    {
        private List<Article> articles;
        private ArticleSettings settings;
        
        public ListOfAtricles()
        {
            this.DisplayName = "Articles";
            this.GroupName = "LTT Labs";
        }
        public override PluginDynamicFolderNavigation GetNavigationArea(DeviceType _)
        {
            return PluginDynamicFolderNavigation.EncoderArea;
        }
        public override Boolean Load()
        {
            this.settings = PluginSettingsManager.GetSetting<ArticleSettings>("ArticleSettings", new ArticleSettings());
            
            if (this.settings.ArticleUrls == null || this.settings.ArticleUrls.Count == 0)
            {
                // If no URLs are configured, use the default
                this.settings.ArticleUrls = new List<string> { "https://www.lttlabs.com/articles/json" };
                PluginSettingsManager.SetSetting("ArticleSettings", this.settings);
            }
            
            this.articles = ArticleConnector.GetArticles(this.settings.ArticleUrls).Result;
            return true;
        }
        
        public override IEnumerable<String> GetButtonPressActionNames(DeviceType _)
        {
            List<String> result = new List<String>();

            result.Add(PluginDynamicFolder.NavigateUpActionName);

            foreach (var article in this.articles)
            {
                result.Add(this.CreateCommandName(article.Title));
            }
            return result;
        }
        public override void RunCommand(String actionParameter)
        {
            String url = this.settings.DefaultUrl;
            foreach (var article in articles)
            {
                if(actionParameter == article.Title)
                {
                    url = article.Url;
                    break;
                }
            }
            
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            this.Close();
        }
    }
}
