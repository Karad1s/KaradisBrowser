using System;
using System.Windows.Threading;

namespace Kar
{
    /// <summary>
    /// Реализация службы диспетчеризации для WPF, использующая класс Dispatcher.
    /// Гарантирует выполнение операций с UI элементами и свойствами в основном потоке интерфейса.
    /// </summary>
    public class WpfDispatcherService : IDispatcherService
    {
        private readonly Dispatcher _dispatcher;

        /// <summary>
        /// Конструктор класса. Принимает WPF-диспетчер.
        /// </summary>
        /// <param name="dispatcher">Объект Dispatcher текущего окна или приложения.</param>
        public WpfDispatcherService(Dispatcher dispatcher)
        {
            _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
        }

        /// <summary>
        /// Выполняет действие в UI-потоке. Если текущий поток уже является UI-потоком,
        /// действие выполняется немедленно, иначе вызывается Dispatcher.Invoke.
        /// </summary>
        /// <param name="action">Действие для выполнения.</param>
        public void Invoke(Action action)
        {
            if (_dispatcher.CheckAccess())
            {
                action();
            }
            else
            {
                _dispatcher.Invoke(action);
            }
        }

        /// <summary>
        /// Проверяет, имеет ли вызывающий поток доступ к диспетчеру (находится ли в UI потоке).
        /// </summary>
        /// <returns>True, если доступ есть, иначе False.</returns>
        public bool CheckAccess() => _dispatcher.CheckAccess();
    }
}
