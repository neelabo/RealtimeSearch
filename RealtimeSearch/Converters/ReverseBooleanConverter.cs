using System;
using System.Windows.Data;
using System.Globalization;

namespace NeeLaboratory.RealtimeSearch
{
    /// <summary>
    /// Reverse boolean
    /// </summary>
    public class ReverseBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolean)
            {
                return !boolean;
            }
            else
            {
                return value;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }


}
