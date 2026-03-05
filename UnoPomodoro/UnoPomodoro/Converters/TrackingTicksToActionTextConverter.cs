using Microsoft.UI.Xaml.Data;

namespace UnoPomodoro.Converters;

public class TrackingTicksToActionTextConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        return value == null ? "Track" : "Stop";
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
