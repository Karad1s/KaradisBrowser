using System.Windows.Input;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using System.IO;
using System.Collections.Generic;

namespace Kar.Shortcut
{
    public class ShortcutDto
    {
        public string Action { get; set; } = string.Empty;
        public string Gesture { get; set; } = string.Empty;
    }

    public class ShortcutLoader
    {
        public static List<ShortcutDto> LoadShortcut()
        {
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Shortcut", "shortcut.yaml");
            if (!File.Exists(path)) return new List<ShortcutDto>();

            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();

            return deserializer.Deserialize<List<ShortcutDto>>(File.ReadAllText(path)) ?? new List<ShortcutDto>();
        }
    }
}
