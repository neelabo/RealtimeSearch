using NeeLaboratory.IO.Search.Files;
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace NeeLaboratory.RealtimeSearch.Converters
{
    public class FileItemStateToVisibilityConverter : IValueConverter
    {
        public bool Inverse { get; set; }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var positive = value is FileItemState state && state == FileItemState.Stable;
            return (positive ^ Inverse) ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

}
