using CefSharp;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;


namespace Kar
{
    /// <summary>
    /// Логика взаимодействия для TabContent.xaml
    /// </summary>
    public partial class TabContent : UserControl
    {
        public TabContent()
        {
            InitializeComponent();
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

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            if (Browser.CanGoBack)
            {
                Browser.Back();
            }
        }
        private void ForwardButton_Click(object sender, RoutedEventArgs e)
        {
            Browser.Forward();
        }
        private void ReloadButton_Click(object sender, RoutedEventArgs e)
        {
            Browser.Reload();
        }
        private void HomeButton_Click(object sender, RoutedEventArgs e)
        {
            Browser.Load("https://www.google.com");
        }

        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            
        }

        private void SearchBox_KeyDown(object sender, KeyEventArgs e)
        {

                Search(e);

        }

        private void ExntensionButton_Click(object sender, RoutedEventArgs e)
        {

        }
        private void FavoriteButton_Click(object sender, RoutedEventArgs e)
        {

        }
        private void DownloadButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void MoreButton_Click(object sender, RoutedEventArgs e)
        {
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {

        }

        private void Search(KeyEventArgs e)
        {
            if(e.Key == Key.Enter)
            {
                string query = SearchBox.Text;
                if (query.StartsWith("http")) Browser.Load(query);
                else Browser.Load("https://www.google.com/search?q=" + Uri.EscapeDataString(query));
            }
            
        }

    }
}
