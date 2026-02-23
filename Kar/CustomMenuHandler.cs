using CefSharp;
using System;
using System.Collections.Generic;
using System.Text;

namespace Kar
{
    internal class CustomMenuHandler: IContextMenuHandler
    {
        public void OnBeforeContextMenu(IWebBrowser browserControl, IBrowser browser, IFrame frame, IContextMenuParams parameters, IMenuModel model)
        {
            model.AddItem((CefMenuCommand)26501, "Посмотреть код");
        }
        public bool OnContextMenuCommand(IWebBrowser browserControl,IBrowser browser,IFrame frame,IContextMenuParams parameters,CefMenuCommand commandId,CefEventFlags eventFlags)
        {
            if ((int)commandId == 26501)
            {
                browser.GetHost().ShowDevTools();
                return true;
            }
            return false;
        }
        public void OnContextMenuDismissed(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame) { }
        public bool RunContextMenu(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, IContextMenuParams parameters, IMenuModel model, IRunContextMenuCallback callback) => false;
    }
    
}
