using Microsoft.UI.Xaml.Data;

namespace UnoPomodoro.Converters
{
    public class BoolToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is bool boolValue && parameter is string param)
            {
                var parts = param.Split('/');
                if (parts.Length == 2)
                {
                    return boolValue ? parts[0] : parts[1]; // Pause/Start, Hide History/Show History, etc.
                }
            }
            
            return "";
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
