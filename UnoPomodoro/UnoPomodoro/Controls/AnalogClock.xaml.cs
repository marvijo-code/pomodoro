using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using System;
using System.Collections.Generic;

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

        private readonly List<Line> _hourMarkers = new();

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

        private void UpdateClockLayout()
        {
            if (ClockCanvas == null)
            {
                return;
            }

            EnsureMarkers();

            double size = ClockSize > 0 ? ClockSize : 200;
            double center = size / 2;
            double radius = center - 8;

            ClockCanvas.Width = size;
            ClockCanvas.Height = size;

            if (ClockFace != null)
            {
                ClockFace.Width = size;
                ClockFace.Height = size;
            }

            if (MarkerLayer != null)
            {
                MarkerLayer.Width = size;
                MarkerLayer.Height = size;
            }

            for (int i = 0; i < _hourMarkers.Count; i++)
            {
                var marker = _hourMarkers[i];
                double angleDegrees = i * 30d;
                double angleRadians = angleDegrees * Math.PI / 180d;

                double outer = radius;
                double innerOffset = i % 3 == 0 ? 18 : 12;
                double inner = radius - innerOffset;

                double cos = Math.Cos(angleRadians);
                double sin = Math.Sin(angleRadians);

                marker.X1 = center + inner * cos;
                marker.Y1 = center + inner * sin;
                marker.X2 = center + outer * cos;
                marker.Y2 = center + outer * sin;
            }

            if (CenterDot != null)
            {
                Canvas.SetLeft(CenterDot, center - CenterDot.Width / 2);
                Canvas.SetTop(CenterDot, center - CenterDot.Height / 2);
            }

            UpdateClock();
        }

        private void UpdateClock()
        {
            if (ClockCanvas == null)
            {
                return;
            }

            EnsureMarkers();

            double size = ClockSize > 0 ? ClockSize : 200;
            double center = size / 2;
            double radius = center - 8;

            var elapsed = TotalTime - TimeLeft;
            if (elapsed < TimeSpan.Zero)
            {
                elapsed = TimeSpan.Zero;
            }
            if (elapsed > TotalTime)
            {
                elapsed = TotalTime;
            }

            double elapsedSeconds = elapsed.TotalSeconds;
            double seconds = elapsedSeconds % 60d;
            double minutes = (elapsedSeconds / 60d) % 60d;
            double hours = (elapsedSeconds / 3600d) % 12d;

            double secondAngle = (seconds / 60d) * 360d;
            double minuteAngle = (minutes / 60d) * 360d + (seconds / 60d) * 6d;
            double hourAngle = (hours / 12d) * 360d + (minutes / 60d) * 30d;

            UpdateHand(SecondHand, center, radius * 0.9, secondAngle);
            UpdateHand(MinuteHand, center, radius * 0.75, minuteAngle);
            UpdateHand(HourHand, center, radius * 0.55, hourAngle);
        }

        private void UpdateHand(Line hand, double center, double length, double angleDegrees)
        {
            if (hand == null) return;

            double angleRadians = (angleDegrees - 90d) * Math.PI / 180d;
            double endX = center + length * Math.Cos(angleRadians);
            double endY = center + length * Math.Sin(angleRadians);

            hand.X1 = center;
            hand.Y1 = center;
            hand.X2 = endX;
            hand.Y2 = endY;
        }

        private void EnsureMarkers()
        {
            if (MarkerLayer == null || _hourMarkers.Count > 0)
            {
                return;
            }

            Brush markerBrush;
            if (Application.Current?.Resources.TryGetValue("OnSurfaceVariantColor", out var resource) == true && resource is Brush brush)
            {
                markerBrush = brush;
            }
            else
            {
                markerBrush = new SolidColorBrush(Colors.White);
            }

            for (int i = 0; i < 12; i++)
            {
                var marker = new Line
                {
                    Stroke = markerBrush,
                    StrokeThickness = i % 3 == 0 ? 3 : 2
                };
                MarkerLayer.Children.Add(marker);
                _hourMarkers.Add(marker);
            }
        }
    }
}
