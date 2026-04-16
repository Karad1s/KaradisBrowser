using CefSharp;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Kar.Handlers;



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

            Browser.LifeSpanHandler = new CustomLifeSpanHandler();
            this.DataContextChanged += (s, e) =>
            {
                if (DataContext is TabViewModel viewModel)
                {
                    viewModel.Browser = this.Browser;
                    Browser.DisplayHandler = new CustomDisplayHandler(viewModel, Dispatcher);
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

    }
}
