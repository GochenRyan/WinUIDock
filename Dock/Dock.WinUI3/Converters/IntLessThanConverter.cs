using Microsoft.UI.Xaml.Data;
using System;

namespace Dock.WinUI3.Converters
{
    public class IntLessThanConverter : IValueConverter
    {
        public int TrueIfLessThan { get; set; }

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is int intValue)
            {
                return intValue < TrueIfLessThan;
            }
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
