using System;

namespace Kar
{
    /// <summary>
    /// Интерфейс, определяющий набор основных операций управления браузером,
    /// абстрагирующий логику CefSharp от ViewModel.
    /// </summary>
    public interface IBrowserOperations
    {
        /// <summary>
        /// Указывает, доступен ли переход назад по истории.
        /// </summary>
        bool CanGoBack { get; }

        /// <summary>
        /// Указывает, доступен ли переход вперед по истории.
        /// </summary>
        bool CanGoForward { get; }

        /// <summary>
        /// Выполнить переход назад в истории навигации.
        /// </summary>
        void GoBack();

        /// <summary>
        /// Выполнить переход вперед в истории навигации.
        /// </summary>
        void GoForward();

        /// <summary>
        /// Перезагрузить текущую страницу.
        /// </summary>
        void Reload();

        /// <summary>
        /// Событие, уведомляющее об изменении URL-адреса.
        /// </summary>
        event EventHandler<string> AddressChanged;
    }
}
