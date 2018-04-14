using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows.Data;

namespace I2VISTools.BindingConverters
{
    public class UpCatalogConverer : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {

            if (value == null) return "";
            var valueType = value.GetType();
            var dtType = new DateTime().GetType();

            if (valueType == dtType)
            {
                var curDt = (DateTime) value;
                if (curDt == new DateTime() || curDt == DateTime.MaxValue )
                {
                    return "";
                }
                else
                {
                    return curDt.ToString("g");
                }
            }

            if (valueType == typeof(uint))
            {
                var curSize = (uint) value;

                if (curSize == 0 || curSize == uint.MaxValue ) return "";

                if (curSize < 1024) return curSize+ " Б";
                if (curSize >= 1024 && curSize < 1024*1024) return (curSize/1024d).ToString("0.###") + " КБ";
                if (curSize >= 1024 * 1024 && curSize < 1024 * 1024 * 1024) return (curSize / Math.Pow(1024, 2)).ToString("0.###") + " МБ";
                if (curSize >= 1024 * 1024 * 1024) return (curSize / Math.Pow(1024, 3)).ToString("0.###") + " ГБ";

            }
            if (valueType == typeof (string))
            {
                var curName = value.ToString();

                if (curName == "   " || curName == "zzzzz") return "...";
                if (curName == "zz?") return "";
                if (curName == "'folder") return "Папка";

                return curName;

            }

            return "";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
