using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Threading;
using CefSharp;
using CefStruct = CefSharp.Structs;

namespace Kar.Handlers
{
    public class CustomDisplayHandler : IDisplayHandler
    {
        private readonly TabViewModel _tab;
        private readonly Dispatcher _dispatcher;
        private readonly Action<bool>? _onFullscreenModeChange;

        public CustomDisplayHandler(TabViewModel tab, Dispatcher dispatcher, Action<bool>? onFullscreenModeChange = null)
        {
            _tab = tab ?? throw new ArgumentNullException(nameof(tab));
            _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
            _onFullscreenModeChange = onFullscreenModeChange;
        }

        public void OnFullscreenModeChange(IWebBrowser chromiumWebBrowser, IBrowser browser, bool fullscreen)
        {
            if (_onFullscreenModeChange != null)
            {
                _dispatcher.Invoke(() => _onFullscreenModeChange(fullscreen));
            }
        }

        public void OnAddressChanged(IWebBrowser chromiumWebBrowser, AddressChangedEventArgs addressChangedArgs) { }
        public bool OnAutoResize(IWebBrowser chromiumWebBrowser, IBrowser browser, CefStruct.Size newSize) => false;
        public bool OnCursorChange(IWebBrowser chromiumWebBrowser, IBrowser browser, nint cursor, CefSharp.Enums.CursorType type, CefStruct.CursorInfo customCursorInfo) => false;

        public void OnTitleChanged(IWebBrowser chromiumWebBrowser, TitleChangedEventArgs titleChangedArgs) { }
        public void OnFaviconUrlChange(IWebBrowser chromiumWebBrowser, IBrowser browser, IList<string> urls) 
        {
            if(urls.Count > 0) 
            {
                _dispatcher.Invoke(() =>
                {
                    string bestIcon = urls.FirstOrDefault(u => u.ToLower().Contains(".png")) ?? urls[0];

                    _tab.Favicon = bestIcon;
                });
            }
        }
        public void OnLoadingProgressChange(IWebBrowser chromiumWebBrowser, IBrowser browser, double progress) { }
        public bool OnTooltipChanged(IWebBrowser chromiumWebBrowser, ref string text) => false;
        public void OnStatusMessage(IWebBrowser chromiumWebBrowser, StatusMessageEventArgs statusMessageArgs) { }
        public bool OnConsoleMessage(IWebBrowser chromiumWebBrowser, ConsoleMessageEventArgs consoleMessageArgs) => false;
    }
}
