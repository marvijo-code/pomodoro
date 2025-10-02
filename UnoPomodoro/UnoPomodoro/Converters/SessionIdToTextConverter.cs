using Microsoft.UI.Xaml.Data;

namespace UnoPomodoro.Converters
{
    public class SessionIdToTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is string sessionId)
            {
                return string.IsNullOrEmpty(sessionId) ? "Start Session" : "End Session";
            }
            
            return "Start Session";
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
