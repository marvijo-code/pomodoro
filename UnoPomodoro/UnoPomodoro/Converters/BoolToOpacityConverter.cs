using Microsoft.UI.Xaml.Data;
using System;

namespace UnoPomodoro.Converters;

public class BoolToOpacityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is bool isCompleted && isCompleted)
        {
            return 0.6; // Dimmed for completed tasks
        }
        return 1.0; // Full opacity for active tasks
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
