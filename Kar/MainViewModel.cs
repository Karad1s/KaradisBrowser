using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Input;
using Kar.Settings;

namespace Kar
{
    /// <summary>
    /// Главная вью-модель приложения. Управляет коллекцией вкладок, загрузками, 
    /// настройками поисковой системы и глобальными командами окна.
    /// </summary>
    public class MainViewModel : INotifyPropertyChanged
    {
        private TabViewModel _selectedTab;
        private readonly IDispatcherService _dispatcherService;
        private readonly SessionManager _sessionManager = new SessionManager();
        private bool _isSearchMenuOpen;
        private SearchSystem _selectedSearchSystem;
        private string _globalSearchEngine = "Google";

        private static readonly HttpClient HttpClient = new HttpClient();
        private readonly ObservableCollection<string> _searchSuggestions = new ObservableCollection<string>();
        
        // Стек для хранения URL адресов недавно закрытых вкладок для возможности их повторного открытия
        private Stack<string> _recentlyClosedUrls = new Stack<string>();

        /// <summary>
        /// Событие, запрашивающее закрытие главного окна приложения (когда закрыты все вкладки).
        /// </summary>
        public event Action? CloseRequested;

        /// <summary>
        /// Управляет отображением меню выбора поисковой системы.
        /// </summary>
        public bool IsSearchMenuOpen
        {
            get => _isSearchMenuOpen;
            set
            {
                if (_isSearchMenuOpen != value)
                {
                    _isSearchMenuOpen = value;
                    OnPropertyChanged(nameof(IsSearchMenuOpen));
                }
            }
        }

        /// <summary>
        /// Выбранная пользователем поисковая система для ввода запросов.
        /// </summary>
        public SearchSystem SelectedSearchSystem
        {
            get
            {
                return _selectedSearchSystem ?? LocalSearchSystems.FirstOrDefault();
            }
            set
            {
                if (_selectedSearchSystem != value)
                {
                    _selectedSearchSystem = value;
                    OnPropertyChanged(nameof(SelectedSearchSystem));

                    // Закрываем выпадающее меню после выбора
                    IsSearchMenuOpen = false;

                    if(_selectedSearchSystem != null)
                    {
                        // Обновляем поисковик для текущей активной вкладки
                        SelectedTab.CurrentSearchEngine = _selectedSearchSystem.Name;
                    }
                }
            }
        }

        /// <summary>
        /// Мост для обмена данными настроек между C# и JavaScript.
        /// </summary>
        public SettingsBridge AppSettingsBridge { get; }

        public string GlobalSearchEngine
        {
            get => _globalSearchEngine;
            private set
            {
                if (_globalSearchEngine != value)
                {
                    _globalSearchEngine = value;
                    OnPropertyChanged();
                }
            }
        }

        public ObservableCollection<TabViewModel> Tabs { get; set; } = new ObservableCollection<TabViewModel>();
        public CompositeCollection TabItems { get; set; }

        public ObservableCollection<DownloadItemModel> RecentDownloads { get; set; } = new ObservableCollection<DownloadItemModel>();

        public ObservableCollection<SearchSystem> LocalSearchSystems { get; } = new ObservableCollection<SearchSystem>
        {
            new SearchSystem("Google","https://www.google.com/search?q="),
            new SearchSystem("Bing","https://www.bing.com/search?q="),
            new SearchSystem("DuckDuckGo","https://duckduckgo.com/?q="),
            new SearchSystem("Yandex","https://www.yandex.com/search?text="),
            new SearchSystem("Yahoo","https://search.yahoo.com/search?p="),
            new SearchSystem("Ask","https://www.ask.com/web?q=")
        };

        public ObservableCollection<string> SearchSuggestions => _searchSuggestions;

        public TabViewModel SelectedTab
        {
            get => _selectedTab;
            set
            {
                _selectedTab = value;
                OnPropertyChanged(nameof(SelectedTab));
            }
        }

        public ICommand AddTabCommand { get; }
        public ICommand CloseTabCommand { get; }
        public ICommand SelectedTabCommand { get; }
        public ICommand SettingsCommand { get; }
        public ICommand HistoryCommand { get; }
        public ICommand ExtentionsCommand { get; }
        public ICommand ShowAllDownloadsCommand { get; }
        public ICommand OpenDownloadsFolderCommand { get; }
        public ICommand ReopenClosedTabCommand { get; }

        /// <summary>
        /// Инициализирует главную вью-модель браузера, настраивает команды управления,
        /// инициализирует службу сохранения сессий и загружает сохраненное состояние вкладок.
        /// </summary>
        /// <param name="dispatcherService">Сервис для работы с потоком пользовательского интерфейса (UI).</param>
        public MainViewModel(IDispatcherService dispatcherService)
        {
            _dispatcherService = dispatcherService ?? throw new ArgumentNullException(nameof(dispatcherService));

            // Создание директории настроек, если она отсутствует
            string settingsDir = BrowserConfig.SettingsDir;
            if (!Directory.Exists(settingsDir)) Directory.CreateDirectory(settingsDir);

            string settingsPath = BrowserConfig.SettingsPath;
            var fileService = new FileSettingsService(settingsPath);

            AppSettingsBridge = new SettingsBridge(fileService);
            AppSettingsBridge.OnSettingsSaved += HandleSettingsUpdate;

            // Загрузка сохраненных настроек (например, дефолтной поисковой системы)
            HandleSettingsUpdate(AppSettingsBridge.GetSettings());

            // Команда добавления новой вкладки
            AddTabCommand = new RelayCommand(obj =>
            {
                AddNewTab(string.Empty);
                System.Diagnostics.Debug.WriteLine("[WPF Command] Вызвано создание новой вкладки!");
            });

            // Команда закрытия указанной вкладки
            CloseTabCommand = new RelayCommand(obj =>
            {
                var tab = obj as TabViewModel ?? SelectedTab;

                if(tab != null)
                {
                    // Сохраняем URL закрытой вкладки в стек для быстрого восстановления
                    if (!string.IsNullOrWhiteSpace(tab.Url))
                    {
                        _recentlyClosedUrls.Push(tab.Url);
                    };
                }
                int index = Tabs.IndexOf(tab);

                // Если закрывается активная вкладка, переключаем фокус на соседнюю вкладку
                if(SelectedTab == tab)
                {
                    if (Tabs.Count > 1)
                    {
                        int newIndex = Math.Max(0, index - 1);
                        SelectedTab = Tabs[newIndex];
                    }
                    else
                    {
                        SelectedTab = null;
                    }
                }
                Tabs.Remove(tab);
                
                // Если все вкладки закрыты, отправляем запрос на закрытие всего приложения
                if(Tabs.Count == 0)
                {
                    CloseRequested?.Invoke();
                }
            });

            // Команда выбора активной вкладки
            SelectedTabCommand = new RelayCommand(obj =>
            {
                if (obj is TabViewModel tab)
                {
                    SelectedTab = tab;
                }
            });

            // Команда открытия локальной HTML-страницы настроек
            SettingsCommand = new RelayCommand(obj =>
            {
                var Url = $"file:///{Path.Combine(BrowserConfig.SettingsDir, "settings.html").Replace('\\', '/')}";
                AddNewTab(Url);
            });

            // Команда открытия локальной HTML-страницы истории посещений
            HistoryCommand = new RelayCommand(obj =>
            {
                var Url = $"file:///{BrowserConfig.HistoryHtmlPath.Replace('\\', '/')}";
                AddNewTab(Url);
            });

            // Команда открытия интернет-магазина расширений Chrome
            ExtentionsCommand = new RelayCommand(obj =>
            {
                AddNewTab(BrowserConfig.ChromeExtensionsUrl);
            });

            // Команда открытия страницы загрузок
            ShowAllDownloadsCommand = new RelayCommand(obj =>
            {
                AddNewTab($"file:///{BrowserConfig.LibraryHtmlPath.Replace('\\', '/')}");
            });

            // Команда открытия папки «Загрузки» в проводнике Windows
            OpenDownloadsFolderCommand = new RelayCommand(obj =>
            {
                string userDownloadsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
                OpenFolderExplorer(userDownloadsPath);
            });

            // Команда восстановления последней закрытой вкладки
            ReopenClosedTabCommand = new RelayCommand(obj =>
            {
                if (_recentlyClosedUrls.Count > 0)
                {
                    string url = _recentlyClosedUrls.Pop();
                    AddNewTab(url);
                }
            });

            // Настройка коллекции вкладок для отображения (включает кнопку добавления новой вкладки)
            TabItems = new CompositeCollection();
            var cont = new CollectionContainer { Collection = Tabs };
            TabItems.Add(cont);
            TabItems.Add(new AddTabButton());

            // Восстановление вкладок предыдущего сеанса
            var SavedTabs = _sessionManager.LoadSession();
            if (SavedTabs != null && SavedTabs.Any())
            {
                foreach (var tab in SavedTabs)
                {
                    RestoreTab(tab);
                }
            }
            else
            {
                // Если сохраненной сессии нет, создаем одну пустую домашнюю вкладку
                AddNewTab(string.Empty);
            }
        }

        /// <summary>
        /// Анализирует ввод пользователя и преобразует его либо в прямой URL, 
        /// либо в строку запроса к указанной поисковой системе.
        /// </summary>
        /// <param name="userInput">Введенный пользователем текст.</param>
        /// <param name="engineName">Имя целевой поисковой системы.</param>
        /// <returns>Готовый URL-адрес для навигации браузера.</returns>
        public string FormatSearchQuery(string userInput, string engineName)
        {
            if (string.IsNullOrWhiteSpace(userInput)) return string.Empty;

            // Если ввод содержит точку и не содержит пробелов, считаем его адресом сайта
            if (userInput.Contains(".") && !userInput.Contains(" "))
            {
                return userInput.StartsWith("http") ? userInput : $"https://{userInput}";
            }

            // Иначе форматируем как поисковый запрос к выбранной системе
            var system = LocalSearchSystems.FirstOrDefault(s => s.Name.Equals(engineName, StringComparison.OrdinalIgnoreCase));
            if (system != null)
            {
                return system.Url + Uri.EscapeDataString(userInput);
            }

            // Дефолтный поиск в Google
            return $"https://www.google.com/search?q={Uri.EscapeDataString(userInput)}";
        }

        /// <summary>
        /// Создает и добавляет новую вкладку в список, переключая на нее фокус.
        /// </summary>
        /// <param name="url">Начальный URL новой вкладки. Если пустой, открывает домашнюю страницу.</param>
        public void AddNewTab(string url)
        {
            string currentEngine = this.GlobalSearchEngine;
            string FinalUrl = FormatSearchQuery(url, currentEngine);

            var newTab = new TabViewModel(_dispatcherService) { Title = "Новая вкладка", Url = url, CurrentSearchEngine = currentEngine };

            if (string.IsNullOrEmpty(url) || url == BrowserConfig.AboutHome)
            {
                newTab.Url = $"file:///{BrowserConfig.HomepageHtmlPath.Replace('\\', '/')}";
            }

            Tabs.Add(newTab);
            SelectedTab = newTab;
        }

        /// <summary>
        /// Восстанавливает вкладку из сохраненной сессии DTO.
        /// </summary>
        /// <param name="dto">Объект сессии вкладки.</param>
        public void RestoreTab(TabSessionDto dto)
        {
            var newTab = new TabViewModel(_dispatcherService)
            {
                Title = dto.Title,
                Url = (string.IsNullOrEmpty(dto.Url) || dto.Url == "Empty URL") ? BrowserConfig.FallbackHomeRelative : dto.Url,
                NavigationHistory = dto.NavigationHistory ?? new List<string>(),
                CurrentHistoryIndex = dto.CurrentHistoryIndex,
            };
            Tabs.Add(newTab);
            SelectedTab = newTab;
        }

        /// <summary>
        /// Сохраняет текущую сессию (список открытых вкладок и историю их переходов) в файл конфигурации сессии YAML.
        /// </summary>
        public void SaveCurrentSession()
        {
            try
            {
                if (Tabs == null || Tabs.Count == 0)
                {
                    System.Diagnostics.Debug.WriteLine("Отладка: Коллекция вкладок пуста, сохранять нечего.", "Session Debug");
                    return;
                }

                string path = BrowserConfig.SessionPath;
                System.Diagnostics.Debug.WriteLine($"Отладка: Успешно!\nФайл должен быть здесь:\n{path}", "Session Debug");

                _sessionManager.SaveSession(Tabs, false);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Сессия] Ошибка при сохранении сессии: {ex.Message}");
            }
        }

        /// <summary>
        /// Открывает указанный файл или директорию в стандартном проводнике Windows (explorer.exe).
        /// </summary>
        /// <param name="filePath">Абсолютный путь к файлу или папке.</param>
        public void OpenFolderExplorer(string filePath)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(filePath)) return;

                if (File.Exists(filePath))
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "explorer.exe",
                        Arguments = $"/select,\"{filePath}\"",
                        UseShellExecute = true
                    });
                }
                else if (Directory.Exists(filePath))
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "explorer.exe",
                        Arguments = $"\"{filePath}\"",
                        UseShellExecute = true
                    });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Файловая система] Ошибка при открытии проводника: {ex.Message}");
            }
        }

        /// <summary>
        /// Асинхронно запрашивает автодополнение поисковой строки от API Google.
        /// Обновляет коллекцию SearchSuggestions в UI-потоке.
        /// </summary>
        /// <param name="query">Текст запроса, введенный пользователем.</param>
        public async Task LoadSearchSuggestionsAsync(string query)
        {
            _dispatcherService.Invoke(() => _searchSuggestions.Clear());
            if (string.IsNullOrWhiteSpace(query)) return;

            try
            {
                string url = $"{BrowserConfig.GoogleSuggestionsApiUrl}{Uri.EscapeDataString(query)}";
                var response = await HttpClient.GetStringAsync(url);

                using (JsonDocument json = JsonDocument.Parse(response))
                {
                    // Google API возвращает JSON массив: [query, [suggestion1, suggestion2, ...]]
                    var list = json.RootElement[1]
                        .EnumerateArray()
                        .Select(x => x.GetString())
                        .Where(x => x != null)
                        .Cast<string>()
                        .ToList();

                    _dispatcherService.Invoke(() =>
                    {
                        foreach (var suggestion in list)
                        {
                            _searchSuggestions.Add(suggestion);
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Suggestions] Error fetching suggestions: {ex.Message}");
            }
        }

        /// <summary>
        /// Обрабатывает событие сохранения настроек в веб-интерфейсе JavaScript.
        /// Обновляет текущую глобальную поисковую систему и применяет ее ко всем вкладкам.
        /// </summary>
        /// <param name="jsonContent">JSON-строка с настройками.</param>
        private void HandleSettingsUpdate(string jsonContent)
        {
            try
            {
                using var JsonDoc = JsonDocument.Parse(jsonContent);
                string? newEngine = null;

                // Извлечение свойства SearchSystem из JSON объекта настроек
                if (JsonDoc.RootElement.TryGetProperty("SearchSystem", out var searchSystem))
                {
                    if (searchSystem.ValueKind == JsonValueKind.Object && searchSystem.TryGetProperty("content", out var content) &&
                        content.TryGetProperty("value", out var value)) newEngine = value.GetString();
                    else if (searchSystem.ValueKind == JsonValueKind.String) newEngine = searchSystem.GetString();
                }

                if (!string.IsNullOrEmpty(newEngine))
                {
                    GlobalSearchEngine = newEngine;
                    System.Diagnostics.Debug.WriteLine($"[Настройки] Обновлена поисковая система: {GlobalSearchEngine}");

                    // Обновляем текущую поисковую систему для всех вкладок в главном потоке
                    _dispatcherService.Invoke(() =>
                    {
                        foreach (var tab in Tabs)
                        {
                            tab.CurrentSearchEngine = GlobalSearchEngine;
                        }
                    });
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("Настройки сохранились, но C# не смог найти поле 'SearchSystem' в файле settings.json.", "Ошибка чтения JSON");
                }
            }
            catch (JsonException ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Настройки] Ошибка при обработке JSON: {ex.Message}");
            }
        }

        /// <summary>
        /// Событие для оповещения UI об изменении свойств привязки данных.
        /// </summary>
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// Безопасный вызов события PropertyChanged с проверкой потока.
        /// </summary>
        protected void OnPropertyChanged([CallerMemberName] string? Name = null)
        {
            if (_dispatcherService.CheckAccess())
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(Name));
            }
            else
            {
                _dispatcherService.Invoke(() =>
                {
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(Name));
                });
            }
        }
    }

    /// <summary>
    /// Специальный класс-заглушка для кнопки "Добавить вкладку", 
    /// используемый в композитной коллекции панели вкладок WPF.
    /// </summary>
    public class AddTabButton
    {
    }

    /// <summary>
    /// Представляет модель поисковой системы (например, Google, Yandex).
    /// </summary>
    public class SearchSystem
    {
        /// <summary>
        /// Имя поисковой системы.
        /// </summary>
        public string Name { get; set; } = "";

        /// <summary>
        /// Шаблон URL-адреса для совершения поискового запроса.
        /// </summary>
        public string Url { get; set; } = "";

        /// <summary>
        /// Конструктор класса.
        /// </summary>
        public SearchSystem(string name, string url)
        {
            Name = name;
            Url = url;
        }
    }

    /// <summary>
    /// Универсальная реализация интерфейса ICommand для шаблона проектирования MVVM.
    /// </summary>
    public class RelayCommand : ICommand
    {
        private readonly Action<object?> _execute;
        private readonly Predicate<object?>? _canExecute;

        /// <summary>
        /// Инициализирует команду заданным действием исполнения и условием возможности запуска.
        /// </summary>
        public RelayCommand(Action<object?> execute, Predicate<object?>? canExecute = null)
        {
            _execute = execute;
            _canExecute = canExecute;
        }

        /// <summary>
        /// Проверяет, может ли команда выполниться с текущим параметром.
        /// </summary>
        public bool CanExecute(object? parameter) => _canExecute == null || _canExecute(parameter);

        /// <summary>
        /// Выполняет команду.
        /// </summary>
        public void Execute(object? parameter) => _execute(parameter);

        /// <summary>
        /// Событие, возникающее при изменении условий, влияющих на возможность выполнения команды.
        /// </summary>
        public event EventHandler? CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }
    }
}