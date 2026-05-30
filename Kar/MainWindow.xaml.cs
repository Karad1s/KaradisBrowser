using CefSharp;
using CefSharp.Wpf;
using Kar;
using Kar.Handlers;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Interop;
using System.Runtime.InteropServices;
using WPF = System.Windows;

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

        private Rect _normalWindowBounds;

        private readonly Dictionary<TabViewModel, ChromiumWebBrowser> _browserCache = new Dictionary<TabViewModel, ChromiumWebBrowser>();

        public MainViewModel ViewModel { get; set; }

        public MainWindow()
        {
            InitializeComponent();
            SourceInitialized += MainWindow_SourceInitialized;

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
            this.SourceInitialized += (s, e) =>
            {
                IntPtr handle = new System.Windows.Interop.WindowInteropHelper(this).Handle;
                var screen = System.Windows.Forms.Screen.FromHandle(handle);

                this.MaxHeight = screen.WorkingArea.Height;
                this.MaxWidth = screen.WorkingArea.Width;
            };

            ApplyShortcuts();

            this.DataContext = ViewModel;
            SetupTabManager();
            UpdBrowserUI();
        }


        private void MainWindow_StateChanged(object sender, EventArgs e)
        {
            if (this.WindowState == WindowState.Maximized)
            {
                this.BorderThickness = new Thickness(8);
            }
            else
            {
                this.BorderThickness = new Thickness(0);
            }
        }

        private void MainWindow_SourceInitialized(object sender, EventArgs e)
        {
            IntPtr handle = new WindowInteropHelper(this).Handle;
            HwndSource.FromHwnd(handle)?.AddHook(WindowProc);
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            if (ViewModel != null)
            {
                ViewModel.SaveCurrentSession();
            }
            Cef.Shutdown();
            base.OnClosed(e);
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

        public void ToggleFullScreen(bool isFullScreen)
        {
            Dispatcher.Invoke(() =>
            {
                if (isFullScreen)
                {

                    _prevWindowState = this.WindowState;
                    _prevWindowStyle = this.WindowStyle;
                    _prevResizeMode = this.ResizeMode;

                    if (this.WindowState == WindowState.Normal)
                    {
                        _normalWindowBounds = new Rect(this.Left, this.Top, this.Width, this.Height);
                    }
                    var handle = new System.Windows.Interop.WindowInteropHelper(this).Handle;
                    var screen = System.Windows.Forms.Screen.FromHandle(handle);

                    this.WindowStyle = WindowStyle.None;
                    this.ResizeMode = ResizeMode.NoResize;
                    this.WindowState = WindowState.Normal;


                    this.Left = screen.Bounds.Left;
                    this.Top = screen.Bounds.Top;
                    this.Width = screen.Bounds.Width;
                    this.Height = screen.Bounds.Height;

                    TopRow.Height = new GridLength(0);
                    PanelControl.Height = new GridLength(0);
                }
                else
                {

                    if (_prevWindowState == WindowState.Normal)
                    {
                        this.WindowState = WindowState.Normal;
                        this.Left = _normalWindowBounds.Left;
                        this.Top = _normalWindowBounds.Top;
                        this.Width = _normalWindowBounds.Width;
                        this.Height = _normalWindowBounds.Height;
                    }
                    else
                    {
                        this.WindowState = _prevWindowState;
                    }

                    TopRow.Height = new GridLength(34);
                    PanelControl.Height = new GridLength(45);
                }
            });
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
            if (!UrlTextBox.IsFocused)
            {
                SuggestionPopup.IsOpen = false;
                return;
            }

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

        private void NegativeToUrl(string query)
        {
            if (string.IsNullOrWhiteSpace(query)) return;

            if (IsSearchQuery(query))
            {

                if (ViewModel.SelectedTab != null)
                {
                    string currentSearchEngine = ViewModel.SelectedTab.CurrentSearchEngine ?? ViewModel.GlobalSearchEngine;
                    ViewModel.SelectedTab.Url = ViewModel.FormatSearchQuery(query, currentSearchEngine);
                }
            }
            else
            {
                if (ViewModel.SelectedTab != null)
                {
                    ViewModel.SelectedTab.Url = query.StartsWith("http", StringComparison.OrdinalIgnoreCase) ? query : "http://" + query;
                }
            }
        }

        private async void UrlTextBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {

            if (e.Key == Key.Enter)
            {
                e.Handled = true;
                string userInput = UrlTextBox.Text;
                string currentTabEngine = ViewModel.SelectedTab?.CurrentSearchEngine ?? "Google";
                System.Diagnostics.Debug.WriteLine($"Для вкладки выбран: '{currentTabEngine}'\nГлобально: '{ViewModel.GlobalSearchEngine}'");

                string currentSearchEngine = ViewModel.SelectedTab?.CurrentSearchEngine ?? "Google";
                string finalUrl = ViewModel.FormatSearchQuery(userInput, currentSearchEngine);

                if (ViewModel.SelectedTab != null) ViewModel.SelectedTab.Url = finalUrl;
                NegativeToUrl(userInput);
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
                newBrowser.KeyboardHandler = new CustomKeyboardHandler(this);
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

                System.Windows.Data.Binding myBinding = new System.Windows.Data.Binding("Url")
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

        private bool IsSearchQuery(string input)
        {
            if (input.Contains(" ")) return true;

            if (input.StartsWith("Localhost", StringComparison.OrdinalIgnoreCase) || input.StartsWith("127.0.0.1")) return false;

            return !input.Contains(".");
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
            WPF.Application.Current.Dispatcher.Invoke(() =>
            {
                var window = (MainWindow)WPF.Application.Current.MainWindow;
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

        public void ApplyShortcuts()
        {
            var shortcuts = Shortcut.ShortcutLoader.LoadShortcut();
            System.Diagnostics.Debug.WriteLine($"[YAML] Загружено шорткатов из файла: {shortcuts.Count}");
            var gestureConverter = new KeyGestureConverter();
            byte addedBindingsCount = 0;

            foreach (var shortcut in shortcuts)
            {
                try
                {
                    if (gestureConverter.ConvertFromString(shortcut.Gesture) is KeyGesture keyGesture)
                    {
                        ICommand? targetCommand = null;

                        switch (shortcut.Action)
                        {
                            case "NewTab":
                                targetCommand = ViewModel.AddTabCommand;

                                break;
                            case "CloseTab":
                                targetCommand = ViewModel.CloseTabCommand;
                                break;
                            case "OpenSettings":
                                targetCommand = ViewModel.SettingsCommand;
                                break;
                            case "OpenHistory":
                                targetCommand = ViewModel.HistoryCommand;
                                break;
                            case "ToggleFullScreen":
                                targetCommand = new RelayCommand(_ => {
                                    if (this.WindowState != WindowState.Maximized)
                                    {
                                        WindowState = WindowState.Maximized;
                                    }
                                    else
                                    {
                                        WindowState = WindowState.Normal;
                                    }
                                });
                                break;
                            case "ShowDevTools":
                                targetCommand = new RelayCommand(_ => ShowDevTools());
                                break;
                        }

                        if (targetCommand != null)
                        {
                            this.InputBindings.Add(new KeyBinding(targetCommand, keyGesture));
                            addedBindingsCount++;
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($"[YAML] Действие '{shortcut.Action}' не распознано в switch.");
                        }
                    }

                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Ошибка при применении горячей клавиши '{shortcut.Gesture}' для действия '{shortcut.Action}': {ex.Message}");
                }

                System.Diagnostics.Debug.WriteLine($"[WPF] Успешно привязано горячих клавиш к окну: {addedBindingsCount}");
            }
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

        private IntPtr WindowProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == 0x0024)
            {
                WmGetMinMaxInfo(hwnd, lParam);
                handled = true;
            }
            return IntPtr.Zero;
        }


        [DllImport("user32.dll")]
        private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);

        [DllImport("user32.dll")]
        private static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint dwFlags);

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int X;
            public int Y;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MINMAXINFO
        {
            public POINT ptReserved;
            public POINT ptMaxSize;
            public POINT ptMaxPosition;
            public POINT ptMinTrackSize;
            public POINT ptMaxTrackSize;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct MONITORINFO
        {
            public int cbSize;
            public RECT rcMonitor;
            public RECT rcWork;
            public uint dwFlags;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        private void WmGetMinMaxInfo(IntPtr hwnd, IntPtr lParam)
        {
            MINMAXINFO mmi = (MINMAXINFO)Marshal.PtrToStructure<MINMAXINFO>(lParam);

            IntPtr monitor = MonitorFromWindow(hwnd, 2);
            if (monitor != IntPtr.Zero)
            {
                MONITORINFO monitorInfo = new MONITORINFO();
                monitorInfo.cbSize = Marshal.SizeOf(typeof(MONITORINFO));
                GetMonitorInfo(monitor, ref monitorInfo);

                RECT area = (this.WindowStyle == WindowStyle.None) ? monitorInfo.rcMonitor : monitorInfo.rcWork;

                mmi.ptMaxSize.X = Math.Abs(area.Right - area.Left);
                mmi.ptMaxSize.Y = Math.Abs(area.Bottom - area.Top);
                mmi.ptMaxPosition.X = Math.Abs(area.Left);
                mmi.ptMaxPosition.Y = Math.Abs(area.Top);
            }
            Marshal.StructureToPtr(mmi, lParam, true);
        }
    

        public class SettingBridge
        {
            public void OpenSettings()
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("ms-settings:defaultapps") { UseShellExecute = true });

            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {

        }
    }
}