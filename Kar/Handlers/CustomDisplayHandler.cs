using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using CefSharp;
using CefStruct = CefSharp.Structs;

namespace Kar.Handlers
{
    public class CustomDisplayHandler : IDisplayHandler
    {
        private readonly TabViewModel _tab;
        private readonly Dispatcher _dispatcher;

        public CustomDisplayHandler(TabViewModel tab, Dispatcher dispatcher)
        {
            _tab = tab;
            _dispatcher = dispatcher;
        }

        public void OnFullscreenModeChange(IWebBrowser chromiumWebBrowser, IBrowser browser, bool fullscreen)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {

                var mainWin = Application.Current.MainWindow as MainWindow;
                if (mainWin != null)
                {
                    mainWin.SetFullScreen(fullscreen);
                }
            });
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
