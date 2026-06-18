using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace Kar
{
    /// <summary>
    /// Вью-модель для отдельной вкладки браузера.
    /// Отвечает за состояние вкладки (заголовок, URL, иконка, история навигации) и навигационные команды.
    /// </summary>
    public class TabViewModel : INotifyPropertyChanged, IDisposable
    {
        private string? _title;
        private string? _url;
        private string? _favicon;
        private IBrowserOperations? _browserOperations;
        private string _currentSearchEngine = "Google";
        private readonly IDispatcherService _dispatcherService;

        /// <summary>
        /// Флаг режима инкогнито (приватного просмотра).
        /// </summary>
        public bool isIncognito { get; set; } = false;

        /// <summary>
        /// Список истории навигации внутри текущей вкладки (хранит посещенные URL).
        /// </summary>
        public List<string> NavigationHistory { get; set; } = new List<string>();

        /// <summary>
        /// Текущий индекс в списке истории навигации NavigationHistory.
        /// </summary>
        public int CurrentHistoryIndex { get; set; } = -1;

        /// <summary>
        /// Заголовок веб-страницы вкладки.
        /// </summary>
        public string? Title
        { 
            get => _title;
            set { _title = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Текущий адрес (URL) веб-страницы вкладки.
        /// </summary>
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

        /// <summary>
        /// Эмулирует расчет использования оперативной памяти вкладкой на основе ее URL-адреса.
        /// </summary>
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

        /// <summary>
        /// Путь к файлу или URL иконки (Favicon) страницы.
        /// </summary>
        public string? Favicon
        {
            get => _favicon;
            set { _favicon = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Интерфейс для взаимодействия с движком браузера CefSharp.
        /// Настраивает подписку на изменение адреса.
        /// </summary>
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

        /// <summary>
        /// Поисковая система, выбранная для этой вкладки.
        /// </summary>
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

        /// <summary>
        /// Команда для перехода назад по истории вкладки.
        /// </summary>
        public ICommand BackCommand { get; }

        /// <summary>
        /// Команда для перехода вперед по истории вкладки.
        /// </summary>
        public ICommand ForwardCommand { get; }

        /// <summary>
        /// Команда перезагрузки страницы.
        /// </summary>
        public ICommand ReloadCommand { get; }

        /// <summary>
        /// Команда перехода на домашнюю страницу.
        /// </summary>
        public ICommand HomeCommand { get; }

        /// <summary>
        /// Инициализирует вью-модель вкладки и привязывает навигационные команды к операциям браузера.
        /// </summary>
        /// <param name="dispatcherService">Сервис для вызова операций в главном потоке.</param>
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

        /// <summary>
        /// Обработчик события изменения адреса CEF. 
        /// Обновляет локальную историю навигации вкладки (NavigationHistory) для работы кнопок Вперед/Назад.
        /// </summary>
        private void OnBrowserAddressChanged(object? sender, string newUrl)
        {
            _dispatcherService.Invoke(() =>
            {
                if(CurrentHistoryIndex == -1 || NavigationHistory[CurrentHistoryIndex] != newUrl)
                {
                    // Если пользователь перешел по новой ссылке после перехода назад, удаляем более позднюю историю
                    if(CurrentHistoryIndex < NavigationHistory.Count - 1)
                    {
                        NavigationHistory.RemoveRange(CurrentHistoryIndex + 1, NavigationHistory.Count - CurrentHistoryIndex - 1);
                    }
                    NavigationHistory.Add(newUrl);
                    CurrentHistoryIndex++;
                }
            });
        }

        /// <summary>
        /// Событие изменения свойств для обновления привязок в UI.
        /// </summary>
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// Безопасный вызов события PropertyChanged с учетом потока UI.
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

        /// <summary>
        /// Освобождает ресурсы вкладки и отписывается от событий CEF.
        /// </summary>
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
