using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace NeeLaboratory.RealtimeSearch.Converters
{
    public class BoolToVisibilityConverter : IValueConverter
    {
        public Visibility True { get; set; } = Visibility.Visible;
        public Visibility False { get; set; } = Visibility.Collapsed;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if ((bool)value)
            {
                return True;
            }
            else
            {
                return False;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
