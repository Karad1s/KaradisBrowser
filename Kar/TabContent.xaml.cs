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
    public partial class TabContent : System.Windows.Controls.UserControl
    {
        public TabContent()
        {
            InitializeComponent();
            
            Browser.JavascriptObjectRepository.Register("historyBridge", new HistoryBridge(), options: BindingOptions.DefaultBinder);

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

            Browser.RequestHandler = new CustomRequestHandler(url =>
            {
                var mainWin = Window.GetWindow(this) as MainWindow;
                mainWin?.ViewModel.AddNewTab(url);
            });

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

        public string Url
        {
            get { return (string)GetValue(UrlProperty); }
            set { SetValue(UrlProperty, value); }
        }

        public string Title
        {
            get { return (string)GetValue(TitleProperty); }
            set { SetValue(TitleProperty, value); }
        }

        public static readonly DependencyProperty UrlProperty =
            DependencyProperty.Register("Url", typeof(string), typeof(TabContent), new PropertyMetadata("https://www.google.com"));

        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register("Title", typeof(string), typeof(TabContent), new PropertyMetadata("Новая вкладка"));

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

        private async void Browser_FrameLoadEnd(object sender, FrameLoadEndEventArgs e)
        {
            if (!e.Frame.IsMain) return;

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
