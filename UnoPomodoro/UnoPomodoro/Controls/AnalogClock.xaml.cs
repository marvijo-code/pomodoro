using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Shapes;
using System;

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

        public AnalogClock()
        {
            this.InitializeComponent();
            this.Loaded += AnalogClock_Loaded;
        }

        private void AnalogClock_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateClock();
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

        private void UpdateClockLayout()
        {
            double center = ClockSize / 2;

            // Position hour markers
            if (Marker12 != null)
            {
                Canvas.SetLeft(Marker12, center);
                Canvas.SetTop(Marker12, 10);
            }

            if (Marker3 != null)
            {
                Canvas.SetLeft(Marker3, ClockSize - 25);
                Canvas.SetTop(Marker3, center);
                Marker3.RenderTransform = new RotateTransform { Angle = 90, CenterX = 7.5, CenterY = 7.5 };
            }

            if (Marker6 != null)
            {
                Canvas.SetLeft(Marker6, center);
                Canvas.SetTop(Marker6, ClockSize - 25);
            }

            if (Marker9 != null)
            {
                Canvas.SetLeft(Marker9, 10);
                Canvas.SetTop(Marker9, center);
                Marker9.RenderTransform = new RotateTransform { Angle = 90, CenterX = 7.5, CenterY = 7.5 };
            }

            // Update hands
            UpdateClock();
        }

        private void UpdateClock()
        {
            if (ClockCanvas == null) return;

            double center = ClockSize / 2;
            double radius = ClockSize / 2;

            // Calculate elapsed time
            var elapsed = TotalTime - TimeLeft;
            double totalMinutes = TotalTime.TotalMinutes;
            double elapsedMinutes = elapsed.TotalMinutes;

            // Calculate angles (12 o'clock is 0 degrees, clockwise)
            // For a countdown timer, we want to show progress
            double minuteAngle = (elapsedMinutes / totalMinutes) * 360;
            double hourAngle = (elapsedMinutes / totalMinutes) * 360; // Same as minute for countdown

            // Minute hand (70% of radius)
            double minuteLength = radius * 0.7;
            UpdateHand(MinuteHand, center, minuteLength, minuteAngle);

            // Hour hand (50% of radius)
            double hourLength = radius * 0.5;
            UpdateHand(HourHand, center, hourLength, hourAngle);

            // Center the center dot
            Canvas.SetLeft(ClockCanvas.Children[^1] as Ellipse, center - 6);
            Canvas.SetTop(ClockCanvas.Children[^1] as Ellipse, center - 6);
        }

        private void UpdateHand(Line hand, double center, double length, double angleDegrees)
        {
            if (hand == null) return;

            // Convert angle to radians (0 degrees is at 12 o'clock, -90 offset)
            double angleRadians = (angleDegrees - 90) * Math.PI / 180;

            // Calculate end point
            double endX = center + length * Math.Cos(angleRadians);
            double endY = center + length * Math.Sin(angleRadians);

            // Set hand position
            Canvas.SetLeft(hand, center);
            Canvas.SetTop(hand, center);
            hand.X2 = endX - center;
            hand.Y2 = endY - center;
        }
    }
}
