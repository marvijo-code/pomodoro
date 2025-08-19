using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI;

namespace UnoPomodoro.Converters
{
    public class ModeToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            var mode = value as string;
            var targetMode = parameter as string;
            
            if (mode == targetMode)
            {
                // Return red for pomodoro, blue for short break, green for long break
                return new SolidColorBrush(mode switch
                {
                    "pomodoro" => Colors.Red,
                    "shortBreak" => Colors.Blue,
                    "longBreak" => Colors.Green,
                    _ => Colors.Gray
                });
            }
            
            return new SolidColorBrush(Colors.Gray);
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
