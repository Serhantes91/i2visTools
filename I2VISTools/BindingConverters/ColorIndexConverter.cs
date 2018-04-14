using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows.Data;
using I2VISTools.Config;
using OxyPlot;

namespace I2VISTools.BindingConverters
{
    public class ColorIndexConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value.GetType() != typeof(OxyColor)) return "error";

            var color = (OxyColor)value;

            return GraphConfig.Instace.ColorMap.IndexOf(color);

        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
