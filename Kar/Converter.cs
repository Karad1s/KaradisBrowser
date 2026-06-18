using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Windows.Data;

namespace Kar
{
    /// <summary>
    /// Мультиконвертер для определения, является ли данная вкладка выбранной (активной).
    /// Сравнивает текущую вкладку с выбранной вкладкой в списке.
    /// </summary>
    public class TabSelectionConverter : IMultiValueConverter
    {
        /// <summary>
        /// Сравнивает два переданных объекта (обычно это проверяемая вкладка и активная вкладка).
        /// </summary>
        /// <returns>True, если вкладки совпадают (вкладка активна), иначе False.</returns>
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length < 2) return false;
            return values[0] == values[1];
        }

        /// <summary>
        /// Обратное преобразование не поддерживается и вызывает исключение.
        /// </summary>
        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
