using Microsoft.UI.Xaml.Data;
using System;

namespace UnoPomodoro.Converters;

public class DateToRelativeStringConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is DateTime dateTime)
        {
            var now = DateTime.Now;
            var diff = now - dateTime;

            if (diff.TotalMinutes < 1)
                return "Just now";
            else if (diff.TotalMinutes < 60)
                return $"{(int)diff.TotalMinutes} min ago";
            else if (diff.TotalHours < 24)
                return $"{(int)diff.TotalHours} h ago";
            else if (diff.TotalDays < 7)
                return $"{(int)diff.TotalDays} days ago";
            else if (diff.TotalDays < 30)
                return $"{(int)(diff.TotalDays / 7)} weeks ago";
            else
                return dateTime.ToString("MMM dd");
        }

        return string.Empty;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}