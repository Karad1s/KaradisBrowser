using CefSharp;
using System;
using System.Windows;


namespace Kar.Handlers
{
    public class CustomDownloadHandler : IDownloadHandler
    {
        public event EventHandler<DownloadItem> OnBeforeDownloadFired;
        public bool CanDownload(IWebBrowser chromiumWebBrowser, IBrowser browser, string url, string requestMethod)
        {
            return true;
        }

        public bool OnBeforeDownload(IWebBrowser chromiumWebBrowser, IBrowser browser, DownloadItem downloadItem, IBeforeDownloadCallback callback)
        {
            if (!callback.IsDisposed)
            {
                callback.Continue(downloadItem.SuggestedFileName, showDialog: true);
            }
            return true;
        }

       public void OnDownloadUpdated(IWebBrowser chromiumWebBrowser, IBrowser browser, DownloadItem downloadItem, IDownloadItemCallback callback)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                OnBeforeDownloadFired?.Invoke(this, downloadItem);
            });
        }
    }
}
