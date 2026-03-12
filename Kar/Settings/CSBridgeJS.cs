using System;
using System.IO;
using System.Threading.Tasks;

namespace Kar.Settings
{
    public interface ISettingsService
    {
        string LoadSettings();

        void SaveSettings(string settings);
    }

    public class FileSettingsService: ISettingsService 
    {
        private readonly string _filePath;

        public FileSettingsService(string filePath) => _filePath = filePath;

        public string LoadSettings() => File.Exists(_filePath) ? File.ReadAllText(_filePath) : "{}";
        public void SaveSettings(string settings) => File.WriteAllText(_filePath, settings);
    }

    public class SettingsBridge
    {
        private readonly ISettingsService _settingsService;

        public SettingsBridge(ISettingsService settingsService) => _settingsService = settingsService;

        public string GetSettings() => _settingsService.LoadSettings();
        public void SaveSettings(string settings) => _settingsService.SaveSettings(settings);
    }



}
