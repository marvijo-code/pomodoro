using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using System;
using Windows.Foundation;

namespace UnoPomodoro.Controls
{
    public sealed partial class AnalogClock : UserControl
    {
        public static readonly DependencyProperty TimeLeftProperty =
            DependencyProperty.Register(nameof(TimeLeft), typeof(TimeSpan), typeof(AnalogClock),
                new PropertyMetadata(TimeSpan.Zero, OnTimeLeftChanged));

        public static readonly DependencyProperty TotalTimeProperty =
            DependencyProperty.Register(nameof(TotalTime), typeof(TimeSpan), typeof(AnalogClock),
                new PropertyMetadata(TimeSpan.FromMinutes(25)));

        public static readonly DependencyProperty ClockSizeProperty =
            DependencyProperty.Register(nameof(ClockSize), typeof(double), typeof(AnalogClock),
                new PropertyMetadata(200.0, OnClockSizeChanged));

        public static readonly DependencyProperty ProgressBrushProperty =
            DependencyProperty.Register(nameof(ProgressBrush), typeof(Brush), typeof(AnalogClock),
                new PropertyMetadata(null, OnVisualPropertyChanged));

        public TimeSpan TimeLeft
        {
            get => (TimeSpan)GetValue(TimeLeftProperty);
            set => SetValue(TimeLeftProperty, value);
        }

        public TimeSpan TotalTime
        {
            get => (TimeSpan)GetValue(TotalTimeProperty);
            set => SetValue(TotalTimeProperty, value);
        }

        public double ClockSize
        {
            get => (double)GetValue(ClockSizeProperty);
            set => SetValue(ClockSizeProperty, value);
        }

        public Brush ProgressBrush
        {
            get => (Brush)GetValue(ProgressBrushProperty);
            set => SetValue(ProgressBrushProperty, value);
        }

        public AnalogClock()
        {
            this.InitializeComponent();
            ProgressBrush = GetDefaultBrush();
            this.Loaded += AnalogClock_Loaded;
        }

        private void AnalogClock_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateClockLayout();
        }

        private static void OnTimeLeftChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is AnalogClock clock)
            {
                clock.UpdateClock();
            }
        }

        private static void OnClockSizeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is AnalogClock clock)
            {
                clock.UpdateClockLayout();
            }
        }

        private static void OnVisualPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is AnalogClock clock)
            {
                clock.UpdateClock();
            }
        }

        private void UpdateClockLayout()
        {
            double size = Math.Max(ClockSize, 60);

            if (ClockFace != null)
            {
                ClockFace.Width = size;
                ClockFace.Height = size;
            }

            UpdateClock();
        }

        private void UpdateClock()
        {
            if (TotalTime <= TimeSpan.Zero)
            {
                return;
            }

            var totalSeconds = Math.Max(1d, TotalTime.TotalSeconds);
            var elapsedSeconds = Math.Clamp((TotalTime - TimeLeft).TotalSeconds, 0d, totalSeconds);
            var progress = elapsedSeconds / totalSeconds;

            UpdateSweep(progress);
        }

        private void UpdateSweep(double progress)
        {
            if (ClockFace == null || SweepHand == null || ProgressFill == null)
            {
                return;
            }

            double size = Math.Max(ClockSize, 60);
            double center = size / 2;
            double radius = center - 8;

            double angle = progress * 360d;
            double radians = (angle - 90d) * Math.PI / 180d;
            double handX = center + radius * Math.Cos(radians);
            double handY = center + radius * Math.Sin(radians);

            SweepHand.X1 = center;
            SweepHand.Y1 = center;
            SweepHand.X2 = handX;
            SweepHand.Y2 = handY;

            if (progress <= 0)
            {
                ProgressFill.Data = null;
                return;
            }

            if (progress >= 1)
            {
                ProgressFill.Data = new EllipseGeometry
                {
                    Center = new Point(center, center),
                    RadiusX = radius,
                    RadiusY = radius
                };
                return;
            }

            var startPoint = new Point(center, center - radius);
            double sweepRadians = angle * Math.PI / 180d;
            var arcEndPoint = new Point(
                center + radius * Math.Sin(sweepRadians),
                center - radius * Math.Cos(sweepRadians));

            var geometry = new PathGeometry();
            var figure = new PathFigure { StartPoint = new Point(center, center) };
            figure.Segments.Add(new LineSegment { Point = startPoint });
            figure.Segments.Add(new ArcSegment
            {
                Point = arcEndPoint,
                Size = new Size(radius, radius),
                SweepDirection = SweepDirection.Clockwise,
                IsLargeArc = angle > 180d
            });
            figure.Segments.Add(new LineSegment { Point = new Point(center, center) });
            geometry.Figures.Add(figure);
            ProgressFill.Data = geometry;
        }

        private Brush GetDefaultBrush()
        {
            if (Application.Current?.Resources.TryGetValue("PrimaryColor", out var resource) == true && resource is Brush brush)
            {
                return brush;
            }

            return new SolidColorBrush(Colors.Red);
        }
    }
}
