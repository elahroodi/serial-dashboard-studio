using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace SerialDebugPanel
{
    // این کانورتر چک می‌کند که اگر فیلدی در مدل null بود، پنل مربوط به آن پارامتر را در بخش RowDetails مخفی (Collapse) کند.
    public class NullToCollapsingConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return Visibility.Collapsed;

            if (value is int intVal && intVal == 0 && parameter?.ToString() == "HideIfZero")
                return Visibility.Collapsed;

            return Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
