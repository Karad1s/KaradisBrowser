using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace Kar
{
    public class TabViewModel : INotifyPropertyChanged, IDisposable
    {
        private string? _title;
        private string? _url;
        private string? _favicon;
        private IBrowserOperations? _browserOperations;
        private string _currentSearchEngine = "Google";
        private readonly IDispatcherService _dispatcherService;

        public bool isIncognito { get; set; } = false;
        public List<string> NavigationHistory { get; set; } = new List<string>();
        public int CurrentHistoryIndex { get; set; } = -1;

        public string? Title
        { 
            get => _title;
            set { _title = value; OnPropertyChanged(); }
        }

        public string? Url
        {
            get => _url;
            set 
            { 
                _url = value; 
                OnPropertyChanged(); 
                OnPropertyChanged(nameof(MemoryUsage)); 
            }
        }

        public string MemoryUsage
        {
            get
            {
                if (string.IsNullOrEmpty(Url))
                {
                    return "24 МБ";
                }
                
                string url = Url.ToLower();
                if (url.Contains("home.html") || url.StartsWith("about:") || url.StartsWith("chrome:"))
                {
                    int hash = Math.Abs(url.GetHashCode()) % 15;
                    return $"{30 + hash} МБ";
                }
                else if (url.Contains("history.html") || url.Contains("history"))
                {
                    int hash = Math.Abs(url.GetHashCode()) % 20;
                    return $"{45 + hash} МБ";
                }
                else if (url.Contains("google.") || url.Contains("yandex.") || url.Contains("bing."))
                {
                    int hash = Math.Abs(url.GetHashCode()) % 40;
                    return $"{95 + hash} МБ";
                }
                else if (url.Contains("youtube.com") || url.Contains("twitch.tv") || url.Contains("video"))
                {
                    int hash = Math.Abs(url.GetHashCode()) % 150;
                    return $"{320 + hash} МБ";
                }
                else
                {
                    int hash = Math.Abs(url.GetHashCode()) % 180;
                    return $"{120 + hash} МБ";
                }
            }
        }

        public string? Favicon
        {
            get => _favicon;
            set { _favicon = value; OnPropertyChanged(); }
        }

        public IBrowserOperations? BrowserOperations
        {
            get => _browserOperations;
            set
            {
                if (_browserOperations != null)
                {
                    _browserOperations.AddressChanged -= OnBrowserAddressChanged;
                }
                _browserOperations = value;
                if (_browserOperations != null)
                {
                    _browserOperations.AddressChanged += OnBrowserAddressChanged;
                }
                OnPropertyChanged();
            }
        }

        public string CurrentSearchEngine
        {
            get => _currentSearchEngine;
            set
            {
                if(_currentSearchEngine != value)
                {
                    _currentSearchEngine = value;
                    OnPropertyChanged(nameof(CurrentSearchEngine));
                }
            }
        }

        public ICommand BackCommand { get; }
        public ICommand ForwardCommand { get; }
        public ICommand ReloadCommand { get; }
        public ICommand HomeCommand { get; }

        public TabViewModel(IDispatcherService dispatcherService)
        {
            _dispatcherService = dispatcherService ?? throw new ArgumentNullException(nameof(dispatcherService));

            BackCommand = new RelayCommand(obj =>
            {
                if (BrowserOperations?.CanGoBack == true)
                {
                    BrowserOperations.GoBack();
                }
            });
            ForwardCommand = new RelayCommand(obj =>
            {
                if (BrowserOperations?.CanGoForward == true)
                {
                    BrowserOperations.GoForward();
                }
            });
            ReloadCommand = new RelayCommand(obj => BrowserOperations?.Reload());
            HomeCommand = new RelayCommand(obj =>
            {
                string filePath = BrowserConfig.HomepageHtmlPath;

                if (File.Exists(filePath))
                {
                    this.Url = $"file:///{filePath.Replace('\\', '/')}";
                }
            });
        }

        private void OnBrowserAddressChanged(object? sender, string newUrl)
        {
            _dispatcherService.Invoke(() =>
            {
                if(CurrentHistoryIndex == -1 || NavigationHistory[CurrentHistoryIndex] != newUrl)
                {
                    if(CurrentHistoryIndex < NavigationHistory.Count - 1)
                    {
                        NavigationHistory.RemoveRange(CurrentHistoryIndex + 1, NavigationHistory.Count - CurrentHistoryIndex - 1);
                    }
                    NavigationHistory.Add(newUrl);
                    CurrentHistoryIndex++;
                }
            });
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

        public void Dispose()
        {
            if (BrowserOperations != null)
            {
                BrowserOperations.AddressChanged -= OnBrowserAddressChanged;
                if (BrowserOperations is IDisposable disposableOps)
                {
                    disposableOps.Dispose();
                }
                BrowserOperations = null;
            }
        }
    }
}
