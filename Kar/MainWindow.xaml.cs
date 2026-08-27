using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Interop;
using CefSharp;
using CefSharp.Wpf;
using Kar.Handlers;
using Microsoft.VisualBasic.Devices;

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

        /// <summary>
        /// Конструктор главного окна. Инициализирует компоненты, 
        /// настраивает вью-модель, подписки на закрытие и изменение коллекции вкладок,
        /// а также динамически привязывает шорткаты.
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
            SourceInitialized += MainWindow_SourceInitialized;

            if (System.ComponentModel.DesignerProperties.GetIsInDesignMode(this))
            {
                return;
            }

            // Создаем MainViewModel с потокобезопасной службой диспетчеризации
            ViewModel = new MainViewModel(new WpfDispatcherService(this.Dispatcher));
            ViewModel.CloseRequested += () => Close();

            // Обработчик удаления вкладки — очищает кэш браузера для предотвращения утечек памяти
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

            // Ограничиваем максимальные размеры окна текущими границами экрана
            this.SourceInitialized += (s, e) =>
            {
                var screen = GetCurrentScreenBounds(true);
                this.MaxHeight = screen.Height;
                this.MaxWidth = screen.Width;
            };

            var networkSM = new Homepage.NetworkSpeedMonitor();
            networkSM.Start();

            ApplyShortcuts();

            this.DataContext = ViewModel;
            SetupTabManager();
            UpdBrowserUI();
        }

        /// <summary>
        /// Управляет толщиной рамки окна при изменении состояния (убирает рамку в режиме Maximized).
        /// </summary>
        private void MainWindow_StateChanged(object sender, EventArgs e)
        {
            if (this.WindowState == WindowState.Maximized)
            {
                this.BorderThickness = new Thickness(0);
            }
            else
            {
                this.BorderThickness = new Thickness(1);
            }
        }

        /// <summary>
        /// Регистрирует перехватчик (Hook) оконных сообщений Windows для кастомной обработки геометрии окна.
        /// </summary>
        private void MainWindow_SourceInitialized(object sender, EventArgs e)
        {
            IntPtr handle = new WindowInteropHelper(this).Handle;
            HwndSource.FromHwnd(handle)?.AddHook(WindowProc);
        }

        /// <summary>
        /// Обработчик кнопки закрытия окна.
        /// </summary>
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        /// <summary>
        /// Вызывается при закрытии окна. Сохраняет текущую сессию вкладок и отключает CEF.
        /// </summary>
        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            if (ViewModel != null)
            {
                ViewModel.SaveCurrentSession();
            }
            Cef.Shutdown();
            base.OnClosed(e);
        }

        /// <summary>
        /// Сворачивает окно приложения.
        /// </summary>
        private void MinimizeButton_Click(object sender, RoutedEventArgs e) => this.WindowState = WindowState.Minimized;

        /// <summary>
        /// Разворачивает окно на весь экран или восстанавливает его прежний размер.
        /// </summary>
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

        /// <summary>
        /// Получает границы экрана для текущего окна с помощью Win32 API.
        /// </summary>
        /// <param name="workingAreaOnly">True, если нужно исключить панель задач (рабочая область), иначе False.</param>
        private Rect GetCurrentScreenBounds(bool workingAreaOnly)
        {
            IntPtr hwnd = new WindowInteropHelper(this).Handle;
            IntPtr monitor = MonitorFromWindow(hwnd, 2); // MONITOR_DEFAULTTONEAREST = 2
            if (monitor != IntPtr.Zero)
            {
                MONITORINFO monitorInfo = new MONITORINFO();
                monitorInfo.cbSize = Marshal.SizeOf(typeof(MONITORINFO));
                if (GetMonitorInfo(monitor, ref monitorInfo))
                {
                    var rect = workingAreaOnly ? monitorInfo.rcWork : monitorInfo.rcMonitor;
                    return new Rect(rect.Left, rect.Top, rect.Right - rect.Left, rect.Bottom - rect.Top);
                }
            }
            
            return workingAreaOnly
                ? new Rect(SystemParameters.WorkArea.Left, SystemParameters.WorkArea.Top, SystemParameters.WorkArea.Width, SystemParameters.WorkArea.Height)
                : new Rect(0, 0, SystemParameters.PrimaryScreenWidth, SystemParameters.PrimaryScreenHeight);
        }

        /// <summary>
        /// Включает или отключает полноэкранный режим (например, при просмотре видео на YouTube).
        /// Скрывает верхнюю панель и строку адреса.
        /// </summary>
        /// <param name="isFullScreen">True для перехода в полноэкранный режим, False для возврата.</param>
        public void ToggleFullScreen(bool isFullScreen)
        {
            Dispatcher.Invoke(() =>
            {
                if (isFullScreen)
                {
                    // Сохраняем текущее состояние перед переходом в полноэкранный режим
                    _prevWindowState = this.WindowState;
                    _prevWindowStyle = this.WindowStyle;
                    _prevResizeMode = this.ResizeMode;

                    if (this.WindowState == WindowState.Normal)
                    {
                        _normalWindowBounds = new Rect(this.Left, this.Top, this.Width, this.Height);
                    }

                    var screen = GetCurrentScreenBounds(false);

                    // Убираем рамки и разворачиваем на полный экран
                    this.WindowStyle = WindowStyle.None;
                    this.ResizeMode = ResizeMode.NoResize;
                    this.WindowState = WindowState.Normal;

                    this.MaxHeight = double.PositiveInfinity;
                    this.MaxWidth = double.PositiveInfinity;

                    this.Left = screen.Left;
                    this.Top = screen.Top;
                    this.Width = screen.Width;
                    this.Height = screen.Height;

                    // Скрываем элементы управления браузера
                    TopRow.Height = new GridLength(0);
                    PanelControl.Height = new GridLength(0);
                }
                else
                {
                    // Восстанавливаем прежний стиль окна
                    this.WindowState = _prevWindowState;
                    this.WindowStyle = _prevWindowStyle;

                    var workArea = GetCurrentScreenBounds(true);
                    this.MaxHeight = workArea.Height;
                    this.MaxWidth = workArea.Width;

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

                    // Показываем элементы управления обратно
                    TopRow.Height = new GridLength(34);
                    PanelControl.Height = new GridLength(45);
                }
            });
        }

        /// <summary>
        /// Вызывается при выборе поисковой подсказки в выпадающем списке.
        /// </summary>
        private void SuggestionList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SuggestionList.SelectedItem is string selected)
            {
                UrlTextBox.Text = selected;
                SuggestionPopup.IsOpen = false;

                MapsToUrl(selected);
            }
        }

        /// <summary>
        /// Переводит поисковый запрос подсказки в отформатированный URL во вью-модели.
        /// </summary>
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

        /// <summary>
        /// Обработчик изменения ввода в адресной строке. Асинхронно запрашивает автодополнение
        /// и открывает всплывающее окно подсказок SuggestionPopup.
        /// </summary>
        private async void UrlTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!UrlTextBox.IsFocused)
            {
                SuggestionPopup.IsOpen = false;
                return;
            }

            string query = UrlTextBox.Text;

            // Если введено более 2-х символов и это не прямая ссылка
            if (query.Length > 2 && !query.StartsWith("http"))
            {
                await ViewModel.LoadSearchSuggestionsAsync(query);

                if (ViewModel.SearchSuggestions.Any())
                {
                    SuggestionList.ItemsSource = ViewModel.SearchSuggestions;
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

        /// <summary>
        /// Внутренний метод перехода по адресу или выполнения поискового запроса.
        /// </summary>
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

        /// <summary>
        /// Вызывается при нажатии клавиш в адресной строке. По Enter инициирует переход.
        /// </summary>
        private void UrlTextBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
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
                System.Windows.Input.Keyboard.ClearFocus();
            }
        }

        /// <summary>
        /// Настраивает перерисовку браузера при смене активной вкладки во вью-модели.
        /// </summary>
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

        /// <summary>
        /// Основной метод обновления пользовательского интерфейса браузера CefSharp.
        /// Извлекает из кэша или создает новый экземпляр ChromiumWebBrowser для выбранной вкладки,
        /// настраивает JS-мосты и регистрирует кастомные обработчики событий (LifeSpan, Request, Download и др.).
        /// </summary>
        private void UpdBrowserUI()
        {
            var selectedTab = ViewModel.SelectedTab;
            if (selectedTab == null) return;

            // Если для вкладки еще нет созданного веб-браузера в кэше, инициализируем его
            if (!_browserCache.ContainsKey(selectedTab))
            {
                var newBrowser = new ChromiumWebBrowser();
                var downloadHandler = new CustomDownloadHandler();
                
                var browserOps = new CefSharpBrowserOperations(newBrowser);
                selectedTab.BrowserOperations = browserOps;

                // Регистрация обработчиков событий
                downloadHandler.DownloadStateChanged += OnDownloadStateChanged;
                newBrowser.MenuHandler = new CustomMenuHandler();
                newBrowser.LifeSpanHandler = new CustomLifeSpanHandler(url => ViewModel.AddNewTab(url), url => OpenPopupInWindow(url));
                newBrowser.RequestHandler = new CustomRequestHandler(url => ViewModel.AddNewTab(url));
                newBrowser.KeyboardHandler = new CustomKeyboardHandler(this);
                newBrowser.DownloadHandler = downloadHandler;

                // Настройка поддержки интеграции C# с JS (загрузка истории, настроек и сессий в HTML)
                newBrowser.JavascriptObjectRepository.Settings.LegacyBindingEnabled = true;
                newBrowser.JavascriptObjectRepository.Register("SettingsHandler", new SettingBridge(), options: BindingOptions.DefaultBinder);
                newBrowser.JavascriptObjectRepository.Register("csharpSettingsBridge", ViewModel.AppSettingsBridge, options: BindingOptions.DefaultBinder);
                newBrowser.JavascriptObjectRepository.Register("HistoryBridgeCStoJS", new HistoryPage.HistoryBridge(), options: BindingOptions.DefaultBinder);
                newBrowser.JavascriptObjectRepository.Register("HomeBridgeCStoJS", new Homepage.HomeBridgeCStoJS(newBrowser), options: BindingOptions.DefaultBinder);

                // Обработчик события изменения заголовка веб-страницы (сохраняет страницы в историю посещений)
                newBrowser.TitleChanged += async (s, args) =>
                {
                    Dispatcher.Invoke(() =>
                    {
                        selectedTab.Title = args.NewValue.ToString() ?? "Загрузка...";
                    });

                    var webBrowser = (ChromiumWebBrowser)s;
                    string CurrentUrl = webBrowser.Address;
                    string pageTitle = args.NewValue.ToString() ?? "Без названия";

                    // Пропускаем служебные страницы при сохранении в историю
                    if (string.IsNullOrWhiteSpace(CurrentUrl) || CurrentUrl.StartsWith("chrome-devtools://") || CurrentUrl.StartsWith("file://") ||
                    CurrentUrl.StartsWith("chrome://") || CurrentUrl == "about:blank")
                    {
                        return;
                    }

                    try
                    {
                        await App.HistoryRepo.SaveQueryAsync(CurrentUrl, pageTitle);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Ошибка при сохранении истории: {ex.Message}");
                    }
                };

                newBrowser.DisplayHandler = new CustomDisplayHandler(selectedTab, Dispatcher, ToggleFullScreen);

                // Двусторонняя привязка свойства адреса веб-страницы
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

            // Заменяем текущий отображаемый браузер на активный
            if (BrowserHost.Children.Count == 0 || BrowserHost.Children[0] != activeBrowser)
            {
                BrowserHost.Children.Clear();
                BrowserHost.Children.Add(activeBrowser);
            }
        }

        /// <summary>
        /// Определяет, является ли ввод поисковым запросом (содержит пробелы или не содержит точек).
        /// </summary>
        private bool IsSearchQuery(string input)
        {
            if (input.Contains(" ")) return true;

            if (input.StartsWith("Localhost", StringComparison.OrdinalIgnoreCase) || input.StartsWith("127.0.0.1")) return false;

            return !input.Contains(".");
        }

        /// <summary>
        /// Удаляет вкладку из кэша браузеров и корректно освобождает связанные ресурсы.
        /// </summary>
        private void DoDelCache(TabViewModel tab)
        {
            if (tab == null) return;

            tab.Dispose();

            if (!_browserCache.ContainsKey(tab)) return;

            var browser = _browserCache[tab];
            BindingOperations.ClearAllBindings(browser);

            if (BrowserHost.Children.Contains(browser))
            {
                BrowserHost.Children.Clear();
            }

            browser.Dispose();
            _browserCache.Remove(tab);
        }

        /// <summary>
        /// Открывает окно инструментов разработчика (Chrome DevTools) для текущей активной вкладки.
        /// </summary>
        public void ShowDevTools()
        {
            Dispatcher.Invoke(() =>
            {
                var selectedTab = ViewModel.SelectedTab;

                if (selectedTab != null && _browserCache.TryGetValue(selectedTab, out var browser))
                {
                    var windowInfo = new WindowInfo();

                    var helper = new WindowInteropHelper(this);
                    IntPtr hostHandle = helper.Handle;

                    // Позиционируем DevTools внутри окна браузера
                    windowInfo.SetAsChild(hostHandle, (int)browser.ActualWidth - 500, 0, (int)browser.ActualWidth, (int)browser.ActualHeight);

                    browser.ShowDevTools(windowInfo);
                }
            });
        }

        /// <summary>
        /// Загружает горячие клавиши из файла конфигурации shortcut.yaml 
        /// и динамически регистрирует их привязки (InputBindings) к командам главного окна.
        /// </summary>
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
                                targetCommand = new RelayCommand(_ =>
                                {
                                    if (WindowState != WindowState.Maximized)
                                    {
                                        ToggleFullScreen(WindowState == WindowState.Maximized);
                                    }
                                    else
                                    {
                                        ToggleFullScreen(WindowState == WindowState.Normal);
                                    }
                                }); 
                                break;
                            case "ShowDevTools":
                                targetCommand = new RelayCommand(_ => ShowDevTools());
                                break;
                            case "Reload":
                                targetCommand = new RelayCommand(_ =>
                                {
                                    if (ViewModel.SelectedTab != null && _browserCache.TryGetValue(ViewModel.SelectedTab, out var browser))
                                    {
                                        browser.Reload();
                                    }
                                });
                                break;
                            case "ReopenClosedTab":
                                targetCommand = ViewModel.ReopenClosedTabCommand;
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
            }
            System.Diagnostics.Debug.WriteLine($"[WPF] Успешно привязано горячих клавиш к окну: {addedBindingsCount}");
        }

        /// <summary>
        /// Открывает всплывающее диалоговое окно (например, для страниц авторизации Google/VK OAuth)
        /// в отдельном дочернем WPF окне.
        /// </summary>
        /// <param name="url">URL-адрес страницы входа.</param>
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

        /// <summary>
        /// Обработчик обновления статуса загрузки. Обновляет модель недавних загрузок.
        /// </summary>
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

                // Ограничиваем список недавних загрузок пятью элементами
                if (downloads.Count > 5)
                {
                    downloads.RemoveAt(downloads.Count - 1);
                }
            }
        }

        /// <summary>
        /// Регистрирует мост настроек настроек с JS для указанного инстанса ChromiumWebBrowser.
        /// </summary>
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

        /// <summary>
        /// Системная процедура обработки сообщений окна Windows Hook. Перехватывает WM_GETMINMAXINFO.
        /// </summary>
        private IntPtr WindowProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == 0x0024) // WM_GETMINMAXINFO
            {
                WmGetMinMaxInfo(hwnd, lParam);
                handled = true;
            }
            return IntPtr.Zero;
        }

        // P/Invoke импорт функций Win32 API для корректного разворачивания кастомного окна WPF
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

        /// <summary>
        /// Рассчитывает размеры развернутого окна (Maximized) с учетом текущего монитора и размера панели задач.
        /// Исключает дефолтное поведение Windows, когда кастомное окно без рамок закрывает панель задач.
        /// </summary>
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

        /// <summary>
        /// Вспомогательный класс моста JS для открытия настроек ОС Windows по умолчанию.
        /// </summary>
        public class SettingBridge
        {
            /// <summary>
            /// Запускает встроенный интерфейс настроек приложений ОС Windows по умолчанию.
            /// </summary>
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