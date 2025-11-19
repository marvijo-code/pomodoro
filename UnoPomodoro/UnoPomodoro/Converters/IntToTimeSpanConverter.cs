using Microsoft.UI.Xaml.Data;
using System;

namespace UnoPomodoro.Converters
{
    public class IntToTimeSpanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is int seconds)
            {
                return TimeSpan.FromSeconds(seconds);
            }
            return TimeSpan.Zero;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            if (value is TimeSpan timeSpan)
            {
                return (int)timeSpan.TotalSeconds;
            }
            return 0;
        }
    }
}
