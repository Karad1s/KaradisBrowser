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
        private Stack<string> _recentlyClosedUrls = new Stack<string>();

        public event Action? CloseRequested;

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

                    IsSearchMenuOpen = false;
                }
            }
        }

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

        public MainViewModel(IDispatcherService dispatcherService)
        {
            _dispatcherService = dispatcherService ?? throw new ArgumentNullException(nameof(dispatcherService));

            string settingsDir = BrowserConfig.SettingsDir;
            if (!Directory.Exists(settingsDir)) Directory.CreateDirectory(settingsDir);

            string settingsPath = BrowserConfig.SettingsPath;
            var fileService = new FileSettingsService(settingsPath);

            AppSettingsBridge = new SettingsBridge(fileService);
            AppSettingsBridge.OnSettingsSaved += HandleSettingsUpdate;

            HandleSettingsUpdate(AppSettingsBridge.GetSettings());

            AddTabCommand = new RelayCommand(obj =>
            {
                AddNewTab(string.Empty);
                System.Diagnostics.Debug.WriteLine("[WPF Command] Вызвано создание новой вкладки!");
            });

            CloseTabCommand = new RelayCommand(obj =>
            {
                var tab = obj as TabViewModel ?? SelectedTab;

                if(tab != null)
                {
                    if (!string.IsNullOrWhiteSpace(tab.Url))
                    {
                        _recentlyClosedUrls.Push(tab.Url);
                    };
                }
                int index = Tabs.IndexOf(tab);

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
                if(Tabs.Count == 0)
                {
                    CloseRequested?.Invoke();
                }
            });

            SelectedTabCommand = new RelayCommand(obj =>
            {
                if (obj is TabViewModel tab)
                {
                    SelectedTab = tab;
                }
            });

            SettingsCommand = new RelayCommand(obj =>
            {
                var Url = $"file:///{Path.Combine(BrowserConfig.SettingsDir, "settings.html").Replace('\\', '/')}";
                AddNewTab(Url);
            });

            HistoryCommand = new RelayCommand(obj =>
            {
                var Url = $"file:///{BrowserConfig.HistoryHtmlPath.Replace('\\', '/')}";
                AddNewTab(Url);
            });

            ExtentionsCommand = new RelayCommand(obj =>
            {
                AddNewTab(BrowserConfig.ChromeExtensionsUrl);
            });

            ShowAllDownloadsCommand = new RelayCommand(obj =>
            {
                AddNewTab($"file:///{BrowserConfig.LibraryHtmlPath.Replace('\\', '/')}");
            });

            OpenDownloadsFolderCommand = new RelayCommand(obj =>
            {
                string userDownloadsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
                OpenFolderExplorer(userDownloadsPath);
            });

            ReopenClosedTabCommand = new RelayCommand(obj =>
            {
                if (_recentlyClosedUrls.Count > 0)
                {
                    string url = _recentlyClosedUrls.Pop();
                    AddNewTab(url);
                }
            });

            TabItems = new CompositeCollection();
            var cont = new CollectionContainer { Collection = Tabs };
            TabItems.Add(cont);
            TabItems.Add(new AddTabButton());

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
                AddNewTab(string.Empty);
            }
        }

        public string FormatSearchQuery(string userInput, string engineName)
        {
            if (string.IsNullOrWhiteSpace(userInput)) return string.Empty;

            if (userInput.Contains(".") && !userInput.Contains(" "))
            {
                return userInput.StartsWith("http") ? userInput : $"https://{userInput}";
            }

            var system = LocalSearchSystems.FirstOrDefault(s => s.Name.Equals(engineName, StringComparison.OrdinalIgnoreCase));
            if (system != null)
            {
                return system.Url + Uri.EscapeDataString(userInput);
            }

            return $"https://www.google.com/search?q={Uri.EscapeDataString(userInput)}";
        }

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

        private void HandleSettingsUpdate(string jsonContent)
        {
            try
            {
                using var JsonDoc = JsonDocument.Parse(jsonContent);
                string? newEngine = null;

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
                    System.Windows.MessageBox.Show("Настройки сохранились, но C# не смог найти поле 'SearchSystem' в файле settings.json.", "Ошибка чтения JSON");
                }
            }
            catch (JsonException ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Настройки] Ошибка при обработке JSON: {ex.Message}");
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
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

    public class AddTabButton
    {
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

    public class RelayCommand : ICommand
    {
        private readonly Action<object?> _execute;
        private readonly Predicate<object?>? _canExecute;

        public RelayCommand(Action<object?> execute, Predicate<object?>? canExecute = null)
        {
            _execute = execute;
            _canExecute = canExecute;
        }

        public bool CanExecute(object? parameter) => _canExecute == null || _canExecute(parameter);
        public void Execute(object? parameter) => _execute(parameter);
        public event EventHandler? CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }
    }
}