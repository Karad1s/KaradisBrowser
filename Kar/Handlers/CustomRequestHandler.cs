using System;
using System.Security.Cryptography.X509Certificates;
using CefSharp;

namespace Kar.Handlers
{
    /// <summary>
    /// Кастомный обработчик запросов для CefSharp.
    /// Управляет процессом навигации, перехватывая переходы по ссылкам 
    /// и перенаправляя их в новые вкладки пользовательского интерфейса.
    /// </summary>
    internal class CustomRequestHandler : IRequestHandler
    {
        private readonly Action<string>? _onAddNewTab;

        /// <summary>
        /// Инициализирует новый экземпляр обработчика запросов.
        /// </summary>
        /// <param name="onAddNewTab">Делегат, вызываемый для открытия URL-адреса в новой вкладке WPF-приложения.</param>
        public CustomRequestHandler(Action<string>? onAddNewTab = null)
        {
            _onAddNewTab = onAddNewTab;
        }

        /// <summary>Запрос учетных данных для авторизации (не используется).</summary>
        public bool GetAuthCredentials(IWebBrowser chromiumWebBrowser, IBrowser browser, string originUrl, bool isProxy, string host, int port, string realm, string scheme, IAuthCallback callback) => false;

        /// <summary>Получение обработчика ресурсов (не используется).</summary>
        public IResourceRequestHandler GetResourceRequestHandler(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, IRequest request, bool isNavigation, bool isDownload, string requestInitiator, ref bool disableDefaultHandling) => null;

        /// <summary>
        /// Вызывается перед началом любой навигации в браузере.
        /// Используется для перехвата кликов по ссылкам: если пользователь кликает по обычной ссылке 
        /// (не поисковому запросу Google или DuckDuckGo), навигация в текущей вкладке отменяется, 
        /// и URL передается в главное окно для открытия в новой вкладке.
        /// </summary>
        /// <param name="chromiumWebBrowser">Текущий экземпляр элемента управления браузером.</param>
        /// <param name="browser">Объект браузера CefSharp.</param>
        /// <param name="frame">Фрейм, в котором происходит навигация.</param>
        /// <param name="request">Объект запроса, содержащий целевой URL.</param>
        /// <param name="userGesture">Указывает, было ли действие инициировано пользователем (например, клик мыши).</param>
        /// <param name="isRedirect">Указывает, является ли запрос серверным или клиентским редиректом.</param>
        /// <returns>Возвращает true для отмены стандартной навигации, или false для ее продолжения.</returns>
        public bool OnBeforeBrowse(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, IRequest request, bool userGesture, bool isRedirect)
        {
            // Проверяем, что это осознанный клик пользователя в главном окне (не скрытом фрейме) и не автоматический редирект
            if (userGesture && !isRedirect && frame.IsMain)
            {
                string url = request.Url.ToLower();

                // Исключаем из перехвата поисковые запросы, позволяя им открываться в текущей вкладке
                if (!url.Contains("google.com/search") && !url.Contains("duckduckgo.com/?q="))
                {
                    // Передаем URL в UI-поток для создания новой вкладки
                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        _onAddNewTab?.Invoke(request.Url);
                    });

                    // Возвращаем true, чтобы заблокировать переход по ссылке в текущей вкладке
                    return true;
                }
            }

            // Разрешаем стандартную навигацию для всех остальных случаев
            return false;
        }

        /// <summary>Обработка ошибок сертификата (не используется, возвращается false для стандартного поведения движка).</summary>
        public bool OnCertificateError(IWebBrowser chromiumWebBrowser, IBrowser browser, CefErrorCode errorCode, string url, ISslInfo sslInfo, IRequestCallback callback) => false;

        /// <summary>Вызывается, когда DOM-документ становится доступен в главном фрейме (не используется).</summary>
        public void OnDocumentAvailableInMainFrame(IWebBrowser chromiumWebBrowser, IBrowser browser) { }

        /// <summary>Вызывается при попытке открыть URL из вкладки (не используется).</summary>
        public bool OnOpenUrlFromTab(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, string targetUrl, WindowOpenDisposition targetDisposition, bool userGesture) => false;

        /// <summary>Вызывается при непредвиденном завершении (краше) процесса рендеринга страницы (не используется).</summary>
        public void OnRenderProcessTerminated(IWebBrowser chromiumWebBrowser, IBrowser browser, CefTerminationStatus status, int errorCode, string errorMessage) { }

        /// <summary>Вызывается при запросе клиентского SSL-сертификата (не используется).</summary>
        public bool OnSelectClientCertificate(IWebBrowser chromiumWebBrowser, IBrowser browser, bool isProxy, string host, int port, X509Certificate2Collection certificates, ISelectClientCertificateCallback callback) => false;

        /// <summary>Вызывается, когда процесс рендеринга готов к работе (не используется).</summary>
        public void OnRenderViewReady(IWebBrowser chromiumWebBrowser, IBrowser browser) { }
    }
}