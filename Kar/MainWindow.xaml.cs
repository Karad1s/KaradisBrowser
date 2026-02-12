using CefSharp.Wpf;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

namespace Kar
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private WindowState _prevWindowState;
        private WindowStyle _prevWindowStyle;
        private ResizeMode _prevResizeMode;
        private readonly Dictionary<TabViewModel, ChromiumWebBrowser> _browserCache = new Dictionary<TabViewModel, ChromiumWebBrowser>();

        public MainViewModel ViewModel { get; set; }

        public MainWindow()
        {
            ViewModel = new MainViewModel(this);
            InitializeComponent();
            this.DataContext = ViewModel;
            SetupTabManager();
            UpdBrowserUI();
        }


        private void MainWindow_StateChanged(object sender, EventArgs e)
        {
            if (this.WindowState == WindowState.Maximized && !_isManualFullscreen)
            {
                this.MaxHeight = SystemParameters.WorkArea.Height;
                this.MaxWidth = SystemParameters.WorkArea.Width;

                if (RootGrid !=null) RootGrid.Margin = new Thickness(8);
            }

            else if (this.WindowState == WindowState.Maximized && _isManualFullscreen)
            {
                this.MaxHeight = double.PositiveInfinity;
                this.MaxWidth = double.PositiveInfinity;
                if (RootGrid != null) RootGrid.Margin = new Thickness(0);
            }   

            else
            {
                this.MaxHeight = double.PositiveInfinity;
                this.MaxWidth = double.PositiveInfinity;
                if (RootGrid != null) RootGrid.Margin = new Thickness(0);
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e) => this.Close();

        private void MinimizeButton_Click(object sender, RoutedEventArgs e) => this.WindowState = WindowState.Minimized;

        private void MaximizeButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.WindowState == WindowState.Maximized)
            {
                this.WindowState = WindowState.Normal;
            }
            else
            {
                this.WindowState = WindowState.Maximized;
            }
        }

        private bool _isManualFullscreen = false;

        public void SetFullScreen(bool fullscreen)
        {   
            _isManualFullscreen = fullscreen;
            
            if (fullscreen)
            {
                _prevWindowState = WindowState;
                _prevWindowStyle = WindowStyle;
                _prevResizeMode = ResizeMode;

                WindowStyle = WindowStyle.None;
                ResizeMode = ResizeMode.NoResize;

                if (TopRow != null) TopRow.Height = new GridLength(0);
                if (PanelControl != null) PanelControl.Height = new GridLength(0);
                if (TopBar != null) TopBar.Visibility = Visibility.Collapsed;

                if (WindowState == WindowState.Maximized) MainWindow_StateChanged(this, EventArgs.Empty);
                else 
                    WindowState = WindowState.Minimized;

            }
            else
            {
                WindowStyle = _prevWindowStyle;
                ResizeMode = _prevResizeMode;

                if (TopRow != null) TopRow.Height = new GridLength(36);
                if (PanelControl != null) PanelControl.Height = new GridLength(35);
                if (TopBar != null) TopBar.Visibility = Visibility.Visible;

                this.WindowState = _prevWindowState;

                MainWindow_StateChanged(this, EventArgs.Empty);
            }
        }

        private void ChangedSearchSystem_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if(ChangedSearchSystem.SelectedItem is "Google") {}
        }
        private void UrlTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                var textBox = sender as TextBox;
                string query = textBox.Text;

                // Проверяем, является ли ввод ссылкой
                if (!query.Contains(".") || query.Contains(" "))
                {
                    // Если это не ссылка, выполняем поиск через выбранную систему
                    if (ViewModel.SelectedSearchSystem != null && ViewModel.SelectedTab != null)
                    {
                        // Формируем полную ссылку: Базовый URL + запрос
                        string searchUrl = ViewModel.SelectedSearchSystem.Url + Uri.EscapeDataString(query);
                        ViewModel.SelectedTab.Url = searchUrl;
                    }
                }
            }
        }

        private void SetupTabManager()
        {
            ViewModel.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(ViewModel.SelectedTab)){
                    UpdBrowserUI();
                }

            };
        }
        private void UpdBrowserUI()
        {
            var selectedTab = ViewModel.SelectedTab;
            if (selectedTab == null) return;

            if (!_browserCache.ContainsKey(selectedTab))
            {
                var newBrowser = new ChromiumWebBrowser();
                Binding myBinding = new Binding("Url")
                {
                    Source = selectedTab,
                    NotifyOnTargetUpdated = true,
                    Mode = BindingMode.TwoWay,
                };
                
                newBrowser.SetBinding(ChromiumWebBrowser.AddressProperty, myBinding);
                
                _browserCache[selectedTab] = newBrowser;
            }

            var activeBrowser = _browserCache[selectedTab];

            if(BrowserHost.Children.Count == 0 || BrowserHost.Children[0] != activeBrowser)
            {
                BrowserHost.Children.Clear();
                BrowserHost.Children.Add(activeBrowser);
            }
        }
    }

    

    public class TabSelectionConverter : IMultiValueConverter
    {
       public object Convert(object[] values, Type targetType,object parameter, CultureInfo culture)
        {
            if(values.Length <2) return false;
            return values[0] == values[1];
        }
        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class SearchSystem
    {
        public string Name { get; set; } = "";
        public string Url { get; set; } = "";
        public SearchSystem(string name, string url)
        {
            Name = name;
            Url = url;
        }
    }
}
       