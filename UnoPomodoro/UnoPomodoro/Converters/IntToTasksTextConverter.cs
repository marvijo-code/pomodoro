using Microsoft.UI.Xaml.Data;
using System;

namespace UnoPomodoro.Converters;

public class IntToTasksTextConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is int i)
        {
            return $"{i} tasks";
        }
        return "0 tasks";
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
