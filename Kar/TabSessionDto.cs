using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using System.IO;
using System.Collections.Generic;
using System.Linq;

namespace Kar
{
    public class TabSessionDto
    {

        public string Title { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;

        public List<string> NavigationHistory { get; set; } = new List<string>();
        public int CurrentHistoryIndex { get; set; } = 0;

    }

    public class SessionManager
    {
        private readonly string _sessionFilePath;

        public SessionManager()
        {
            _sessionFilePath = Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "Settings", "session.yaml");
        }
        public void SaveSession(IEnumerable<TabViewModel> tabs, bool isIncognitoWindow)
        {
            if(isIncognitoWindow) return; // Не сохраняем сессию для инкогнито окна
            var dtos = tabs.Where(t => !t.isIncognito).Select(t => new TabSessionDto
            {
                Title = string.IsNullOrEmpty(t.Title) ? "Empty Title" : t.Title,
                Url = string.IsNullOrEmpty(t.Url) ? "Empty URL" : t.Url,
                NavigationHistory = t.NavigationHistory.ToList(),
                CurrentHistoryIndex = t.CurrentHistoryIndex
            }).ToList();

            System.Windows.MessageBox.Show($"Отладка SessionManager: Вкладок подготовлено: {dtos.Count}", "Session Debug");

            var serializer = new SerializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();
            var yaml = serializer.Serialize(dtos);

            Directory.CreateDirectory(Path.GetDirectoryName(_sessionFilePath) ?? string.Empty);
            File.WriteAllText(_sessionFilePath, yaml);
        }

        public List<TabSessionDto> LoadSession()
        {
            if(!File.Exists(_sessionFilePath)) return new List<TabSessionDto>();
            var yaml = File.ReadAllText(_sessionFilePath);
            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();
            var dtos = deserializer.Deserialize<List<TabSessionDto>>(yaml);
            return dtos ?? new List<TabSessionDto>();
        }
    }
    
}
