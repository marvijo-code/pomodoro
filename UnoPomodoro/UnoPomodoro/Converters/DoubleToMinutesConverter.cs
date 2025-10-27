using Microsoft.UI.Xaml.Data;
using System;

namespace UnoPomodoro.Converters;

public class DoubleToMinutesConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is double d)
        {
            return $"{d:F0}m";
        }
        return "0m";
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
