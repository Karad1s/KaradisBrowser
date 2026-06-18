using CefSharp;
using System;
using System.Windows.Input;

namespace Kar.Handlers
{
    /// <summary>
    /// Кастомный обработчик клавиатуры для CefSharp.
    /// Перехватывает нажатия клавиш в браузере и перенаправляет горячие клавиши (shortcuts) в главное окно WPF.
    /// </summary>
    public class CustomKeyboardHandler : IKeyboardHandler
    {
        private readonly MainWindow _mainWindow;

        /// <summary>
        /// Инициализирует новый экземпляр обработчика клавиатуры.
        /// </summary>
        /// <param name="mainWindow">Ссылка на главное окно приложения для доступа к его диспетчеру и привязкам ввода (InputBindings).</param>
        public CustomKeyboardHandler(MainWindow mainWindow)
        {
            _mainWindow = mainWindow;
        }

        /// <summary>
        /// Вызывается перед тем, как событие клавиатуры будет обработано браузером.
        /// Используется для перехвата глобальных горячих клавиш WPF, когда фокус находится внутри веб-обозревателя.
        /// </summary>
        /// <param name="chromiumWebBrowser">Текущий экземпляр элемента управления браузером.</param>
        /// <param name="browser">Объект браузера CefSharp.</param>
        /// <param name="type">Тип события клавиатуры (нажатие, отпускание и т.д.).</param>
        /// <param name="windowsKeyCode">Код клавиши Windows.</param>
        /// <param name="nativeKeyCode">Нативный код клавиши.</param>
        /// <param name="modifiers">Модификаторы клавиатуры (Ctrl, Alt, Shift).</param>
        /// <param name="isSystemKey">Указывает, является ли клавиша системной.</param>
        /// <param name="isKeyboardShortcut">Ссылка на флаг горячей клавиши.</param>
        /// <returns>Возвращает false, чтобы позволить браузеру продолжить стандартную обработку события.</returns>
        public bool OnPreKeyEvent(IWebBrowser chromiumWebBrowser, IBrowser browser, KeyType type, int windowsKeyCode, int nativeKeyCode, CefEventFlags modifiers, bool isSystemKey, ref bool isKeyboardShortcut)
        {
            // Обрабатываем только первичное нажатие клавиши
            if (type == KeyType.RawKeyDown)
            {
                ModifierKeys wpfModifierKeys = ModifierKeys.None;

                // Конвертация модификаторов CefSharp в модификаторы WPF
                if (modifiers.HasFlag(CefEventFlags.ControlDown)) wpfModifierKeys |= ModifierKeys.Control;
                if (modifiers.HasFlag(CefEventFlags.AltDown)) wpfModifierKeys |= ModifierKeys.Alt;
                if (modifiers.HasFlag(CefEventFlags.ShiftDown)) wpfModifierKeys |= ModifierKeys.Shift;

                // Конвертация виртуального кода клавиши Windows в перечисление Key WPF
                Key wpfKey = KeyInterop.KeyFromVirtualKey(windowsKeyCode);

                System.Diagnostics.Debug.WriteLine($"[CEF Key] Нажата: {wpfKey}, Модификаторы: {wpfModifierKeys}");

                // Переход в главный UI-поток для взаимодействия с элементами окна
                _mainWindow.Dispatcher.Invoke(() =>
                {
                    // Поиск совпадений среди глобальных горячих клавиш главного окна
                    foreach (InputBinding binding in _mainWindow.InputBindings)
                    {
                        if (binding is KeyBinding keyBinding && keyBinding.Gesture is KeyGesture gesture)
                        {
                            if (gesture.Key == wpfKey && gesture.Modifiers == wpfModifierKeys)
                            {
                                System.Diagnostics.Debug.WriteLine($"[CEF Key] Найдено совпадение для {gesture.Key}!");

                                // Проверка возможности выполнения и вызов привязанной команды
                                if (keyBinding.Command != null && keyBinding.Command.CanExecute(null))
                                {
                                    System.Diagnostics.Debug.WriteLine($"[CEF Key] Выполнение команды...");
                                    keyBinding.Command.Execute(null);
                                }
                                else
                                {
                                    System.Diagnostics.Debug.WriteLine($"[CEF Key] ОШИБКА: Command равен null или CanExecute=false");
                                }
                            }
                        }
                    }
                });
            }

            // Возвращаем false, чтобы не блокировать ввод внутри самой веб-страницы
            return false;
        }

        /// <summary>
        /// Вызывается после того, как событие клавиатуры было обработано браузером (не используется).
        /// </summary>
        public bool OnKeyEvent(IWebBrowser browserControl, IBrowser browser, KeyType type, int windowsKeyCode, int nativeKeyCode, CefEventFlags modifiers, bool isSystemKey)
        {
            return false;
        }
    }
}