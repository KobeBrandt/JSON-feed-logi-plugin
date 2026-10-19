namespace Loupedeck.JsonFeedPlugin.Actions
{
    using System;

    internal class OpenSettings : PluginDynamicCommand
    {
        public OpenSettings()
            : base(
                displayName: "Open Feed Settings",
                description: "Open the page where you add and remove JSON feed URLs",
                groupName: "Settings"
            )
        {
        }

        protected override void RunCommand(String actionParameter)
        {
            (this.Plugin as JsonFeedPlugin)?.OpenSettingsUrl();
        }
    }
}
