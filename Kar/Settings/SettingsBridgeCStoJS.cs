using System;
using System.IO;
using System.Threading.Tasks;

namespace Kar.Settings
{
    /// <summary>
    /// Интерфейс сервиса управления настройками приложения.
    /// Абстрагирует логику чтения и записи конфигурационных данных.
    /// </summary>
    public interface ISettingsService
    {
        /// <summary>Загружает настройки из хранилища.</summary>
        /// <returns>Строка с данными настроек (обычно в формате JSON).</returns>
        string LoadSettings();

        /// <summary>Сохраняет настройки в хранилище.</summary>
        /// <param name="settings">Строка с данными настроек для сохранения.</param>
        void SaveSettings(string settings);
    }

    /// <summary>
    /// Реализация сервиса настроек, использующая локальную файловую систему для хранения данных.
    /// </summary>
    public class FileSettingsService : ISettingsService
    {
        private readonly string _filePath;

        /// <summary>
        /// Инициализирует новый экземпляр файлового сервиса настроек.
        /// </summary>
        /// <param name="filePath">Полный путь к файлу конфигурации.</param>
        public FileSettingsService(string filePath) => _filePath = filePath;

        /// <summary>
        /// Считывает содержимое файла настроек. Если файл не существует, возвращает пустой JSON-объект.
        /// </summary>
        public string LoadSettings() => File.Exists(_filePath) ? File.ReadAllText(_filePath) : "{}";

        /// <summary>
        /// Записывает строку настроек в указанный файл, перезаписывая его содержимое.
        /// </summary>
        public void SaveSettings(string settings) => File.WriteAllText(_filePath, settings);
    }

    /// <summary>
    /// Класс-мост для интеграции C# и JavaScript (CefSharp JS Binding).
    /// Обеспечивает доступ к чтению и записи настроек из пользовательского веб-интерфейса браузера.
    /// </summary>
    public class SettingsBridge
    {
        private readonly ISettingsService _settingsService;

        /// <summary>
        /// Событие, возникающее при успешном сохранении настроек.
        /// Позволяет другим компонентам приложения (например, UI) реагировать на изменение конфигурации.
        /// </summary>
        public event Action<string> OnSettingsSaved;

        /// <summary>
        /// Инициализирует новый экземпляр моста настроек с использованием внедрения зависимостей (DI).
        /// </summary>
        /// <param name="settingsService">Сервис для работы с хранилищем настроек.</param>
        public SettingsBridge(ISettingsService settingsService) => _settingsService = settingsService;

        /// <summary>
        /// Вызывается из JavaScript для получения текущих настроек.
        /// </summary>
        /// <returns>Строка с настройками.</returns>
        public string GetSettings() => _settingsService.LoadSettings();

        /// <summary>
        /// Вызывается из JavaScript для сохранения новых настроек.
        /// Перехватывает возможные исключения ввода-вывода.
        /// </summary>
        /// <param name="settings">Новые данные настроек (обычно JSON-строка).</param>
        /// <returns>Возвращает true, если сохранение прошло успешно, иначе false.</returns>
        public bool SaveSettings(string settings)
        {
            System.Diagnostics.Debug.WriteLine("C# успешно получил данные из JavaScript!", "Диагностика моста");
            try
            {
                _settingsService.SaveSettings(settings);
                OnSettingsSaved?.Invoke(settings);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при сохранении настроек: {ex.Message}");
                return false;
            }
        }
    }
}