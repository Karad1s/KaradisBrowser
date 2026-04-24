using CefSharp;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;



namespace Kar
{
    public class TabViewModel : INotifyPropertyChanged
    {
        private string? _title;
        private string? _url;
        private string? _favicon;
        private IWebBrowser? _browser;

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
            set { _url = value; OnPropertyChanged(); }
        }

        public string? Favicon
        {
            get => _favicon;
            set { _favicon = value; OnPropertyChanged(); }
        }

        public IWebBrowser? Browser
        {
            get => _browser;
            set { _browser = value; OnPropertyChanged(); }
        }

        private string _currentSearchEngine = "Google";

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

        public TabViewModel()
        {
            BackCommand = new RelayCommand(obj =>
            {
                
                if (Browser?.CanGoBack == true)
                {
                    Browser.Back();
                }
            });
            ForwardCommand = new RelayCommand(obj =>
            {
                if (Browser?.CanGoForward == true)
                {
                    Browser.Forward();
                }
            });
            ReloadCommand = new RelayCommand(obj => Browser?.Reload());
            HomeCommand = new RelayCommand(obj =>
            {
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                string filePath = System.IO.Path.Combine(baseDir, "Homepage", "home.html");

                if (System.IO.File.Exists(filePath))
                {
                    this.Url = $"file:///{filePath.Replace('\\', '/')}";
                }
            });
        }

        private void OnBrowserAddresChanged(object sender, AddressChangedEventArgs e)
        {
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                string NewUrl = e.Address;

                if(CurrentHistoryIndex == -1 || NavigationHistory[CurrentHistoryIndex] != NewUrl)
                {
                    if(CurrentHistoryIndex < NavigationHistory.Count - 1)
                    {
                        NavigationHistory.RemoveRange(CurrentHistoryIndex + 1, NavigationHistory.Count - CurrentHistoryIndex - 1);
                    }
                    NavigationHistory.Add(NewUrl);
                    CurrentHistoryIndex++;
                }
            });
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? Name = null)
        {
            if (System.Windows.Application.Current.Dispatcher.CheckAccess())
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(Name));
            }
            else
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(Name));
                });
            }
        }
    }
}
