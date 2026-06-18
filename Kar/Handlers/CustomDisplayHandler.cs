using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Threading;
using CefSharp;
using CefStruct = CefSharp.Structs;

namespace Kar.Handlers
{
    /// <summary>
    /// Кастомный обработчик событий отображения CefSharp.
    /// Отвечает за переключение полноэкранного режима и обновление иконки (Favicon) вкладки.
    /// </summary>
    public class CustomDisplayHandler : IDisplayHandler
    {
        private readonly TabViewModel _tab;
        private readonly Dispatcher _dispatcher;
        private readonly Action<bool>? _onFullscreenModeChange;

        /// <summary>
        /// Конструктор. Принимает вью-модель вкладки, диспетчер и колбэк для полноэкранного режима.
        /// </summary>
        /// <param name="tab">Вью-модель текущей вкладки.</param>
        /// <param name="dispatcher">WPF Dispatcher для вызова кода в UI-потоке.</param>
        /// <param name="onFullscreenModeChange">Колбэк для переключения полноэкранного режима.</param>
        public CustomDisplayHandler(TabViewModel tab, Dispatcher dispatcher, Action<bool>? onFullscreenModeChange = null)
        {
            _tab = tab ?? throw new ArgumentNullException(nameof(tab));
            _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
            _onFullscreenModeChange = onFullscreenModeChange;
        }

        /// <summary>
        /// Вызывается при переключении полноэкранного режима на веб-странице (например, видео на YouTube).
        /// Передает управление колбэку главного окна.
        /// </summary>
        public void OnFullscreenModeChange(IWebBrowser chromiumWebBrowser, IBrowser browser, bool fullscreen)
        {
            if (_onFullscreenModeChange != null)
            {
                _dispatcher.Invoke(() => _onFullscreenModeChange(fullscreen));
            }
        }

        /// <summary>Обработчик смены адреса (не используется, обрабатывается в другом месте).</summary>
        public void OnAddressChanged(IWebBrowser chromiumWebBrowser, AddressChangedEventArgs addressChangedArgs) { }

        /// <summary>Обработчик авторесайза (не используется).</summary>
        public bool OnAutoResize(IWebBrowser chromiumWebBrowser, IBrowser browser, CefStruct.Size newSize) => false;

        /// <summary>Обработчик смены курсора (не используется).</summary>
        public bool OnCursorChange(IWebBrowser chromiumWebBrowser, IBrowser browser, nint cursor, CefSharp.Enums.CursorType type, CefStruct.CursorInfo customCursorInfo) => false;

        /// <summary>Обработчик смены заголовка страницы (не используется, обрабатывается через TitleChanged событие).</summary>
        public void OnTitleChanged(IWebBrowser chromiumWebBrowser, TitleChangedEventArgs titleChangedArgs) { }

        /// <summary>
        /// Вызывается при изменении иконки (Favicon) веб-страницы.
        /// Выбирает PNG-иконку (если есть), иначе первую доступную.
        /// </summary>
        public void OnFaviconUrlChange(IWebBrowser chromiumWebBrowser, IBrowser browser, IList<string> urls) 
        {
            if(urls.Count > 0) 
            {
                _dispatcher.Invoke(() =>
                {
                    // Предпочитаем PNG формат для лучшего качества отображения
                    string bestIcon = urls.FirstOrDefault(u => u.ToLower().Contains(".png")) ?? urls[0];
                    _tab.Favicon = bestIcon;
                });
            }
        }

        /// <summary>Обработчик прогресса загрузки страницы (не используется).</summary>
        public void OnLoadingProgressChange(IWebBrowser chromiumWebBrowser, IBrowser browser, double progress) { }

        /// <summary>Обработчик тултипов (не используется).</summary>
        public bool OnTooltipChanged(IWebBrowser chromiumWebBrowser, ref string text) => false;

        /// <summary>Обработчик сообщений строки статуса (не используется).</summary>
        public void OnStatusMessage(IWebBrowser chromiumWebBrowser, StatusMessageEventArgs statusMessageArgs) { }

        /// <summary>Обработчик консольных сообщений JavaScript (не используется).</summary>
        public bool OnConsoleMessage(IWebBrowser chromiumWebBrowser, ConsoleMessageEventArgs consoleMessageArgs) => false;
    }
}
