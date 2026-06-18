using System;
using System.Windows;
using CefSharp;
using CefSharp.Wpf;

namespace Kar
{
    /// <summary>
    /// Предоставляет конкретную реализацию операций браузера, используя CefSharp ChromiumWebBrowser.
    /// </summary>
    public class CefSharpBrowserOperations : IBrowserOperations, IDisposable
    {
        private readonly ChromiumWebBrowser _webBrowser;

        /// <summary>
        /// Конструктор класса. Подписывается на событие изменения адреса браузера CefSharp.
        /// </summary>
        /// <param name="webBrowser">Экземпляр контрола ChromiumWebBrowser.</param>
        public CefSharpBrowserOperations(ChromiumWebBrowser webBrowser)
        {
            _webBrowser = webBrowser ?? throw new ArgumentNullException(nameof(webBrowser));
            _webBrowser.AddressChanged += OnCefAddressChanged;
        }

        /// <summary>
        /// Обработчик изменения адреса в компоненте ChromiumWebBrowser.
        /// Транслирует событие во внешнее событие AddressChanged.
        /// </summary>
        private void OnCefAddressChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue is string address)
            {
                AddressChanged?.Invoke(this, address);
            }
        }

        /// <summary>
        /// Возвращает значение, указывающее, можно ли перейти назад по истории.
        /// </summary>
        public bool CanGoBack => _webBrowser.CanGoBack;

        /// <summary>
        /// Возвращает значение, указывающее, можно ли перейти вперед по истории.
        /// </summary>
        public bool CanGoForward => _webBrowser.CanGoForward;

        /// <summary>
        /// Выполняет переход на предыдущую страницу в истории.
        /// </summary>
        public void GoBack() => _webBrowser.Back();

        /// <summary>
        /// Выполняет переход на следующую страницу в истории.
        /// </summary>
        public void GoForward() => _webBrowser.Forward();

        /// <summary>
        /// Перезагружает текущую веб-страницу.
        /// </summary>
        public void Reload() => _webBrowser.Reload();

        /// <summary>
        /// Событие, возникающее при изменении текущего URL-адреса браузера.
        /// </summary>
        public event EventHandler<string>? AddressChanged;

        /// <summary>
        /// Освобождает ресурсы, отписываясь от событий браузера.
        /// </summary>
        public void Dispose()
        {
            _webBrowser.AddressChanged -= OnCefAddressChanged;
        }
    }
}
