using System;
using System.Web;
using System.Windows.Data;
using System.Globalization;

namespace Kar
{
    /// <summary>
    /// Конвертер значения для форматирования URL-адреса в более читаемый вид.
    /// Если URL представляет собой запрос к поисковой системе (например, Google с параметром ?q=),
    /// конвертер извлекает и возвращает сам поисковый запрос для отображения в строке адреса.
    /// </summary>
    public class UrlDisplayConverter : IValueConverter
    {
        // Ключи параметров запроса, используемые популярными поисковиками для хранения текста поиска
        private readonly string[] _searchKeys = { "q", "query", "text", "p" };

        /// <summary>
        /// Преобразует URL в строку для вывода пользователю. Извлекает поисковые запросы из URL.
        /// </summary>
        public object Convert(object value, Type targetType, object param, CultureInfo culture)
        {
            string url = value as string ?? string.Empty;
            if (string.IsNullOrEmpty(url)) return "";

            // Домашняя страница отображается как пустая строка
            if (url.Contains("home.html")) return "";
            try
            {
                if (url.Contains("?"))
                {
                    var uri = new Uri(url);
                    var query = HttpUtility.ParseQueryString(uri.Query);

                    // Проверяем наличие ключей поисковых запросов в параметрах URL
                    foreach (var key in _searchKeys)
                    {
                        var searchValue = query.Get(key);
                        if (!string.IsNullOrEmpty(searchValue)) return searchValue;
                    }
                }
            }
            catch { }
            return url;
        }

        /// <summary>
        /// Обратное преобразование возвращает переданное значение без изменений.
        /// </summary>
        public object ConvertBack(object value, Type targetType, object param, CultureInfo culture) => value;
    }
}
