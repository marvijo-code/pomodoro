using Microsoft.UI.Xaml.Data;
using UnoPomodoro.Data.Models;

namespace UnoPomodoro.Converters;

public class TaskStatusToTextConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is TaskWorkStatus status)
        {
            return status switch
            {
                TaskWorkStatus.Todo => "TODO",
                TaskWorkStatus.InProgress => "InProgress",
                TaskWorkStatus.Done => "Done",
                _ => "TODO"
            };
        }

        if (value is int rawStatus)
        {
            return rawStatus switch
            {
                1 => "InProgress",
                2 => "Done",
                _ => "TODO"
            };
        }

        return "TODO";
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
