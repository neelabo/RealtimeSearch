﻿using NeeLaboratory.IO.Search;
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace NeeLaboratory.RealtimeSearch
{
    public class FileInfoToTypeNameConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is FileInfo file)
            {
                return FileSystem.CreateTypeName(file.Path, file.IsDirectory);
            }

            return DependencyProperty.UnsetValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}