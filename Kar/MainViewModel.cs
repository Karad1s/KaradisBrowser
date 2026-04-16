using CefSharp;
using Kar.Settings; 
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.Json;
using WPF = System.Windows;
using System.Windows.Data;
using System.Windows.Input;

namespace Kar
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private TabViewModel _selectedTab;
        private MainWindow mainWindow;
        private IWebBrowser? _browser;

        public  SettingsBridge AppSettingsBridge { get;}

        private string _globalSearchEngine = "Google";
        public string GlobalSearchEngine
        {
            get => _globalSearchEngine = "Google";
            private set
            {
                if (_globalSearchEngine != value)
                {
                    _globalSearchEngine = value;
                    OnPropertyChanged();
                }
            }
        }

        public List<string> SearchSystems { get; } = new List<string>
        {
            "Google","Bing","DuckDuckGo","Yandex","Yahoo","Ask"
        };

        public ObservableCollection<TabViewModel> Tabs { get; set; } = new ObservableCollection<TabViewModel>();
        public CompositeCollection TabItems { get; set; }

        public ObservableCollection<DownloadItemModel> RecentDownloads { get; set; } = new ObservableCollection<DownloadItemModel>();

        public TabViewModel SelectedTab
        {
            get => _selectedTab;
            set
            {
                _selectedTab = value;
                OnPropertyChanged(nameof(SelectedTab));
            }
        }
        
        public IWebBrowser? Browser
        {
            get => _browser;
            set { _browser = value; OnPropertyChanged(); }
        }
        public ICommand AddTabCommand { get; }
        public ICommand CloseTabCommand { get; }
        public ICommand SelectedTabCommand { get; }
        public ICommand BackCommand { get; }
        public ICommand ForwardCommand { get; }
        public ICommand ReloadCommand { get; }
        public ICommand HomeCommand { get; }
        public ICommand SettingsCommand { get; }

        public ICommand ExtentionsCommand { get; }

        public ICommand ShowAllDownloadsCommand { get; }
        public ICommand OpenDownloadsFolderCommand { get; }

        public MainViewModel(MainWindow window)
        {
            this.mainWindow = window;

            string settingsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Settings");
            if (!Directory.Exists(settingsDir)) Directory.CreateDirectory(settingsDir);


            string settingsPath = Path.Combine(settingsDir, "Settings.json");
            var fileService = new FileSettingsService(settingsPath);

            AppSettingsBridge = new SettingsBridge(fileService);
            AppSettingsBridge.OnSettingsSaved += HandleSettingsUpdate;

            HandleSettingsUpdate(AppSettingsBridge.GetSettings());

            AddTabCommand = new RelayCommand(obj => AddNewTab(string.Empty));
            CloseTabCommand = new RelayCommand(obj =>
            {
                if (obj is TabViewModel tab)
                {
                    int index = Tabs.IndexOf(tab);

                    if (SelectedTab == tab)
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

                    if (Tabs.Count == 0) {
                        mainWindow.Close();
                    }
                }
                ;
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
                var Url = $"file:///{Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Settings", "settings.html").Replace('\\', '/')}";
                AddNewTab(Url);
            });

            ExtentionsCommand = new RelayCommand(obj =>
            {
                var Url = "https://chromewebstore.google.com/category/extensions";
                AddNewTab(Url);
            });

            ShowAllDownloadsCommand = new RelayCommand(obj =>
            {
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                string downloadsPageUrl = $"file:///{Path.Combine(baseDir, "Library", "library.html").Replace('\\', '/')}";

                AddNewTab(downloadsPageUrl);
            });

            OpenDownloadsFolderCommand = new RelayCommand(obj =>
            {
            string userDownloadsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
            OpenFolderExplorer(userDownloadsPath);
            });


            TabItems = new CompositeCollection();
            var cont = new CollectionContainer { Collection = Tabs };
            TabItems.Add(cont);
            TabItems.Add(new AddTabButton());

            AddNewTab(string.Empty);
        }
        public string FormatSearchQuery(string userInpur, string localSearchEngine)
        {
            if(string.IsNullOrWhiteSpace(userInpur)) return string.Empty;

            if(userInpur.Contains(".")&& userInpur.Contains(" "))
            {
                return userInpur.StartsWith("http")? userInpur : $"https://{userInpur}";
            }

            string encodedQuery = Uri.EscapeDataString(userInpur);

            return localSearchEngine switch
            {
                "Google" => $"https://www.google.com/search?q={encodedQuery}",
                "Bing" => $"https://www.bing.com/search?q={encodedQuery}",
                "DuckDuckGo" => $"https://duckduckgo.com/?q={encodedQuery}",
                "Yandex" => $"https://www.yandex.com/search?text={encodedQuery}",
                "Yahoo" => $"https://search.yahoo.com/search?p={encodedQuery}",
                "Ask" => $"https://www.ask.com/web?q={encodedQuery}",
                _ => $"https://www.google.com/search?q={encodedQuery}"
            };  
        }
        public void AddNewTab(string url)
        {
            
            string currentEngine = this.GlobalSearchEngine;
            
            string FinalUrl= FormatSearchQuery(url, currentEngine);

            var newTab = new TabViewModel { Title = "Новая вкладка", Url = url , CurrentSearchEngine = currentEngine };          

            if (string.IsNullOrEmpty(url) || url == "about:home")
            {
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                newTab.Url = $"file:///{Path.Combine(baseDir, "Homepage", "home.html").Replace('\\', '/')}";
            }

            Tabs.Add(newTab);
            SelectedTab = newTab;
        }

        public void OpenFolderExplorer(string filePath)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(filePath)) return;

                if(File.Exists(filePath))
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

        private void HandleSettingsUpdate(string jsonContent)
        {
            try
            {
                using var JsonDoc = JsonDocument.Parse(jsonContent);
                string? newEngine = null;

                if (JsonDoc.RootElement.TryGetProperty("SearchSystem", out var searchSystem)){
                    if (searchSystem.ValueKind == JsonValueKind.Object && searchSystem.TryGetProperty("content", out var content) &&
                content.TryGetProperty("value", out var value)) newEngine = value.GetString();
                    else if (searchSystem.ValueKind == JsonValueKind.String) newEngine = searchSystem.GetString();
                    
                }

                if (!string.IsNullOrEmpty(newEngine))
                {
                    GlobalSearchEngine = newEngine;
                    System.Diagnostics.Debug.WriteLine($"[Настройки] Обновлена поисковая система: {GlobalSearchEngine}");

                    WPF.Application.Current.Dispatcher.Invoke(() =>
                    {
                        foreach (var tab in Tabs)
                        {
                            tab.CurrentSearchEngine = GlobalSearchEngine;
                        }
                    });
                }
                else
                {
                    WPF.MessageBox.Show("Настройки сохранились, но C# не смог найти поле 'SearchSystem' в файле settings.json.", "Ошибка чтения JSON");
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
            if (WPF.Application.Current.Dispatcher.CheckAccess())
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(Name));
            }
            else
            {
                WPF.Application.Current.Dispatcher.Invoke(() =>
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
