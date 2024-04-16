using NeeLaboratory.IO.Search.Files;
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace NeeLaboratory.RealtimeSearch.Converters
{
    public class FileInfoToIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is FileItem file)
            {
                return ShellFileResource.CreateIcon(file.Path, file.IsDirectory);
            }

            return DependencyProperty.UnsetValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
