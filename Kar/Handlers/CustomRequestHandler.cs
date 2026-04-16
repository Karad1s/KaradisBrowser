using CefSharp;
using System.Security.Cryptography.X509Certificates;
using System.Windows;

namespace Kar.Handlers
{
    internal class CustomRequestHandler: IRequestHandler
    {
        public bool GetAuthCredentials(IWebBrowser chromiumWebBrowser,IBrowser browser,string originUrl,bool isProxy,string host,int port,string realm,string scheme,IAuthCallback callback) => false;
        public IResourceRequestHandler GetResourceRequestHandler(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, IRequest request, bool isNavigation, bool isDownload, string requestInitiator, ref bool disableDefaultHandling) => null;
        public bool OnBeforeBrowse(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, IRequest request, bool userGesture, bool isRedirect)
        {
            if(userGesture && !isRedirect && frame.IsMain) 
            {
                string url = request.Url.ToLower();

                if (!url.Contains("google.com/search") && !url.Contains("duckduckgo.com/?q="))
                {
                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        var mainWindow = System.Windows.Application.Current.MainWindow as MainWindow;
                        if (mainWindow != null)
                        {
                            mainWindow.ViewModel.AddNewTab(request.Url);
                        }
                    });
                    return true;
                }
            }
            return false;
        }
        public bool OnCertificateError(IWebBrowser chromiumWebBrowser, IBrowser browser, CefErrorCode errorCode, string url, ISslInfo sslInfo, IRequestCallback callback) => false;
        public void OnDocumentAvailableInMainFrame(IWebBrowser chromiumWebBrowser, IBrowser browser) { }
        public bool OnOpenUrlFromTab(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, string targetUrl, WindowOpenDisposition targetDisposition, bool userGesture) => false;
        public void OnRenderProcessTerminated(IWebBrowser chromiumWebBrowser, IBrowser browser, CefTerminationStatus status, int errorCode, string errorMessage) { }
        public bool OnSelectClientCertificate(IWebBrowser chromiumWebBrowser, IBrowser browser, bool isProxy, string host, int port, X509Certificate2Collection certificates, ISelectClientCertificateCallback callback) => false;
        public void OnRenderViewReady(IWebBrowser chromiumWebBrowser, IBrowser browser) { }
    }
}
