using CefSharp;
using System;
using System.Windows.Input;


namespace Kar.Handlers
{
    public class CustomKeyboardHandler : IKeyboardHandler
    {
        private readonly MainWindow _mainWindow;

        public CustomKeyboardHandler(MainWindow mainWindow)
        {
            _mainWindow = mainWindow;
        }
    

        public bool OnPreKeyEvent(IWebBrowser chromiumWebBrowser, IBrowser browser, KeyType type, int windowsKeyCode, int nativeKeyCode, CefEventFlags modifiers, bool isSystemKey, ref bool isKeyboardShortcut)
        {
            if (type == KeyType.RawKeyDown)
            {
                ModifierKeys wpfModifierKeys = ModifierKeys.None;

                if(modifiers.HasFlag(CefEventFlags.ControlDown)) wpfModifierKeys |= ModifierKeys.Control;
                if(modifiers.HasFlag(CefEventFlags.AltDown)) wpfModifierKeys |= ModifierKeys.Alt;
                if(modifiers.HasFlag(CefEventFlags.ShiftDown)) wpfModifierKeys |= ModifierKeys.Shift;

                Key wpfKey = KeyInterop.KeyFromVirtualKey(windowsKeyCode);

                System.Diagnostics.Debug.WriteLine($"[CEF Key] Нажата: {wpfKey}, Модификаторы: {wpfModifierKeys}");

                _mainWindow.Dispatcher.Invoke(() =>
                {
                    
                    foreach (InputBinding binding in _mainWindow.InputBindings)
                    {
                        if (binding is KeyBinding keyBinding && keyBinding.Gesture is KeyGesture gesture)
                        {
                            if (gesture.Key == wpfKey && gesture.Modifiers == wpfModifierKeys)
                            {
                                System.Diagnostics.Debug.WriteLine($"[CEF Key] Найдено совпадение для {gesture.Key}!");

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
            return false    ;
        }

        public bool OnKeyEvent(IWebBrowser browserControl, IBrowser browser, KeyType type, int windowsKeyCode, int nativeKeyCode, CefEventFlags modifiers, bool isSystemKey)
        {
            return false;
        }

    }
}
