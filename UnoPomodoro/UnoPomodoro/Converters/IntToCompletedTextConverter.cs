using Microsoft.UI.Xaml.Data;
using System;

namespace UnoPomodoro.Converters;

public class IntToCompletedTextConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is int i)
        {
            return $"{i} completed";
        }
        return "0 completed";
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
