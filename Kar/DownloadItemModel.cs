using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Kar
{
    /// <summary>
    /// Модель представления для элемента загрузки, используемая в списке недавних загрузок.
    /// Реализует интерфейс INotifyPropertyChanged для автообновления UI WPF.
    /// </summary>
    public class DownloadItemModel : INotifyPropertyChanged
    {
        /// <summary>
        /// Уникальный идентификатор операции загрузки.
        /// </summary>
        public int Id { get; set; }

        private string _fileName = string.Empty;
        private int _percent;
        private string _progressText = string.Empty;

        /// <summary>
        /// Имя загружаемого файла.
        /// </summary>
        public string FileName
        {
            get => _fileName;
            set { _fileName = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Процент выполнения загрузки (от 0 до 100).
        /// </summary>
        public int Percent
        {
            get => _percent;
            set { _percent = value; OnPropertyChanged(); } 
        }

        /// <summary>
        /// Текстовое описание прогресса загрузки (например, размер скачанных данных или статус "Завершено").
        /// </summary>
        public string ProgressText
        {
            get => _progressText;
            set { _progressText = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Обновляет свойства модели на основе текущего состояния объекта загрузки CefSharp.
        /// </summary>
        /// <param name="item">Объект загрузки от CefSharp.</param>
        public void Update(CefSharp.DownloadItem item)
        {
            FileName = item.SuggestedFileName;
            Percent = item.PercentComplete > 0 ? item.PercentComplete : 0;

            // Вычисляем мегабайты
            double receivedMb = item.ReceivedBytes / 1048576.0;
            double totalMb = item.TotalBytes / 1048576.0;

            if (item.IsComplete)
                ProgressText = "Загрузка завершена";
            else if (item.TotalBytes > 0) 
                ProgressText = $"{receivedMb:F1} MB из {totalMb:F1} MB ({Percent}%)";
            else
                ProgressText = $"{receivedMb:F1} MB";
        }

        /// <summary>
        /// Событие для оповещения UI об изменении свойств.
        /// </summary>
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// Вызывает событие PropertyChanged для указанного свойства.
        /// </summary>
        /// <param name="Name">Имя изменившегося свойства (определяется автоматически с помощью CallerMemberName).</param>
        protected void OnPropertyChanged([CallerMemberName] string? Name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(Name));
        }
    } 
}