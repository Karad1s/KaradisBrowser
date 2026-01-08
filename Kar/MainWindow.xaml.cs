using System.Text;
using System.Windows;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Kar
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private TabViewModel _selectedTab;
        public TabViewModel SelectedTab
        {
            get => _selectedTab;
            set
            {
                _selectedTab = value;
                OnPropertyChanged(nameof(SelectedTab));
            }
        }

        public MainWindow() 
        {

            InitializeComponent();

            Tabs = new ObservableCollection<TabViewModel>();

            this.DataContext = this;
            AddNewTab("https://www.google.com");
        }
        public ObservableCollection<TabViewModel> Tabs { get; set; }
        public void AddNewTab(string url)
        {
            var newTab = new TabViewModel { Title = "Новая вкладка", Url = url };
            Tabs.Add(newTab);
            SelectedTab = newTab;
        }

        private void AddTabButton_Click(object sender, RoutedEventArgs e)
        {
            AddNewTab("https://www.google.com");
        }

        private void CloseTabButton_Click(object sender, RoutedEventArgs e)
        {
            if (Tabs.Count > 1)
            {
                var tabViewModel = (sender as FrameworkElement).DataContext as TabViewModel;
                Tabs.Remove(tabViewModel);
            }
            else
            {
                this.Close();
            }


        }

        private void MainWindow_StateChanged(object sender, EventArgs e)
        {
            if (this.WindowState == WindowState.Maximized)
            {
                var thickness = SystemParameters.WindowResizeBorderThickness;
                this.BorderThickness = new Thickness(thickness.Left, thickness.Top, thickness.Right, thickness.Bottom);
            }
            else
            {
                this.BorderThickness = new Thickness(0);
            }
        }

        private void Browser_Loaded(object sender, RoutedEventArgs e)
        {

        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e) => this.WindowState = WindowState.Minimized;

        private void MaximizeButton_Click(object sender, RoutedEventArgs e) =>
            this.WindowState = (this.WindowState == WindowState.Maximized) ? WindowState.Normal : WindowState.Maximized;


        private void CloseButton_Click(object sender, RoutedEventArgs e) => this.Close();

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        
    }
}