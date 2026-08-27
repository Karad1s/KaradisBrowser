using System;
using System.IO;
using System.Text.Json;
using System.Linq;
using CefSharp;
using CefSharp.Wpf;

namespace Kar.Homepage
{

    public class HomeBridgeCStoJS
    {
        private readonly string _notesDirectory;
        private readonly ChromiumWebBrowser _browser;

        private readonly NetworkSpeedMonitor _networkSpeedMonitor;

        public HomeBridgeCStoJS(ChromiumWebBrowser browser)
        {
            _browser = browser;

            _notesDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "home");

            if (!Directory.Exists(_notesDirectory))
            {
                Directory.CreateDirectory(_notesDirectory);
            }
            _networkSpeedMonitor = new NetworkSpeedMonitor();
            _networkSpeedMonitor.SpeedUpd += OnSpeedUpdated;
        }
        public void StartNetworkMonitor()
        {
            _networkSpeedMonitor.Start();
        }
        public void StopNetworkMonitor()
        {
            _networkSpeedMonitor.Stop();
        }
        private void OnSpeedUpdated(double download, double upload)
        {
            string script = $@"updateNetworkSpeed({download},{upload});";

            _browser.Dispatcher.Invoke(() =>
            {
                _browser.ExecuteScriptAsync(script);
            });
        }
    }
}