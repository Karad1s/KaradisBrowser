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

        /// <summary>
        /// Репозиторий для хранения и управления историей поиска и посещений.
        /// </summary>
        public static ISearchHistoryRepository HistoryRepo { get; private set; }

        /// <summary>
        /// Вызывается при запуске приложения. Инициализирует CefSharp (Chromium Embedded Framework),
        /// базу данных истории SQLite и настраивает базовые параметры рендеринга.
        /// </summary>
        /// <param name="e">Аргументы события запуска.</param>
        protected override async void OnStartup(StartupEventArgs e)
        {   
            base.OnStartup(e);

            // Настройка параметров CefSettings для оптимизации производительности
            CefSettings settings = new CefSettings();
            
            // Включение аппаратного ускорения GPU и растеризации
            settings.CefCommandLineArgs.Add("enable_gpu", "1");
            settings.CefCommandLineArgs.Add("enable-gpu-rasterization", "1");
            settings.CefCommandLineArgs.Add("enable-begin-frame-scheduling", "1");
            settings.CefCommandLineArgs.Add("ignore-gpu-blocklist", "1");
            settings.SetOffScreenRenderingBestPerformanceArgs();
            settings.CefCommandLineArgs.Add("enable-webgl", "1");

            // Разрешение выполнения нескольких задач CEF в конкурентном режиме
            CefSharpSettings.ConcurrentTaskExecution = true;

            // Разрешение кросс-доменного доступа к локальным файлам (необходимо для работы страниц настроек/истории)
            settings.CefCommandLineArgs.Add("allow-file-access-from-files", "1");
            settings.CefCommandLineArgs.Add("disable-web-security", "1");
            
            // Инициализация инфраструктуры CEF
            Cef.Initialize(settings, performDependencyCheck: true, browserProcessHandler: null);
            
            // Инициализация SQLite провайдера базы данных
            SQLitePCL.Batteries.Init();
            
            // Создание и асинхронная инициализация таблицы истории
            HistoryRepo = new SqliteSearchHistoryRepository("history.db");
            await HistoryRepo.InitializeAsync();
        }

        /// <summary>
        /// Вызывается перед выходом из приложения. Корректно завершает работу CEF.
        /// </summary>
        /// <param name="e">Аргументы события выхода.</param>
        protected override void OnExit(ExitEventArgs e)
        {
            // Корректное освобождение ресурсов и завершение процессов CefSharp
            Cef.Shutdown();
            base.OnExit(e);
        }
    }
}
