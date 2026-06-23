namespace Loupedeck.LttlabsArticlesPlugin.Services
{
    using System;
    using System.Globalization;
    using System.IO;
    using System.Net;
    using System.Text;
    using System.Text.Json;
    using System.Threading;
    using Loupedeck;
    using Loupedeck.LttlabsArticlesPlugin.Helpers;
    using Loupedeck.LttlabsArticlesPlugin.Actions;

    public class SettingsWebServer : IDisposable
    {
        private const String ArticleSettingsName = "ArticleSettings";
        private const String NamedUrlSettingsFile = "NamedUrlSettings.json";
        private const Int32 MIN_PORT = 8800;
        private const Int32 MAX_PORT = 8899;
        private const Int32 MAX_RETRIES = 10;

        private HttpListener? _listener;
        private Thread? _listenerThread;
        private readonly Object _lifecycleLock = new Object();
        private Boolean _isRunning;
        private Boolean _isDisposed;
        private Int32 _activePort;
        private static readonly Random _random = new Random();
        private LttlabsArticlesPlugin? _plugin;

        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true
        };

        public String SettingsUrl => "http://localhost:" + this._activePort + "/";

        public void SetPlugin(LttlabsArticlesPlugin plugin)
        {
            this._plugin = plugin;
        }

        public void Start()
        {
            if (this._isDisposed)
            {
                PluginLog.Warning("Cannot start SettingsWebServer after it has been disposed");
                return;
            }

            if (this._isRunning)
            {
                PluginLog.Warning("SettingsWebServer already running");
                return;
            }

            var attemptedPorts = new HashSet<Int32>();
            Exception lastException = null;

            for (var attempt = 0; attempt < MAX_RETRIES; attempt++)
            {
                Int32 port;
                do
                {
                    port = _random.Next(MIN_PORT, MAX_PORT + 1);
                } while (attemptedPorts.Contains(port) && attemptedPorts.Count < (MAX_PORT - MIN_PORT + 1));

                attemptedPorts.Add(port);

                if (this.TryStartServerOnPort(port, out lastException))
                {
                    return;
                }
            }

            var errorMessage = "Failed to start SettingsWebServer after " + MAX_RETRIES + " attempts in port range " + MIN_PORT + "-" + MAX_PORT + ". Last error: " + (lastException?.Message ?? "unknown");
            PluginLog.Error(errorMessage);
        }

        private Boolean TryStartServerOnPort(Int32 port, out Exception exception)
        {
            exception = null;

            try
            {
                var listener = new HttpListener();
                listener.Prefixes.Add("http://localhost:" + port + "/");
                listener.Start();

                lock (this._lifecycleLock)
                {
                    if (this._isDisposed)
                    {
                        listener.Stop();
                        listener.Close();
                        return false;
                    }

                    this._listener = listener;
                    this._activePort = port;
                    this._isRunning = true;
                }

                this._listenerThread = new Thread(this.Listen);
                this._listenerThread.IsBackground = true;
                this._listenerThread.Start();

                PluginLog.Info("SettingsWebServer started on port " + port);
                return true;
            }
            catch (Exception ex)
            {
                exception = ex;
                return false;
            }
        }

        private void Listen()
        {
            while (this._isRunning)
            {
                try
                {
                    var listener = this._listener;
                    if (listener == null)
                    {
                        break;
                    }

                    var context = listener.GetContext();
                    this.QueueRequestHandler(context);
                }
                catch (HttpListenerException)
                {
                    if (this._isRunning)
                    {
                        PluginLog.Warning("HttpListener exception in Listen loop");
                    }
                }
                catch (Exception ex)
                {
                    PluginLog.Error("Error in SettingsWebServer listen loop: " + ex.Message);
                }
            }
        }

        private void QueueRequestHandler(HttpListenerContext context)
        {
            if (!this._isRunning)
            {
                try
                {
                    context.Response.StatusCode = 503;
                    context.Response.Close();
                }
                catch { }
                return;
            }

            ThreadPool.QueueUserWorkItem(_ =>
            {
                try
                {
                    this.HandleRequest(context);
                }
                catch (Exception ex)
                {
                    PluginLog.Error("Error handling request: " + ex.Message);
                }
            });
        }

        private void HandleRequest(HttpListenerContext context)
        {
            try
            {
                var request = context.Request;
                var response = context.Response;

                if (!this.IsLocalRequest(request))
                {
                    this.SendError(response, 403, "Forbidden");
                    return;
                }

                this.AddCorsHeaders(response);

                if (request.HttpMethod == "OPTIONS")
                {
                    response.StatusCode = 200;
                    response.Close();
                    return;
                }

                var path = request.Url?.AbsolutePath ?? "/";

                switch (path)
                {
                    case "/":
                        this.ServeSettingsUI(response);
                        break;
                    case "/api/settings":
                        if (request.HttpMethod == "GET")
                        {
                            this.GetSettings(response);
                        }
                        else if (request.HttpMethod == "POST")
                        {
                            this.SaveSettings(request, response);
                        }
                        else
                        {
                            this.SendError(response, 405, "Method not allowed");
                        }
                        break;
                    default:
                        this.SendError(response, 404, "Not found");
                        break;
                }
            }
            catch (Exception ex)
            {
                PluginLog.Error("Error handling request: " + ex.Message);
                try
                {
                    this.SendError(context.Response, 500, "Internal server error");
                }
                catch { }
            }
        }

        private Boolean IsLocalRequest(HttpListenerRequest request)
        {
            var remoteEndpoint = request.RemoteEndPoint;
            if (remoteEndpoint == null)
            {
                return false;
            }

            var address = remoteEndpoint.Address;
            var isLoopback = IPAddress.IsLoopback(address);
            var isLocalhost = address.Equals(IPAddress.Loopback);
            return isLoopback || isLocalhost;
        }

        private void AddCorsHeaders(HttpListenerResponse response)
        {
            response.Headers.Add("Access-Control-Allow-Origin", "http://localhost");
            response.Headers.Add("Access-Control-Allow-Methods", "GET, POST, OPTIONS");
            response.Headers.Add("Access-Control-Allow-Headers", "Content-Type");
        }

        private String GetSettingsFilePath()
        {
            if (this._plugin != null)
            {
                var pluginDataDir = this._plugin.GetPluginDataDirectory();
                if (IoHelpers.EnsureDirectoryExists(pluginDataDir))
                {
                    return Path.Combine(pluginDataDir, NamedUrlSettingsFile);
                }
            }
            // Fallback to app directory if plugin instance not available
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, NamedUrlSettingsFile);
        }

        private NamedUrlSettings LoadNamedUrlSettings()
        {
            try
            {
                var settingsPath = GetSettingsFilePath();
                if (File.Exists(settingsPath))
                {
                    var json = File.ReadAllText(settingsPath);
                    return JsonSerializer.Deserialize<NamedUrlSettings>(json) ?? new NamedUrlSettings();
                }
            }
            catch (Exception ex)
            {
                PluginLog.Error("Error loading named URL settings: " + ex.Message);
            }
            return new NamedUrlSettings();
        }

        private void SaveNamedUrlSettings(NamedUrlSettings settings)
        {
            try
            {
                var settingsPath = GetSettingsFilePath();
                var json = JsonSerializer.Serialize(settings, JsonOptions);
                File.WriteAllText(settingsPath, json);
                PluginLog.Info("Named URL settings saved to: " + settingsPath);
            }
            catch (Exception ex)
            {
                PluginLog.Error("Error saving named URL settings: " + ex.Message);
            }
        }

        private void ServeSettingsUI(HttpListenerResponse response)
        {
            try
            {
                String html;
                try
                {
                    html = PluginResources.ReadTextResource("Loupedeck.LttlabsArticlesPlugin.Resources.settings-ui.html");
                }
                catch
                {
                    html = PluginResources.ReadTextResource("Loupedeck.LttlabsArticlesPlugin.src.Resources.settings-ui.html");
                }
                this.SendTextResponse(response, html, "text/html");
            }
            catch (Exception ex)
            {
                PluginLog.Error("Error reading settings UI: " + ex.Message);
                this.SendError(response, 500, "Failed to load settings UI");
            }
        }

         private void GetSettings(HttpListenerResponse response)
         {
             try
             {
                 var settings = PluginSettingsManager.GetSetting<ArticleSettings>(ArticleSettingsName, new ArticleSettings());
                 var namedUrlSettings = LoadNamedUrlSettings();
                 this.SendJsonResponse(response, new {
                     articleUrls = settings.ArticleUrls,
                     defaultUrl = settings.DefaultUrl,
                     namedUrls = namedUrlSettings.NamedUrls
                 });
             }
             catch (Exception ex)
             {
                 PluginLog.Error("Error getting settings: " + ex.Message);
                 this.SendError(response, 500, "Failed to load settings");
             }
         }

         private void SaveSettings(HttpListenerRequest request, HttpListenerResponse response)
         {
             try
             {
                 using (var reader = new StreamReader(request.InputStream, request.ContentEncoding))
                 {
                     var json = reader.ReadToEnd();
                     PluginLog.Info("Received settings save request: " + json);
                     var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                     
                     // Deserialize the full settings object
                     var settingsData = JsonDocument.Parse(json);
                     
                     // Save ArticleSettings if present
                     if (settingsData.RootElement.TryGetProperty("articleUrls", out var articleUrlsElement) ||
                         settingsData.RootElement.TryGetProperty("defaultUrl", out _))
                     {
                         var articleSettings = new ArticleSettings();
                         if (settingsData.RootElement.TryGetProperty("articleUrls", out articleUrlsElement))
                         {
                             articleSettings.ArticleUrls = JsonSerializer.Deserialize<List<string>>(articleUrlsElement.GetRawText());
                         }
                         if (settingsData.RootElement.TryGetProperty("defaultUrl", out var defaultUrlElement))
                         {
                             articleSettings.DefaultUrl = defaultUrlElement.GetString();
                         }
                         PluginSettingsManager.SetSetting(ArticleSettingsName, articleSettings);
                     }

                     // Save NamedUrlSettings to JSON file
                     if (settingsData.RootElement.TryGetProperty("namedUrls", out var namedUrlsElement))
                     {
                         var namedUrlSettings = new NamedUrlSettings
                         {
                             NamedUrls = JsonSerializer.Deserialize<List<NamedUrl>>(namedUrlsElement.GetRawText(), options)
                         };
                         SaveNamedUrlSettings(namedUrlSettings);
                         PluginLog.Info("Saved " + namedUrlSettings.NamedUrls.Count + " named URLs");
                     }

                      this.SendJsonResponse(response, new {
                          success = true,
                          message = "Settings saved successfully"
                      });
                      
                      // Reload articles in the dynamic folder
                      try
                      {
                          var listOfArticles = ListOfAtricles.Instance;
                          if (listOfArticles != null)
                          {
                              listOfArticles.ReloadArticles();
                          }
                      }
                      catch (Exception ex)
                      {
                          PluginLog.Warning("Could not reload articles after settings save: " + ex.Message);
                      }
                 }
             }
             catch (Exception ex)
             {
                 PluginLog.Error("Error saving settings: " + ex.Message);
                 this.SendError(response, 500, "Failed to save settings");
             }
         }

        private void SendTextResponse(HttpListenerResponse response, String content, String contentType)
        {
            var bytes = Encoding.UTF8.GetBytes(content);
            response.ContentType = contentType;
            response.ContentLength64 = bytes.Length;
            response.OutputStream.Write(bytes, 0, bytes.Length);
            response.Close();
        }

        private void SendJsonResponse(HttpListenerResponse response, Object data)
        {
            var json = JsonSerializer.Serialize(data, JsonOptions);
            this.SendTextResponse(response, json, "application/json");
        }

        private void SendError(HttpListenerResponse response, Int32 statusCode, String message)
        {
            response.StatusCode = statusCode;
            var errorResponse = new { error = message };
            var json = JsonSerializer.Serialize(errorResponse);
            var bytes = Encoding.UTF8.GetBytes(json);
            response.ContentType = "application/json";
            response.ContentLength64 = bytes.Length;
            response.OutputStream.Write(bytes, 0, bytes.Length);
            response.Close();
        }

        public void Stop()
        {
            HttpListener listenerToStop = null;
            Thread listenerThreadToJoin = null;

            lock (this._lifecycleLock)
            {
                if (!this._isRunning && this._listener == null && this._listenerThread == null)
                {
                    return;
                }

                this._isRunning = false;
                listenerToStop = this._listener;
                listenerThreadToJoin = this._listenerThread;
                this._listener = null;
                this._listenerThread = null;
            }

            try
            {
                listenerToStop?.Stop();
                listenerToStop?.Close();

                if (listenerThreadToJoin != null && !listenerThreadToJoin.Join(1000))
                {
                    PluginLog.Warning("SettingsWebServer listener thread did not exit within timeout");
                }
            }
            catch (Exception ex)
            {
                PluginLog.Error("Error stopping SettingsWebServer: " + ex.Message);
            }
        }

        public void Dispose()
        {
            this._isDisposed = true;
            this.Stop();
        }
    }
}
