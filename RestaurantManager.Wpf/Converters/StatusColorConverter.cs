using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using RestaurantManager.Wpf.Models;

namespace RestaurantManager.Wpf.Converters
{
    public class StatusColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is TableStatus status)
            {
                return status switch
                {
                    TableStatus.Free => Brushes.LightGreen,
                    TableStatus.Occupied => Brushes.Salmon,
                    TableStatus.WaitingPayment => Brushes.Gold,
                    _ => Brushes.Gray
                };
            }
            return Brushes.White;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
