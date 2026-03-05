using Microsoft.UI.Xaml.Data;
using UnoPomodoro.Data.Models;

namespace UnoPomodoro.Converters;

public class TaskTrackedTimeConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is not TaskItem task)
        {
            return "00:00";
        }

        var totalSeconds = Math.Max(0, task.TrackedSeconds);

        if (task.TrackingStartedAtUtcTicks.HasValue)
        {
            try
            {
                var startedUtc = new DateTime(task.TrackingStartedAtUtcTicks.Value, DateTimeKind.Utc);
                var elapsedSeconds = (int)Math.Max(0, (DateTime.UtcNow - startedUtc).TotalSeconds);
                totalSeconds += elapsedSeconds;
            }
            catch
            {
                // Ignore invalid persisted values.
            }
        }

        var hours = totalSeconds / 3600;
        var minutes = (totalSeconds % 3600) / 60;
        var seconds = totalSeconds % 60;

        return hours > 0
            ? $"{hours:D2}:{minutes:D2}:{seconds:D2}"
            : $"{minutes:D2}:{seconds:D2}";
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
