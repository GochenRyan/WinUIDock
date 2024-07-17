using Microsoft.UI.Xaml.Data;
using System;

namespace Dock.WinUI3.Converters
{
    public class ObjectToBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (parameter is bool boolParam && boolParam == true)
            {
                return value == null;
            }

            return value != null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
