using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Kar
{
    public class DownloadItemModel : INotifyPropertyChanged
    {
        public int Id { get; set; }
        private string _fileName;
        private int _percent;
        private string _progressText;

        public string FileName
        {
            get => _fileName;
            set { _fileName = value; OnPropertyChanged(); }
        }

        public int Percent
        {
            get => _percent;
            set { _percent = value; OnPropertyChanged(); } 
        }

        public string ProgressText
        {
            get => _progressText;
            set { _progressText = value; OnPropertyChanged(); }
        }

        public void Update(CefSharp.DownloadItem item)
        {
            FileName = item.SuggestedFileName;
            Percent = item.PercentComplete > 0 ? item.PercentComplete:0 ;

            double receivedMb = item.ReceivedBytes / 1048576.0;
            double totalMb = item.TotalBytes / 1048576.0;

            if (item.IsComplete)
                ProgressText = "Загрузка завершена";
            else if (item.TotalBytes > 0) 
                ProgressText = $"{receivedMb:F1} MB из {totalMb:F1} MB ({Percent}%)";
            else
                ProgressText = $"{receivedMb:F1} MB";
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? Name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(Name));
        }
    } 
}