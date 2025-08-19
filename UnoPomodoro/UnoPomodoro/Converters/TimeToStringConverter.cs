using Microsoft.UI.Xaml.Data;

namespace UnoPomodoro.Converters
{
    public class TimeToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is int seconds)
            {
                var mins = seconds / 60;
                var secs = seconds % 60;
                return $"{mins:D2}:{secs:D2}";
            }
            
            return "00:00";
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
