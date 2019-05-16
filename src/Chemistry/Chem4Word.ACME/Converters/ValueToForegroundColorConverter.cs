using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media;

namespace Chem4Word.ACME.Converters
{
    class ValueToForegroundColorConverter : IValueConverter
    {
        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            SolidColorBrush brush = new SolidColorBrush(Colors.Red);

            Double doubleValue = 0.0;
            if (value == null)
            {
                brush = new SolidColorBrush(Colors.Black);
                return brush;
            }
            Double.TryParse(value.ToString(), out doubleValue);

            if (doubleValue < 0)
                brush = new SolidColorBrush(Colors.Blue);
            else if (doubleValue == 0.0)
            {
                brush=new SolidColorBrush(Colors.Black);
            }

            return brush;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
