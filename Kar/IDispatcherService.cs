using System;

namespace Kar
{
    /// <summary>
    /// Интерфейс для диспетчеризации выполнения кода в UI-потоке.
    /// Позволяет отделить зависимости WPF Dispatcher от бизнес-логики (ViewModel).
    /// </summary>
    public interface IDispatcherService
    {
        /// <summary>
        /// Выполняет переданное действие (Action) в UI-потоке синхронно.
        /// </summary>
        /// <param name="action">Выполняемый блок кода.</param>
        void Invoke(Action action);

        /// <summary>
        /// Проверяет, выполняется ли текущий код в UI-потоке.
        /// </summary>
        /// <returns>True, если текущий поток является потоком UI, иначе False.</returns>
        bool CheckAccess();
    }
}
