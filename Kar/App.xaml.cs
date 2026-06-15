using CefSharp;
using CefSharp.Wpf;
using Kar.HistoryPage;
using System.Windows;

namespace Kar
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : System.Windows.Application
    {

        public static ISearchHistoryRepository HistoryRepo { get; private set; }
        protected override async void OnStartup(StartupEventArgs e)
        {   
            
            base.OnStartup(e);
            CefSettings settings = new CefSettings();
            settings.CefCommandLineArgs.Add("enable_gpu", "1");
            settings.CefCommandLineArgs.Add("enable-gpu-rasterization", "1");
            settings.CefCommandLineArgs.Add("enable-begin-frame-scheduling", "1");

            settings.CefCommandLineArgs.Add("ignore-gpu-blocklist", "1");
            settings.SetOffScreenRenderingBestPerformanceArgs();
            settings.CefCommandLineArgs.Add("enable-webgl", "1");

            CefSharpSettings.ConcurrentTaskExecution = true;

            settings.CefCommandLineArgs.Add("allow-file-access-from-files", "1");
            settings.CefCommandLineArgs.Add("disable-web-security", "1");
            
            Cef.Initialize(settings, performDependencyCheck: true, browserProcessHandler: null);
            SQLitePCL.Batteries.Init();
            
            HistoryRepo = new SqliteSearchHistoryRepository("history.db");
            await HistoryRepo.InitializeAsync();
            
            

            
           
        }
        protected override void OnExit(ExitEventArgs e)
        {
            Cef.Shutdown();
            base.OnExit(e);
        }
    }
}
