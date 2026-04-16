using CefSharp;
using CefSharp.Wpf;
using System.Configuration;
using System.Data;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

namespace Kar
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : System.Windows.Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
           base.OnStartup(e);
            CefSettings settings = new CefSettings();
            settings.CefCommandLineArgs.Add("enable_gpu", "1");
            settings.CefCommandLineArgs.Add("enable-gpu-rasterization", "1");
            settings.CefCommandLineArgs.Add("enable-begin-frame-scheduling", "1");

            settings.CefCommandLineArgs.Add("ignore-gpu-blocklist", "1");
            settings.SetOffScreenRenderingBestPerformanceArgs();
            settings.CefCommandLineArgs.Add("enable-webgl", "1");
            Cef.Initialize(settings, performDependencyCheck: true, browserProcessHandler: null);
        }
        protected override void OnExit(ExitEventArgs e)
        {
            Cef.Shutdown();
            base.OnExit(e);
        }
    }
}
