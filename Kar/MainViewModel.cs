using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace Kar
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private TabViewModel? _selectedTab;
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

        public ICommand AddTabCommand { get; }
        public ICommand CloseTabCommand { get; }
        public ICommand SelectedTabCommand { get; }

        public MainViewModel(MainWindow window)
        {
            this.mainWindow = window;
            AddTabCommand = new RelayCommand(obj => AddNewTab("https://www.google.com"));
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

            AddNewTab("https://www.google.com");
        }

        public void AddNewTab(string url)
        {
            var newTab = new TabViewModel { Title = "Новая вкладка", Url = url };
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
