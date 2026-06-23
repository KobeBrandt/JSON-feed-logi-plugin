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
        private static ListOfAtricles _instance;
        
        public static ListOfAtricles Instance => _instance;
        
        public ListOfAtricles()
        {
            this.DisplayName = "Articles";
            this.GroupName = "LTT Labs";
            _instance = this;
        }
        public override PluginDynamicFolderNavigation GetNavigationArea(DeviceType _)
        {
            return PluginDynamicFolderNavigation.EncoderArea;
        }
         public override Boolean Load()
         {
             this.ReloadArticles();
             return true;
         }
         
          public void ReloadArticles()
          {
              try
              {
                  this.settings = PluginSettingsManager.GetSetting<ArticleSettings>("ArticleSettings", new ArticleSettings());
                  this.Plugin.OnPluginStatusChanged(PluginStatus.Normal, null);
                  this.articles = ArticleConnector.GetArticlesFromAllUrls().Result;
              }
              catch (Exception ex)
              {
                  PluginLog.Error("Error reloading articles: " + ex.Message);
                  this.articles = new List<Article>();
              }
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
