using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dock.WinUI3.Converters
{
    public class OrientationConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return value switch
            {
                null => Orientation.Vertical,
                Model.Core.Orientation orientation => orientation switch
                { 
                    Model.Core.Orientation.Horizontal => Orientation.Horizontal,
                    Model.Core.Orientation.Vertical => Orientation.Vertical,
                    _ => throw new NotSupportedException($"Provided orientation is not supported in WinUI3.")
                },
                _ => value
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return value switch
            {
                null => Model.Core.Orientation.Horizontal,
                Orientation orientation => orientation switch
                {
                    Orientation.Horizontal => Model.Core.Orientation.Horizontal,
                    Orientation.Vertical => Model.Core.Orientation.Vertical,
                    _ => throw new NotSupportedException($"Provided orientation is not supported in Model.")
                },
                _ => value
            };
        }
    }
}
