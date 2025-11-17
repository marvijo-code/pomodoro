using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace UnoPomodoro.Converters
{
    public class ModeToColorConverter : IValueConverter
    {
        private static readonly SolidColorBrush InactiveBrush = new(Colors.Gray);

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is not string mode)
            {
                return InactiveBrush;
            }

            var accentBrush = ResolveModeBrush(mode);

            if (parameter is string targetMode && !string.IsNullOrEmpty(targetMode))
            {
                return mode == targetMode ? accentBrush : InactiveBrush;
            }

            return accentBrush;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
        private static Brush ResolveModeBrush(string mode)
        {
            return mode switch
            {
                "pomodoro" => GetBrushFromResource("PomodoroColor", Colors.Red),
                "shortBreak" => GetBrushFromResource("ShortBreakColor", Colors.Blue),
                "longBreak" => GetBrushFromResource("LongBreakColor", Colors.Green),
                _ => InactiveBrush
            };
        }

        private static Brush GetBrushFromResource(string resourceKey, Color fallback)
        {
            if (Application.Current?.Resources.TryGetValue(resourceKey, out var resource) == true)
            {
                return resource switch
                {
                    Brush brush => brush,
                    Color color => new SolidColorBrush(color),
                    _ => new SolidColorBrush(fallback)
                };
            }

            return new SolidColorBrush(fallback);
        }
    }
}
