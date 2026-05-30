using System;
using CefSharp;

namespace Kar.Handlers
{
    public class CustomLifeSpanHandler : ILifeSpanHandler
    {
        private readonly Action<string> _onOpenTab;
        private readonly Action<string> _onOpenPopup;

        public CustomLifeSpanHandler(Action<string> onOpenTab, Action<string> onOpenPopup)
        {
            _onOpenTab = onOpenTab ?? throw new ArgumentNullException(nameof(onOpenTab));
            _onOpenPopup = onOpenPopup ?? throw new ArgumentNullException(nameof(onOpenPopup));
        }

        public bool OnBeforePopup(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, string targetUrl, string targetFrameName, WindowOpenDisposition targetDisposition, bool userGesture, IPopupFeatures popupFeatures, IWindowInfo windowInfo, IBrowserSettings browserSettings, ref bool noJavascriptAccess, out IWebBrowser newBrowser)
        {
            newBrowser = null;

            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                if (IsAuthUrl(targetUrl))
                {
                    _onOpenPopup(targetUrl);
                }
                else
                {
                    _onOpenTab(targetUrl);
                }
            });
            return true;
        }

        public void OnAfterCreated(IWebBrowser chromiumWebBrowser, IBrowser browser) { }

        public bool DoClose(IWebBrowser chromiumWebBrowser, IBrowser browser) => false;

        public void OnBeforeClose(IWebBrowser chromiumWebBrowser, IBrowser browser) { }

        private bool IsAuthUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return false;

            try
            {
                Uri uri = new Uri(url);
                string host = uri.Host.ToLower();
                string pathAndQuery = uri.PathAndQuery.ToLower();

                // 1. Check for universal OAuth2 query parameters
                foreach (var parameter in BrowserConfig.OAuthParameters)
                {
                    if (pathAndQuery.Contains(parameter.ToLower()))
                    {
                        return true;
                    }
                }

                // 2. Check for configured auth domains
                foreach (var authDomain in BrowserConfig.AuthDomains)
                {
                    if (host.Contains(authDomain.ToLower()))
                    {
                        return true;
                    }
                }

                // 3. Check for configured auth keywords in path
                string path = uri.AbsolutePath.ToLower();
                foreach (var keyword in BrowserConfig.AuthKeywords)
                {
                    if (path.Contains(keyword.ToLower()))
                    {
                        return true;
                    }
                }
            }
            catch
            {
                // Fallback in case of malformed URL
                string lowerUrl = url.ToLower();
                foreach (var parameter in BrowserConfig.OAuthParameters)
                {
                    if (lowerUrl.Contains(parameter.ToLower())) return true;
                }
                foreach (var authDomain in BrowserConfig.AuthDomains)
                {
                    if (lowerUrl.Contains(authDomain.ToLower())) return true;
                }
                foreach (var keyword in BrowserConfig.AuthKeywords)
                {
                    if (lowerUrl.Contains(keyword.ToLower())) return true;
                }
            }

            return false;
        }
    } 
}
