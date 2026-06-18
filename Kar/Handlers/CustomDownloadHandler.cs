using CefSharp;
using System;
using System.Windows;

namespace Kar.Handlers
{
    /// <summary>
    /// Кастомный обработчик загрузки файлов для CefSharp.
    /// Перехватывает процессы скачивания и передает данные о состоянии загрузки в UI-поток.
    /// </summary>
    public class CustomDownloadHandler : IDownloadHandler
    {
        /// <summary>
        /// Событие, передающее информацию о текущем состоянии загрузки.
        /// Используется для обновления элементов интерфейса главного окна (например, прогресс-бара).
        /// </summary>
        public event EventHandler<DownloadItem> DownloadStateChanged;

        /// <summary>
        /// Проверяет, разрешено ли скачивание по данному URL.
        /// В текущей реализации ограничения отсутствуют, загрузка разрешена всегда.
        /// </summary>
        /// <param name="chromiumWebBrowser">Текущий экземпляр элемента управления браузером.</param>
        /// <param name="browser">Объект браузера CefSharp.</param>
        /// <param name="url">URL-адрес для скачивания.</param>
        /// <param name="requestMethod">HTTP-метод запроса.</param>
        /// <returns>Возвращает true, разрешая скачивание.</returns>
        public bool CanDownload(IWebBrowser chromiumWebBrowser, IBrowser browser, string url, string requestMethod)
        {
            return true;
        }

        /// <summary>
        /// Вызывается перед началом сохранения файла на диск.
        /// Открывает системное диалоговое окно, позволяя пользователю выбрать директорию сохранения.
        /// </summary>
        /// <param name="chromiumWebBrowser">Текущий экземпляр элемента управления браузером.</param>
        /// <param name="browser">Объект браузера CefSharp.</param>
        /// <param name="downloadItem">Информация о загружаемом файле.</param>
        /// <param name="callback">Колбэк для управления процессом загрузки.</param>
        /// <returns>Возвращает true для продолжения обработки скачивания.</returns>
        public bool OnBeforeDownload(IWebBrowser chromiumWebBrowser, IBrowser browser, DownloadItem downloadItem, IBeforeDownloadCallback callback)
        {
            if (!callback.IsDisposed)
            {
                // Указываем движку продолжить загрузку, открыв диалоговое окно для выбора пути
                callback.Continue(downloadItem.SuggestedFileName, showDialog: true);
            }
            return true;
        }

        /// <summary>
        /// Вызывается движком CefSharp в фоновом потоке при обновлении статуса загрузки файла.
        /// Перенаправляет данные в главный UI-поток WPF для безопасного обновления интерфейса.
        /// </summary>
        /// <param name="chromiumWebBrowser">Текущий экземпляр элемента управления браузером.</param>
        /// <param name="browser">Объект браузера CefSharp.</param>
        /// <param name="downloadItem">Актуальная информация о загружаемом файле (прогресс, скорость, статус).</param>
        /// <param name="callback">Колбэк для управления загрузкой.</param>
        public void OnDownloadUpdated(IWebBrowser chromiumWebBrowser, IBrowser browser, DownloadItem downloadItem, IDownloadItemCallback callback)
        {
            System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
            {
                DownloadStateChanged?.Invoke(this, downloadItem);
            });
        }
    }
}