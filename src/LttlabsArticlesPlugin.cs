namespace Loupedeck.LttlabsArticlesPlugin
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Loupedeck.LttlabsArticlesPlugin.Actions;
    using Loupedeck.LttlabsArticlesPlugin.Helpers;
    using Loupedeck.LttlabsArticlesPlugin.Services;

    // This class contains the plugin-level logic of the Loupedeck plugin.

    public class LttlabsArticlesPlugin : Plugin
    {
        private readonly PluginPreferenceAccount _settingsPreferenceAccount;
        public SettingsWebServer? WebServer { get; private set; }
        private readonly Object _webServerLock = new();
        private volatile Boolean _isUnloading;

        // Gets a value indicating whether this is an API-only plugin.
        public override Boolean UsesApplicationApiOnly => true;

        // Gets a value indicating whether this is a Universal plugin or an Application plugin.
        public override Boolean HasNoApplication => true;

        // Initializes a new instance of the plugin class.
        public LttlabsArticlesPlugin()
        {
            // Initialize the plugin log.
            PluginLog.Init(this.Log);

            // Initialize the plugin resources.
            PluginResources.Init(this.Assembly);
            
            // Initialize the settings manager with this plugin instance
            PluginSettingsManager.Initialize(this);

            this._settingsPreferenceAccount = new PluginPreferenceAccount("article-settings")
            {
                DisplayName = "Article URL Settings",
                Description = "Configure article URLs",
                LoginUrlTitle = "Configure URLs",
                LogoutUrlTitle = "Configure URLs",
                HasLogout = true,
                IsRequired = false
            };

            this.PluginPreferences.Add(this._settingsPreferenceAccount);

            this._settingsPreferenceAccount.LoginRequested += this.OnSettingsPreferenceRequested;
            this._settingsPreferenceAccount.LogoutRequested += this.OnSettingsPreferenceRequested;
        }

        // This method is called when the plugin is loaded.
        public override void Load()
        {
            this._isUnloading = false;

            // Keep account in logged-in state to avoid sign-in prompt in Options+
            this.ReportSettingsPreferenceReady();

            // Start web server for settings UI off the Load() thread
            _ = Task.Run(() =>
            {
                if (this._isUnloading)
                {
                    return;
                }

                SettingsWebServer? startedServer = null;
                try
                {
                    startedServer = new SettingsWebServer();
                    startedServer.SetPlugin(this);
                    startedServer.Start();

                    lock (this._webServerLock)
                    {
                        if (this._isUnloading)
                        {
                            startedServer.Stop();
                            startedServer.Dispose();
                            return;
                        }

                        this.WebServer = startedServer;
                    }
                }
                catch (Exception ex)
                {
                    PluginLog.Error($"Failed to start web server: {ex.Message}");

                    try
                    {
                        startedServer?.Dispose();
                    }
                    catch { }
                }
            });
        }

        // This method is called when the plugin is unloaded.
        public override void Unload()
        {
            this._isUnloading = true;

            // Unsubscribe from preference account requests
            this._settingsPreferenceAccount.LoginRequested -= this.OnSettingsPreferenceRequested;
            this._settingsPreferenceAccount.LogoutRequested -= this.OnSettingsPreferenceRequested;

            // Stop web server
            try
            {
                lock (this._webServerLock)
                {
                    this.WebServer?.Stop();
                    this.WebServer?.Dispose();
                    this.WebServer = null;
                }
            }
            catch (Exception ex)
            {
                PluginLog.Error($"Error stopping web server: {ex.Message}");
            }
        }

        private void OnSettingsPreferenceRequested(Object? sender, EventArgs e)
        {
            this.OpenSettingsUrl();
            this.ReportSettingsPreferenceReady();
        }

        private void OpenSettingsUrl()
        {
            if (this.WebServer != null)
            {
                var url = this.WebServer.SettingsUrl;
                PluginLog.Info($"Opening settings URL: {url}");
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(url) { UseShellExecute = true });
            }
        }

        private void ReportSettingsPreferenceReady() =>
            this._settingsPreferenceAccount.ReportLogin("Articles", "local-settings", null);
    }
}
