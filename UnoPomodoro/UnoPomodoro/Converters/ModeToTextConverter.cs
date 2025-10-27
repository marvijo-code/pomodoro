using Microsoft.UI.Xaml.Data;
using System;

namespace UnoPomodoro.Converters;

public class ModeToTextConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is string mode)
        {
            return $"{mode} Mode";
        }
        return value?.ToString() ?? "";
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
