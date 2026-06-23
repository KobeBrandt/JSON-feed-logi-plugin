namespace Loupedeck.LttlabsArticlesPlugin.Helpers
{
    using System;
    using System.Collections.Generic;
    using System.Text.Json;

    public static class PluginSettingsManager
    {
        private static Plugin _pluginInstance;

        public static Boolean IsInitialized => _pluginInstance != null;

        public static void Initialize(Plugin plugin) => _pluginInstance = plugin ?? throw new ArgumentNullException(nameof(plugin));

        public static T GetSetting<T>(String settingName, T defaultValue = default)
        {
            if (_pluginInstance == null)
            {
                throw new InvalidOperationException("PluginSettingsManager not initialized. Call Initialize() first.");
            }

            try
            {
                if (_pluginInstance.TryGetPluginSetting(settingName, out var settingValue) && !String.IsNullOrEmpty(settingValue))
                {
                    var parsed = JsonSerializer.Deserialize<T>(settingValue);
                    if (parsed != null)
                    {
                        return parsed;
                    }
                }
            }
            catch (Exception ex)
            {
                PluginLog.Warning($"Failed to deserialize setting '{settingName}': {ex.Message}");
            }

            return defaultValue;
        }

        public static void SetSetting<T>(String settingName, T value, Boolean backupOnline = true)
        {
            if (_pluginInstance == null)
            {
                throw new InvalidOperationException("PluginSettingsManager not initialized. Call Initialize() first.");
            }

            try
            {
                var jsonValue = JsonSerializer.Serialize(value);
                _pluginInstance.SetPluginSetting(settingName, jsonValue, backupOnline);
            }
            catch (Exception ex)
            {
                PluginLog.Error($"Failed to serialize and save setting '{settingName}': {ex.Message}");
                throw;
            }
        }

        public static String[] ListAllSettings()
        {
            return _pluginInstance == null
                ? throw new InvalidOperationException("PluginSettingsManager not initialized. Call Initialize() first.")
                : _pluginInstance.ListPluginSettings();
        }
    }
}
