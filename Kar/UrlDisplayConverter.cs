using System;
using System.Web;
using System.Windows.Data;
using System.Globalization;

namespace Kar
{
    public class UrlDisplayConverter : IValueConverter
    {
        private readonly string[] _searchKeys = { "q", "query", "text", "p" };
        public object Convert(object value, Type targetType, object param, CultureInfo culture)
        {
            string url = value as string ?? string.Empty;
            if (string.IsNullOrEmpty(url)) return "";

            if (url.Contains("home.html")) return "";
            try
            {
                if (url.Contains("?"))
                {
                    var uri = new Uri(url);

                    var query = HttpUtility.ParseQueryString(uri.Query);

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
        public object ConvertBack(object value, Type targetType, object param, CultureInfo culture) => value;
    }
}
