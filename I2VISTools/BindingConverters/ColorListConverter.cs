using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows.Data;
using System.Windows.Media;
using OxyPlot;

namespace I2VISTools.BindingConverters
{
    public class ColorListConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value.GetType() != typeof (OxyColor)) return null;

            var color = (OxyColor)value;

            return new Color() {A = color.A, R = color.R, G = color.G, B = color.B};

        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value.GetType() != typeof (Color)) return OxyColors.Black;

            var color = (Color) value;

            return OxyColor.FromRgb(color.R, color.G, color.B);

        }
    }
}
