using Microsoft.UI.Xaml.Data;
using System;

namespace UnoPomodoro.Converters;

public class ProgressToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is double progress)
        {
            if (progress >= 90)
                return Microsoft.UI.ColorHelper.FromArgb(255, 76, 175, 80); // Green
            else if (progress >= 70)
                return Microsoft.UI.ColorHelper.FromArgb(255, 255, 193, 7); // Amber
            else if (progress >= 50)
                return Microsoft.UI.ColorHelper.FromArgb(255, 255, 152, 0); // Orange
            else
                return Microsoft.UI.ColorHelper.FromArgb(255, 244, 67, 54); // Red
        }

        return Microsoft.UI.ColorHelper.FromArgb(255, 158, 158, 158); // Gray
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}