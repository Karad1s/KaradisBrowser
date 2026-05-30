using System;
using System.Windows;
using CefSharp;
using CefSharp.Wpf;

namespace Kar
{
    public class CefSharpBrowserOperations : IBrowserOperations, IDisposable
    {
        private readonly ChromiumWebBrowser _webBrowser;

        public CefSharpBrowserOperations(ChromiumWebBrowser webBrowser)
        {
            _webBrowser = webBrowser ?? throw new ArgumentNullException(nameof(webBrowser));
            _webBrowser.AddressChanged += OnCefAddressChanged;
        }

        private void OnCefAddressChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue is string address)
            {
                AddressChanged?.Invoke(this, address);
            }
        }

        public bool CanGoBack => _webBrowser.CanGoBack;
        public bool CanGoForward => _webBrowser.CanGoForward;
        public void GoBack() => _webBrowser.Back();
        public void GoForward() => _webBrowser.Forward();
        public void Reload() => _webBrowser.Reload();

        public event EventHandler<string>? AddressChanged;

        public void Dispose()
        {
            _webBrowser.AddressChanged -= OnCefAddressChanged;
        }
    }
}
