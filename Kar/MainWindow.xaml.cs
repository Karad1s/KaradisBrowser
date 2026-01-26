using CefSharp;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows;
using System.Windows.Automation.Provider;
using System.Windows.Data;

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

        public MainViewModel ViewModel { get; set; }

        public MainWindow()
        {
            ViewModel = new MainViewModel(this);
            InitializeComponent();
            this.DataContext = ViewModel;
            ChangedSearchSystem.ItemsSource = new SearchSystem[]
            {
                new SearchSystem("Google","https://www.google.com/search?q="),
                new SearchSystem("Bing","https://www.bing.com/search?q="),
                new SearchSystem("DuckDuckGo","https://duckduckgo.com/?q="),
                new SearchSystem("Yandex","https://www.yandex.com/search?q="),
            };
            ChangedSearchSystem.SelectedIndex = 0;

        }


        private void MainWindow_StateChanged(object sender, EventArgs e)
        {
            if (this.WindowState == WindowState.Maximized && !_isManualFullscreen)
            {
                this.MaxHeight = SystemParameters.WorkArea.Height;
                this.MaxWidth = SystemParameters.WorkArea.Width;

                if (RootGrid !=null) RootGrid.Margin = new Thickness(8);
            }

            else if (this.WindowState == WindowState.Maximized && _isManualFullscreen)
            {
                this.MaxHeight = double.PositiveInfinity;
                this.MaxWidth = double.PositiveInfinity;
                if (RootGrid != null) RootGrid.Margin = new Thickness(0);
            }   

            else
            {
                this.MaxHeight = double.PositiveInfinity;
                this.MaxWidth = double.PositiveInfinity;
                if (RootGrid != null) RootGrid.Margin = new Thickness(0);
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e) => this.Close();

        private void MinimizeButton_Click(object sender, RoutedEventArgs e) => this.WindowState = WindowState.Minimized;

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

        private bool _isManualFullscreen = false;

        public void SetFullScreen(bool fullscreen)
        {   
            _isManualFullscreen = fullscreen;
            
            if (fullscreen)
            {
                _prevWindowState = WindowState;
                _prevWindowStyle = WindowStyle;
                _prevResizeMode = ResizeMode;

                WindowStyle = WindowStyle.None;
                ResizeMode = ResizeMode.NoResize;

                if (TopRow != null) TopRow.Height = new GridLength(0);
                if (PanelControl != null) PanelControl.Height = new GridLength(0);
                if (TopBar != null) TopBar.Visibility = Visibility.Collapsed;

                if (WindowState == WindowState.Maximized) MainWindow_StateChanged(this, EventArgs.Empty);
                else 
                    WindowState = WindowState.Minimized;

            }
            else
            {
                WindowStyle = _prevWindowStyle;
                ResizeMode = _prevResizeMode;

                if (TopRow != null) TopRow.Height = new GridLength(36);
                if (PanelControl != null) PanelControl.Height = new GridLength(35);
                if (TopBar != null) TopBar.Visibility = Visibility.Visible;

                this.WindowState = _prevWindowState;

                MainWindow_StateChanged(this, EventArgs.Empty);
            }
        }

        private void ChangedSearchSystem_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if(ChangedSearchSystem.SelectedItem is "Google") {}
        }
    }

    public class TabSelectionConverter : IMultiValueConverter
    {
       public object Convert(object[] values, Type targetType,object parameter, CultureInfo culture)
        {
            if(values.Length <2) return false;
            return values[0] == values[1];
        }
        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
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
}
       