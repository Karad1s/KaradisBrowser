using System;
using CefSharp;

namespace Kar.Handlers
{
    /// <summary>
    /// Кастомный обработчик жизненного цикла окон для CefSharp.
    /// Перехватывает запросы на открытие новых всплывающих окон (popups) и маршрутизирует их:
    /// страницы авторизации открываются в отдельных окнах, а остальные ссылки — в новых вкладках браузера.
    /// </summary>
    public class CustomLifeSpanHandler : ILifeSpanHandler
    {
        private readonly Action<string> _onOpenTab;
        private readonly Action<string> _onOpenPopup;

        /// <summary>
        /// Инициализирует новый экземпляр обработчика жизненного цикла.
        /// </summary>
        /// <param name="onOpenTab">Делегат для открытия URL в новой вкладке главного окна.</param>
        /// <param name="onOpenPopup">Делегат для открытия URL в отдельном всплывающем окне (используется для авторизации).</param>
        /// <exception cref="ArgumentNullException">Выбрасывается, если один из переданных делегатов равен null.</exception>
        public CustomLifeSpanHandler(Action<string> onOpenTab, Action<string> onOpenPopup)
        {
            _onOpenTab = onOpenTab ?? throw new ArgumentNullException(nameof(onOpenTab));
            _onOpenPopup = onOpenPopup ?? throw new ArgumentNullException(nameof(onOpenPopup));
        }

        /// <summary>
        /// Вызывается перед созданием нового окна (например, при клике по ссылке с target="_blank" или вызове window.open).
        /// Блокирует стандартное создание окна движком CefSharp и передает управление в UI-поток приложения.
        /// </summary>
        /// <param name="chromiumWebBrowser">Текущий экземпляр элемента управления браузером.</param>
        /// <param name="browser">Объект браузера CefSharp.</param>
        /// <param name="frame">Фрейм, инициировавший открытие окна.</param>
        /// <param name="targetUrl">Целевой URL-адрес для нового окна.</param>
        /// <param name="targetFrameName">Имя целевого фрейма.</param>
        /// <param name="targetDisposition">Расположение цели (например, новая вкладка, новое окно).</param>
        /// <param name="userGesture">Указывает, было ли действие инициировано пользователем (например, кликом).</param>
        /// <param name="popupFeatures">Настройки всплывающего окна (размеры, элементы управления).</param>
        /// <param name="windowInfo">Информация о создаваемом окне.</param>
        /// <param name="browserSettings">Настройки для нового браузера.</param>
        /// <param name="noJavascriptAccess">Флаг, отключающий доступ к JavaScript из родительского окна.</param>
        /// <param name="newBrowser">Выходной параметр для созданного браузера (возвращаем null, так как обрабатываем самостоятельно).</param>
        /// <returns>Возвращает true для отмены стандартного создания всплывающего окна движком CefSharp.</returns>
        public bool OnBeforePopup(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, string targetUrl, string targetFrameName, WindowOpenDisposition targetDisposition, bool userGesture, IPopupFeatures popupFeatures, IWindowInfo windowInfo, IBrowserSettings browserSettings, ref bool noJavascriptAccess, out IWebBrowser newBrowser)
        {
            // Отменяем стандартное создание браузера
            newBrowser = null;

            // Передаем выполнение в главный UI-поток WPF
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                // Если URL относится к авторизации, открываем popup, иначе — новую вкладку
                if (IsAuthUrl(targetUrl))
                {
                    _onOpenPopup(targetUrl);
                }
                else
                {
                    _onOpenTab(targetUrl);
                }
            });

            // Возвращаем true, сообщая CefSharp, что мы сами обработали запрос
            return true;
        }

        /// <summary>
        /// Вызывается после создания окна браузера (не используется).
        /// </summary>
        public void OnAfterCreated(IWebBrowser chromiumWebBrowser, IBrowser browser) { }

        /// <summary>
        /// Вызывается при запросе на закрытие браузера.
        /// </summary>
        /// <returns>Возвращает false, чтобы разрешить стандартное закрытие.</returns>
        public bool DoClose(IWebBrowser chromiumWebBrowser, IBrowser browser) => false;

        /// <summary>
        /// Вызывается перед тем, как окно браузера будет уничтожено (не используется).
        /// </summary>
        public void OnBeforeClose(IWebBrowser chromiumWebBrowser, IBrowser browser) { }

        /// <summary>
        /// Внутренний метод для проверки, является ли URL-адрес страницей авторизации или процессом OAuth.
        /// Сопоставляет URL с параметрами, доменами и ключевыми словами из конфигурации браузера.
        /// </summary>
        /// <param name="url">Проверяемый URL-адрес.</param>
        /// <returns>Возвращает true, если URL идентифицирован как маршрут аутентификации.</returns>
        private bool IsAuthUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return false;

            try
            {
                Uri uri = new Uri(url);
                string host = uri.Host.ToLower();
                string pathAndQuery = uri.PathAndQuery.ToLower();

                // 1. Проверка на наличие универсальных query-параметров OAuth2
                foreach (var parameter in BrowserConfig.OAuthParameters)
                {
                    if (pathAndQuery.Contains(parameter.ToLower()))
                    {
                        return true;
                    }
                }

                // 2. Проверка по списку известных доменов авторизации
                foreach (var authDomain in BrowserConfig.AuthDomains)
                {
                    if (host.Contains(authDomain.ToLower()))
                    {
                        return true;
                    }
                }

                // 3. Проверка по ключевым словам авторизации в пути URL
                string path = uri.AbsolutePath.ToLower();
                foreach (var keyword in BrowserConfig.AuthKeywords)
                {
                    if (path.Contains(keyword.ToLower()))
                    {
                        return true;
                    }
                }
            }
            catch
            {
                // Резервная проверка (Fallback) на случай некорректного или нестандартного формата URL
                string lowerUrl = url.ToLower();
                foreach (var parameter in BrowserConfig.OAuthParameters)
                {
                    if (lowerUrl.Contains(parameter.ToLower())) return true;
                }
                foreach (var authDomain in BrowserConfig.AuthDomains)
                {
                    if (lowerUrl.Contains(authDomain.ToLower())) return true;
                }
                foreach (var keyword in BrowserConfig.AuthKeywords)
                {
                    if (lowerUrl.Contains(keyword.ToLower())) return true;
                }
            }

            return false;
        }
    }
}