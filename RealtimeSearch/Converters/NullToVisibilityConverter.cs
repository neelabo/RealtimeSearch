using System;
using System.Windows;
using System.Windows.Data;

namespace NeeLaboratory.RealtimeSearch.Converters
{
    // NULLの場合、非表示にする
    [ValueConversion(typeof(object), typeof(Visibility))]
    internal class NullToVisibilityConverter : IValueConverter
    {
        public Visibility NotNull { get; set; } = Visibility.Visible;
        public Visibility Null { get; set; } = Visibility.Collapsed;


        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return value == null ? Null : NotNull;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
