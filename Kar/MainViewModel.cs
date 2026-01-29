using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace Kar
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private TabViewModel _selectedTab;
        private MainWindow mainWindow;
        public ObservableCollection<TabViewModel> Tabs { get; set; } = new ObservableCollection<TabViewModel>();

        public TabViewModel SelectedTab
        {
            get => _selectedTab;
            set
            {
                _selectedTab = value;
                OnPropertyChanged(nameof(SelectedTab));
            }
        }

        public ObservableCollection<SearchSystem> SearchSystems { get; set; }

        private SearchSystem _selectedSearchSystem;
        public SearchSystem SelectedSearchSystem
        {
            get => _selectedSearchSystem;
            set
            {
                _selectedSearchSystem = value;
                OnPropertyChanged(); 
            }
        }

        public ICommand AddTabCommand { get; }
        public ICommand CloseTabCommand { get; }
        public ICommand SelectedTabCommand { get; }

        public MainViewModel(MainWindow window)
        {
            this.mainWindow = window;
            AddTabCommand = new RelayCommand(obj => AddNewTab(string.Empty));
            CloseTabCommand = new RelayCommand(obj =>
            {
                if (obj is TabViewModel tab) 
                {
                    if(Tabs.Count() == 1) mainWindow?.Close();
                    else Tabs.Remove(tab);

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

            SearchSystems = new ObservableCollection<SearchSystem>
           {
                new SearchSystem("Google","https://www.google.com/search?q="),
                new SearchSystem("Bing","https://www.bing.com/search?q="),
                new SearchSystem("DuckDuckGo","https://duckduckgo.com/?q="),
                new SearchSystem("Yandex","https://www.yandex.com/search?q="),
           };
            SelectedSearchSystem = SearchSystems[0];

            AddNewTab(string.Empty);
        }

        public void AddNewTab(string url)
        {
            var newTab = new TabViewModel { Title = "Новая вкладка", Url = url };

            if (string.IsNullOrEmpty(url) || url == "about:home")
            {
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                newTab.Url = $"file:///{System.IO.Path.Combine(baseDir,"Homepage", "home.html").Replace('\\','/')}";
            }

            Tabs.Add(newTab);
            SelectedTab = newTab;
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
                } );
            }
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
