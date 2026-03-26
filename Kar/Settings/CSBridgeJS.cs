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

        public event Action<string> OnSettingsSaved;

        public SettingsBridge(ISettingsService settingsService) => _settingsService = settingsService;

        public string GetSettings() => _settingsService.LoadSettings();
        public bool SaveSettings(string settings)
        {
            System.Windows.MessageBox.Show("C# успешно получил данные из JavaScript!", "Диагностика моста");
            try
            {
                _settingsService.SaveSettings(settings);
                OnSettingsSaved?.Invoke(settings);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving settings: {ex.Message}");
                return false;
            }
        }
    }



}
