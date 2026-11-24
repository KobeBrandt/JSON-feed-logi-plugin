namespace Loupedeck.JsonFeedPlugin.Helpers
{
    using System;

    // Feed and article links come from outside the plugin. Only http and https links are
    // allowed, so a link can never start a local program, a file or another app.
    public static class WebUrl
    {
        public static Boolean IsValid(String url)
        {
            return Uri.TryCreate(url, UriKind.Absolute, out var uri)
                && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
        }
    }
}
