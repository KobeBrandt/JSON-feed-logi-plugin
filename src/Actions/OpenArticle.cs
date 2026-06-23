namespace Loupedeck.LttlabsArticlesPlugin.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    internal class OpenArticle : PluginDynamicCommand
    {
        private String url = "https://www.lttlabs.com/articles";
        
        public OpenArticle()
            : base(
                displayName: "Open Article",
                description: "Opens article in the browser",
                groupName: "URL Commands"
               )
        {
        }
        
        public OpenArticle(String commandName, String url)
            : base(
                displayName: commandName,
                description: $"Opens {commandName} in the browser",
                groupName: "URL Commands"
               )
        {
            this.url = url;
        }
        
        public void SetUrl(String newUrl)
        {
            this.url = newUrl;
        }
        
        protected override void RunCommand(String actionParameter) => Process.Start(new ProcessStartInfo(this.url) { UseShellExecute = true });
    }
}
