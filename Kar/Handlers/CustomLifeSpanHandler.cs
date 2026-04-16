using CefSharp;
using System.Windows;

namespace Kar.Handlers
{
    public class CustomLifeSpanHandler : ILifeSpanHandler
    {
        public MainViewModel MainViewModel
        {
            get
            {
                var mainWindow = System.Windows.Application.Current.MainWindow as MainWindow;
                return mainWindow.ViewModel;
            }
        }

        public CustomLifeSpanHandler() { }

        public bool OnBeforePopup(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, string targetUrl, string targetFrameName, WindowOpenDisposition targetDisposition, bool userGesture, IPopupFeatures popupFeatures, IWindowInfo windowInfo, IBrowserSettings browserSettings, ref bool noJavascriptAccess, out IWebBrowser newBrowser)
        {
            newBrowser = null;

            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                if (System.Windows.Application.Current.MainWindow is MainWindow mainWindow)
                {
                    if (IsAuthUrl(targetUrl))
                    {
                        mainWindow.OpenPopupInWindow(targetUrl);
                    }
                    else
                    {
                        mainWindow.ViewModel.AddNewTab(targetUrl);
                    }
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
            url = url.ToLower();
            return url.Contains("accounts.google.com") ||
                url.Contains("facebook.com/dialog/oauth") ||
                url.Contains("login") ||
                url.Contains("auth") ||
                url.Contains("oauth") ||
                url.Contains("oauth2");

        }
    } 
}
