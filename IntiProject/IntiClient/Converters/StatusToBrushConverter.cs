using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media;

namespace IntiClient.Converters
{
    public class StatusToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string status)
            {
                return status switch
                {
                    "Активен" => new SolidColorBrush(Color.FromArgb(255, 29, 29, 29)),// #1D1D1D (RGB: 29, 29, 29)
                    "В очереди" => new SolidColorBrush(Color.FromArgb(255, 255, 204, 153)),// #FFEB3B (RGB: 255, 204, 153)
                    "Завершен" => new SolidColorBrush(Color.FromArgb(255, 76, 187, 23)),// #4CAF50 (RGB: 76, 187, 23)
                    _ => new SolidColorBrush(Color.FromArgb(255, 200, 200, 200)),// Цвет по умолчанию, если статус неизвестен
                };
            }
            return new SolidColorBrush(Color.FromArgb(255, 200, 200, 200)); // #C8C8C8
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
