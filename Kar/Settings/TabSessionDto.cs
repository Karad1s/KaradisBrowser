using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using System.IO;
using System.Collections.Generic;
using System.Linq;

namespace Kar.Settings
{
    /// <summary>
    /// Объект передачи данных (DTO), представляющий сохраненное состояние отдельной вкладки браузера.
    /// Используется исключительно для сериализации данных сессии в формат YAML.
    /// </summary>
    public class TabSessionDto
    {
        /// <summary>Заголовок последней открытой веб-страницы.</summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>Текущий URL-адрес вкладки.</summary>
        public string Url { get; set; } = string.Empty;

        /// <summary>Полная история навигации (переходов назад/вперед) для данной вкладки.</summary>
        public List<string> NavigationHistory { get; set; } = new List<string>();

        /// <summary>Индекс текущей страницы в списке истории навигации вкладки.</summary>
        public int CurrentHistoryIndex { get; set; } = 0;
    }

    /// <summary>
    /// Менеджер сессий, отвечающий за сохранение состояния открытых вкладок 
    /// при закрытии браузера и их восстановление при следующем запуске.
    /// </summary>
    public class SessionManager
    {
        private readonly string _sessionFilePath;

        /// <summary>
        /// Инициализирует новый экземпляр менеджера сессий, определяя абсолютный путь к файлу session.yaml.
        /// </summary>
        public SessionManager()
        {
            _sessionFilePath = Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "Settings", "session.yaml");
        }

        /// <summary>
        /// Извлекает данные из активных вкладок и сохраняет их в конфигурационный файл YAML.
        /// </summary>
        /// <param name="tabs">Коллекция моделей представления текущих открытых вкладок.</param>
        /// <param name="isIncognitoWindow">Флаг, указывающий, является ли текущее окно приватным.</param>
        public void SaveSession(IEnumerable<TabViewModel> tabs, bool isIncognitoWindow)
        {
            // Политика конфиденциальности: полностью игнорируем сохранение сессии для инкогнито окон
            if (isIncognitoWindow) return;

            // Фильтруем отдельные инкогнито-вкладки (если применимо) и маппим данные в DTO
            var dtos = tabs.Where(t => !t.isIncognito).Select(t => new TabSessionDto
            {
                Title = string.IsNullOrEmpty(t.Title) ? "Empty Title" : t.Title,
                Url = string.IsNullOrEmpty(t.Url) ? "Empty URL" : t.Url,
                NavigationHistory = t.NavigationHistory.ToList(),
                CurrentHistoryIndex = t.CurrentHistoryIndex
            }).ToList();

            System.Diagnostics.Debug.WriteLine($"Отладка SessionManager: Вкладок подготовлено: {dtos.Count}", "Session Debug");

            // Настройка сериализатора YAML с конвертацией имен свойств в формат camelCase
            var serializer = new SerializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();
            var yaml = serializer.Serialize(dtos);

            // Гарантируем, что директория Settings существует перед записью файла
            Directory.CreateDirectory(Path.GetDirectoryName(_sessionFilePath) ?? string.Empty);
            File.WriteAllText(_sessionFilePath, yaml);
        }

        /// <summary>
        /// Считывает файл сессии YAML и десериализует его в список DTO-объектов.
        /// </summary>
        /// <returns>Список сохраненных вкладок. Если файл отсутствует или пуст, возвращает пустой список.</returns>
        public List<TabSessionDto> LoadSession()
        {
            // Безопасный выход, если файл сессии еще не был создан (например, при первом запуске)
            if (!File.Exists(_sessionFilePath)) return new List<TabSessionDto>();

            var yaml = File.ReadAllText(_sessionFilePath);

            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();

            var dtos = deserializer.Deserialize<List<TabSessionDto>>(yaml);

            // Оператор null-coalescing гарантирует, что мы не вернем null, если структура файла повреждена
            return dtos ?? new List<TabSessionDto>();
        }
    }
}