using System;
using System.Windows;
using System.Windows.Controls;
using CefSharp;
using Kar.Handlers;
using Kar.HistoryPage;

namespace Kar
{
    /// <summary>
    /// Логика взаимодействия для TabContent.xaml
    /// </summary>
    /// <summary>
    /// Логика взаимодействия для TabContent.xaml.
    /// Представляет собой визуальный контейнер вкладки, содержащий экземпляр ChromiumWebBrowser.
    /// </summary>
    public partial class TabContent : System.Windows.Controls.UserControl
    {
        /// <summary>
        /// Конструктор компонента. Настраивает интеграцию с JavaScript, 
        /// регистрирует обработчики событий жизненного цикла браузера (LifeSpan, Request)
        /// и связывает контекст данных (DataContext) с операциями браузера.
        /// </summary>
        public TabContent()
        {
            InitializeComponent();
            
            // Регистрация моста истории для доступа из JS кода страницы истории
            Browser.JavascriptObjectRepository.Register("historyBridge", new HistoryBridge(), options: BindingOptions.DefaultBinder);

            // Обработка всплывающих окон и новых вкладок
            Browser.LifeSpanHandler = new CustomLifeSpanHandler(
                url => {
                    var mainWin = Window.GetWindow(this) as MainWindow;
                    mainWin?.ViewModel.AddNewTab(url);
                },
                url => {
                    var mainWin = Window.GetWindow(this) as MainWindow;
                    mainWin?.OpenPopupInWindow(url);
                }
            );

            // Обработка внешних запросов на переход
            Browser.RequestHandler = new CustomRequestHandler(url =>
            {
                var mainWin = Window.GetWindow(this) as MainWindow;
                mainWin?.ViewModel.AddNewTab(url);
            });

            // Инициализация при смене DataContext (подвязка вью-модели)
            this.DataContextChanged += (s, e) =>
            {
                if (DataContext is TabViewModel viewModel)
                {
                    var browserOps = new CefSharpBrowserOperations(this.Browser);
                    viewModel.BrowserOperations = browserOps;

                    Browser.DisplayHandler = new CustomDisplayHandler(viewModel, Dispatcher, fullscreen =>
                    {
                        var mainWin = Window.GetWindow(this) as MainWindow;
                        mainWin?.ToggleFullScreen(fullscreen);
                    });
                }
            };
        }

        /// <summary>
        /// Свойство зависимости для URL-адреса текущей вкладки.
        /// </summary>
        public string Url
        {
            get { return (string)GetValue(UrlProperty); }
            set { SetValue(UrlProperty, value); }
        }

        /// <summary>
        /// Свойство зависимости для заголовка текущей вкладки.
        /// </summary>
        public string Title
        {
            get { return (string)GetValue(TitleProperty); }
            set { SetValue(TitleProperty, value); }
        }

        /// <summary>
        /// Регистрация свойства зависимости Url.
        /// </summary>
        public static readonly DependencyProperty UrlProperty =
            DependencyProperty.Register("Url", typeof(string), typeof(TabContent), new PropertyMetadata("https://www.google.com"));

        /// <summary>
        /// Регистрация свойства зависимости Title.
        /// </summary>
        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register("Title", typeof(string), typeof(TabContent), new PropertyMetadata("Новая вкладка"));

        /// <summary>
        /// Вызывается после инициализации экземпляра ChromiumWebBrowser.
        /// Задает начальный адрес и настраивает синхронизацию адреса и заголовка с вью-моделью.
        /// </summary>
        private void Browser_Initialized(object sender, EventArgs e)
        {
            Browser.Address = Url;

            Browser.AddressChanged += (s, args) =>
            {
                Dispatcher.Invoke(() =>
                {
                    Url = args.NewValue.ToString();
                });
            };

            Browser.TitleChanged += (s, args) =>
            {
                Dispatcher.Invoke(() =>
                {
                    Title = args.NewValue.ToString();
                });
            };
        }

        /// <summary>
        /// Вызывается по окончании загрузки фрейма страницы.
        /// Используется для асинхронного сохранения успешно загруженных страниц в базу данных истории SQLite.
        /// </summary>
        private async void Browser_FrameLoadEnd(object sender, FrameLoadEndEventArgs e)
        {
            // Сохраняем только главную страницу (не фреймы рекламы/виджетов)
            if (!e.Frame.IsMain) return;

            // Исключаем служебные адреса
            if (e.Url.StartsWith("devtols://") || string.IsNullOrEmpty(e.Url)) return;

            var currentBrowser = (CefSharp.Wpf.ChromiumWebBrowser)sender;

            string title = currentBrowser.Title;
            string url = e.Url;

            if(App.HistoryRepo != null)
            {
                await App.HistoryRepo.SaveQueryAsync(url, title); 
            }
        }
    }
}
