using System.Windows;
using System.Windows.Input;
using CefSharp;
using CefStruct = CefSharp.Structs;

namespace Kar
{
    public class CustomDisplayHandler : IDisplayHandler
    {
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
        public void OnFaviconUrlChange(IWebBrowser chromiumWebBrowser, IBrowser browser, IList<string> urls) { }
        public void OnLoadingProgressChange(IWebBrowser chromiumWebBrowser, IBrowser browser, double progress) { }
        public bool OnTooltipChanged(IWebBrowser chromiumWebBrowser, ref string text) => false;
        public void OnStatusMessage(IWebBrowser chromiumWebBrowser, StatusMessageEventArgs statusMessageArgs) { }
        public bool OnConsoleMessage(IWebBrowser chromiumWebBrowser, ConsoleMessageEventArgs consoleMessageArgs) => false;
    }
}
