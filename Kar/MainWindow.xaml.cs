using CefSharp;
using CefSharp.Wpf;
using System.Globalization; 
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using Kar.Handlers;
using CefSharp.Fluent;

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
            InitializeComponent();

            if (System.ComponentModel.DesignerProperties.GetIsInDesignMode(this))
            {
                return;
            }

            ViewModel = new MainViewModel(this);

            ViewModel.Tabs.CollectionChanged += (s, e) =>
            {
                if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Remove)
                {
                    foreach (TabViewModel oldTab in e.OldItems)
                    {
                        DoDelCache(oldTab);
                    }
                }
            };
            
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

                if (RootGrid != null) RootGrid.Margin = new Thickness(8);
            }

            else
            {
                this.MaxHeight = double.PositiveInfinity;
                this.MaxWidth = double.PositiveInfinity;
                if (RootGrid != null) RootGrid.Margin = new Thickness(0);
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Cef.Shutdown();
            Close();
        }

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

        private void ChangedSearchSystem_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ChangedSearchSystem.SelectedItem is "Google") { }
        }

        private void SuggestionList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SuggestionList.SelectedItem is string selected)
            {
                UrlTextBox.Text = selected;
                SuggestionPopup.IsOpen = false;

                MapsToUrl(selected);
            }
        }

        private void MapsToUrl(string query)
        {
            if (string.IsNullOrWhiteSpace(query)) return;

            if (ViewModel.SelectedTab != null)
            {
                string currentSearchEngine = ViewModel.SelectedTab.CurrentSearchEngine ?? ViewModel.GlobalSearchEngine;
                string finalUrl = ViewModel.FormatSearchQuery(query, currentSearchEngine);

                ViewModel.SelectedTab.Url = finalUrl;
            }
        }
        private async void UrlTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string query = UrlTextBox.Text;
            if (query.Length > 2 && !query.StartsWith("http"))
            {
                var suggestions = await GetSearchSuggestions(query);

                if (suggestions.Any())
                {
                    SuggestionList.ItemsSource = suggestions;
                    SuggestionPopup.PlacementTarget = UrlTextBox;
                    SuggestionPopup.IsOpen = true;
                }
                else
                {
                    SuggestionPopup.IsOpen = false;
                }
            }
            else
            {
                SuggestionPopup.IsOpen = false;
            }
        }

        private async void UrlTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                e.Handled = true;
                string userInput = UrlTextBox.Text;
                string currentTabEngine = ViewModel.SelectedTab?.CurrentSearchEngine ?? "Google";
                MessageBox.Show($"Для вкладки выбран: '{currentTabEngine}'\nГлобально: '{ViewModel.GlobalSearchEngine}'");

                string currentSearchEngine = ViewModel.SelectedTab?.CurrentSearchEngine ?? "Google";
                string finalUrl = ViewModel.FormatSearchQuery(userInput, currentSearchEngine);

                if (ViewModel.SelectedTab != null) ViewModel.SelectedTab.Url = finalUrl;

                Keyboard.ClearFocus();
            }
        }

        private async Task<List<string>> GetSearchSuggestions(string query)
        {
            try
            {
                using (var client = new System.Net.Http.HttpClient())
                {
                    string url = $"http://suggestqueries.google.com/complete/search?client=firefox&q={Uri.EscapeDataString(query)}";
                    var response = await client.GetStringAsync(url);

                    using (JsonDocument json = JsonDocument.Parse(response))
                    {
                        // Исправлено CS0021
                        return json.RootElement[1]
                            .EnumerateArray()
                            .Select(x => x.GetString())
                            .ToList();
                    }
                }
            }
            catch { return new List<string>(); }
        }
        private void SetupTabManager()
        {
            ViewModel.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(ViewModel.SelectedTab))
                {
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
                var downloadHandler = new CustomDownloadHandler();
                selectedTab.Browser = newBrowser;
                downloadHandler.DownloadStateChanged += OnDownloadStateChanged;
                newBrowser.MenuHandler = new CustomMenuHandler();
                newBrowser.LifeSpanHandler = new CustomLifeSpanHandler();
                newBrowser.RequestHandler = new CustomRequestHandler();
                newBrowser.DownloadHandler = downloadHandler;


                string SettingsPath = System.IO.Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "Settings", "settings.json");
                var settingsService = new Settings.FileSettingsService(SettingsPath);
                var settingsBridge = new Settings.SettingsBridge(settingsService);

                newBrowser.JavascriptObjectRepository.Settings.LegacyBindingEnabled = true;
                newBrowser.JavascriptObjectRepository.Register("SettingsHandler", new SettingBridge(), options: BindingOptions.DefaultBinder);
                newBrowser.JavascriptObjectRepository.Register("csharpSettingsBridge", ViewModel.AppSettingsBridge, options: BindingOptions.DefaultBinder);

                newBrowser.TitleChanged += (s, args) =>
                {
                    Dispatcher.Invoke(() =>
                    {
                        selectedTab.Title = args.NewValue.ToString() ?? "Загрузка...";
                    });
                };

                newBrowser.DisplayHandler = new CustomDisplayHandler(selectedTab, Dispatcher);

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

            if (BrowserHost.Children.Count == 0 || BrowserHost.Children[0] != activeBrowser)
            {
                BrowserHost.Children.Clear();
                BrowserHost.Children.Add(activeBrowser);
            }


        }
        private void DoDelCache(TabViewModel tab)
        {

            if (tab == null || !_browserCache.ContainsKey(tab)) return;

            var browser = _browserCache[tab];
            BindingOperations.ClearAllBindings(browser);

            if (BrowserHost.Children.Contains(browser))
            {
                BrowserHost.Children.Clear();
            }

            browser.Dispose();

            _browserCache.Remove(tab);
        }

        public void ShowDevTools()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                var window = (MainWindow)Application.Current.MainWindow;
                var selectedTab = window.ViewModel.SelectedTab;

                if (selectedTab != null && window._browserCache.TryGetValue(selectedTab, out var browser))
                {
                    var windowInfo = new WindowInfo();

                    var helper = new System.Windows.Interop.WindowInteropHelper(window);
                    IntPtr hostHandle = helper.Handle;

                    windowInfo.SetAsChild(hostHandle, (int)browser.ActualWidth - 500, 0, (int)browser.ActualWidth, (int)browser.ActualHeight);

                    browser.ShowDevTools(windowInfo);
                }
            });
        }

        public void OpenPopupInWindow(string url)
        {
            var popupBrowser = new ChromiumWebBrowser(url);

            var popupWindow = new Window
            {
                Title = "Авторизация",
                Content = popupBrowser,
                Width = 600,
                Height = 700,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                Owner = this
            };

            popupWindow.Closed += (s, e) =>
            {
                popupBrowser.Dispose();
            };

            popupWindow.Show();
        }

        private void OnDownloadStateChanged(object sender, DownloadItem e)
        {
            var downloads = ViewModel.RecentDownloads;

            var existingItem = downloads.FirstOrDefault(d => d.Id == e.Id);

            if (existingItem != null)
            {
                existingItem.Update(e);
            }
            else
            {
                var newItem = new DownloadItemModel
                {
                    Id = e.Id
                };
                newItem.Update(e);
                downloads.Insert(0, newItem);

                if (downloads.Count > 5)
                {
                    downloads.RemoveAt(downloads.Count - 1);
                }
            }
        }

        private void RegisterSettingsBridge(ChromiumWebBrowser browser)
        {
            if (ViewModel?.AppSettingsBridge == null)
            {
                System.Diagnostics.Debug.WriteLine("[Мост JS] Oшибка: AppSettingsBridge равен null!");
                return;
            }

            browser.JavascriptObjectRepository.Settings.LegacyBindingEnabled = true;

            try
            {
                browser.JavascriptObjectRepository.Register("csharpSettingsBridge", ViewModel.AppSettingsBridge, options: BindingOptions.DefaultBinder);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Мост JS] Ошибка при регистрации csharpSettingsBridge: {ex.Message}");
            }
        }

        

        public class SettingBridge
        {
            public void OpenSettings()
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("ms-settings:defaultapps") { UseShellExecute = true });

            }
        }

    }
}

        
    

       