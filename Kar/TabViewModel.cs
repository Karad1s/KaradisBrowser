using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using CefSharp;


namespace Kar
{
    public class TabViewModel : INotifyPropertyChanged
    {
        private string? _title;
        private string? _url;
        private IWebBrowser? _browser;

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

        public IWebBrowser? Browser
        {
            get => _browser;
            set { _browser = value; OnPropertyChanged(); }
        }

        public ICommand BackCommand { get; }
        public ICommand ForwardCommand { get; }
        public ICommand ReloadCommand { get; }
        public ICommand HomeCommand { get; }

        public TabViewModel()
        {
            BackCommand = new RelayCommand(obj => Browser?.Back());
            ForwardCommand = new RelayCommand(obj => Browser?.Forward());
            ReloadCommand = new RelayCommand(obj => Browser?.Reload());
            HomeCommand = new RelayCommand(obj => 
            {
                if (Browser != null)
                {
                    Browser.Load("https://www.google.com");
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
