using Dock.Model.Core;
using Microsoft.UI.Xaml.Data;
using System;

namespace Dock.WinUI3.Converters
{
    public class NormalConverter : IValueConverter
    {

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is IDock)
            {
                return (IDock)value;
            }
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
