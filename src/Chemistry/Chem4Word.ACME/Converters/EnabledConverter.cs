using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace Chem4Word.ACME.Converters
{
    public class EnabledConverter :IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value != null && value is bool)
            {
                if ((bool) value)
                {
                    return (Brush) parameter;
                }
                else
                {
                    return new SolidColorBrush(Colors.DarkGray);
                }

            }
            return new SolidColorBrush(Colors.DarkGray);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
