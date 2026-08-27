using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Data.Sqlite;
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
        private readonly Dictionary<string, (string ProcessName, string DataPath)> _supportedBrowsers;

        /// <summary>
        /// Событие, возникающее при успешном сохранении настроек.
        /// Позволяет другим компонентам приложения (например, UI) реагировать на изменение конфигурации.
        /// </summary>
        public event Action<string> OnSettingsSaved;

        /// <summary>
        /// Инициализирует новый экземпляр моста настроек с использованием внедрения зависимостей (DI).
        /// </summary>
        /// <param name="settingsService">Сервис для работы с хранилищем настроек.</param>
        public SettingsBridge(ISettingsService settingsService)
        {
            _settingsService = settingsService;

            string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string roamingAppData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

            _supportedBrowsers = new Dictionary<string, (string, string)>
                {
                    { "Google Chrome", ("chrome", Path.Combine(localAppData, @"Google\Chrome\User Data")) },
                    { "Mozilla Firefox", ("firefox", Path.Combine(roamingAppData, @"Mozilla\Firefox\Profiles")) },
                    { "Microsoft Edge", ("msedge", Path.Combine(localAppData, @"Microsoft\Edge\User Data")) }
            };

        }
           
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
            Debug.WriteLine("C# успешно получил данные из JavaScript!", "Диагностика моста");
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


        public string GetInstalledBrowsers()
        {
            var installed = new List<string>();
            foreach (var browser in _supportedBrowsers)
            {
                if (Directory.Exists(browser.Value.DataPath))
                {
                    installed.Add(browser.Key);
                }
            }
            return JsonSerializer.Serialize(installed);
        }

        public bool IsBrowserRunning(string browserName)
        {
            if (!_supportedBrowsers.ContainsKey(browserName)) return false;

            string processName = _supportedBrowsers[browserName].ProcessName;
            Process[] processes = Process.GetProcessesByName(processName);
            return processes.Length > 0;
        }
        public string ImportData(string browserName, string optionsJson)
        {
            if (!_supportedBrowsers.ContainsKey(browserName)) return "Ошибка: Браузер не найден.";

            var options = JsonSerializer.Deserialize<ImportOptions>(optionsJson);
            string dataPath = _supportedBrowsers[browserName].DataPath;

            //Структура вызовов парсера
            try
            {
                if (options.History) ImportHistory(dataPath, browserName);
                if (options.Bookmarks) ImportBookmarks(dataPath, browserName);
                if (options.Cookies) ImportCookies(dataPath, browserName);
                if (options.Passwords) ImportPasswords(dataPath, browserName);

                return "Импорт данных завершен успешно.";
            }
            catch (Exception ex)
            {
                return $"Ошибка при импорте данных: {ex.Message}";
            }
        }

        private void ImportHistory(string dataPath, string browserName)
        {
            // Реализовать логику импорта истории браузера
            Debug.WriteLine($"Импорт истории из {browserName} по пути {dataPath}");
        }

        private void ImportBookmarks(string dataPath, string browserName)
        {
            // Реализовать логику импорта закладок браузера
            Debug.WriteLine($"Импорт закладок из {browserName} по пути {dataPath}");
        }

        private void ImportCookies(string dataPath, string browserName)
        {
            // Реализовать логику импорта cookies браузера
            Debug.WriteLine($"Импорт cookies из {browserName} по пути {dataPath}");
        }
        private void ImportPasswords(string dataPath, string browserName)
        {
            // Реализовать логику импорта паролей браузера
            Debug.WriteLine($"Импорт паролей из {browserName} по пути {dataPath}");
        }

        private class ImportOptions
        {
            public bool History { get; set; }
            public bool Bookmarks { get; set; }
            public bool Cookies { get; set; }
            public bool Passwords { get; set; }
        }
    }
}