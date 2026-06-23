namespace Loupedeck.LttlabsArticlesPlugin.Actions
{
    using System;
    using System.Diagnostics;
    using Loupedeck.LttlabsArticlesPlugin.Helpers;

    internal class OpenSettings : PluginDynamicCommand
    {
        public OpenSettings()
            : base(
                displayName: "Open URL Settings",
                description: "Open the article URL settings page",
                groupName: "Settings"
            )
        {
        }

        protected override void RunCommand(String actionParameter)
        {
            if (this.Plugin is LttlabsArticlesPlugin plugin && plugin.WebServer != null)
            {
                var url = plugin.WebServer.SettingsUrl;
                PluginLog.Info($"Opening settings URL: {url}");
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            }
        }
    }
}
