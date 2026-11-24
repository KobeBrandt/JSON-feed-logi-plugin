# JSON Feed plugin for Logi Actions

A plugin for Logitech and Loupedeck devices that shows the latest articles from one or more
[JSON Feed](https://www.jsonfeed.org/) sources. Each article is a button. Press a button to open
the article in your browser.

## Features

- Combine articles from several JSON feeds into one list, newest first.
- Manage your feeds on a local settings page that opens in your browser.
- Only `http://` and `https://` links are opened. Other links in a feed are ignored.

## Requirements

- Logi Options+ or Loupedeck software with Logi Plugin Service 6.4 or later.
- A supported device (Loupedeck CT, Live, Live S, Razer Stream Controller, Razer Stream Controller X).

## Install

1. Download the latest `.lplug4` file from the [Releases](https://github.com/KobeBrandt/JSON-feed-logi-plugin/releases) page.
2. Open the file. Logi Plugin Service installs the plugin.

## Use

1. Add the **JSON Feed** folder to your device.
2. Run the **Open Feed Settings** action, or open the plugin settings in Logi Options+.
3. On the settings page, select **Add feed**, enter a name and the feed URL, then select **Save settings**.
4. Open the **JSON Feed** folder on your device and press an article to open it.

## Settings page security

The settings page runs on a local web server (`http://localhost`, port 8800-8899).

- The plugin opens the page with a random token that changes every session. The settings API
  refuses requests without this token.
- The API refuses requests from other web origins and saves that are not `application/json`.
- The plugin saves and opens only `http://` and `https://` URLs.

If the page says it cannot load your settings, open it again from the plugin.

## Build from source

Requirements:

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- Logi Plugin Service installed (the build references its `PluginApi.dll`)
- LogiPluginTool: `dotnet tool install --global LogiPluginTool`

Build and load into the local Logi Plugin Service:

```powershell
dotnet build JsonFeedPlugin.sln
```

Create and check a package:

```powershell
dotnet build JsonFeedPlugin.sln -c Release
logiplugintool pack bin\Release JsonFeed.lplug4
logiplugintool verify JsonFeed.lplug4
```

## Support

Report bugs and ideas on the [issues page](https://github.com/KobeBrandt/JSON-feed-logi-plugin/issues).

## License

[MIT](LICENSE)
