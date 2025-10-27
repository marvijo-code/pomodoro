using Microsoft.UI.Xaml.Data;
using System;

namespace UnoPomodoro.Converters;

public class IntToMinutesTextConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is int i)
        {
            return $"{i} minutes";
        }
        return "0 minutes";
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
