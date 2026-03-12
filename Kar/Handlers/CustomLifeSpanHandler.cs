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
                var mainWindow = Application.Current.MainWindow as MainWindow;
                return mainWindow.ViewModel;
            }
        }
        public bool OnBeforePopup(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, string targetUrl, string targetFrameName, WindowOpenDisposition targetDisposition, bool userGesture, IPopupFeatures popupFeatures, IWindowInfo windowInfo, IBrowserSettings browserSettings, ref bool noJavascriptAccess, out IWebBrowser newBrowser)
        {
            newBrowser = null;

            bool isAuth = targetUrl.Contains("accounts.google.com") ||
                  targetUrl.Contains("facebook.com/dialog/oauth") ||
                  targetUrl.Contains("login") ||
                  targetUrl.Contains("auth");

            if (!isAuth)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    var mainWindow = Application.Current.MainWindow as MainWindow;

                    if (mainWindow != null)
                    {
                        mainWindow.ViewModel.AddNewTab(targetUrl);
                    }
                });
                 return true;
            }
            
            

            return false;
        }

        public void OnAfterCreated(IWebBrowser chromiumWebBrowser, IBrowser browser) { }

        public bool DoClose(IWebBrowser chromiumWebBrowser, IBrowser browser) => false;

        public void OnBeforeClose(IWebBrowser chromiumWebBrowser, IBrowser browser) { }
    }
}