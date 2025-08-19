using System;
using Microsoft.UI.Xaml;

namespace UnoPomodoro.Services
{
    public class TimerService
    {
        private DispatcherTimer _timer;
        private DateTime _targetEndTime;
        private int _remainingSeconds;
        private bool _isRunning;

        public event EventHandler<int>? Tick;
        public event EventHandler? TimerCompleted;

        public TimerService()
        {
            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromSeconds(1);
            _timer.Tick += OnTimerTick;
        }

        public void Start(int seconds)
        {
            _remainingSeconds = seconds;
            _targetEndTime = DateTime.Now.AddSeconds(seconds);
            _isRunning = true;
            _timer.Start();
        }

        public void Pause()
        {
            _isRunning = false;
            _timer.Stop();
        }

        public void Reset(int seconds)
        {
            _isRunning = false;
            _timer.Stop();
            _remainingSeconds = seconds;
            Tick?.Invoke(this, _remainingSeconds);
        }

        public void Resume()
        {
            if (!_isRunning)
            {
                // Recompute remaining seconds from wall clock to avoid drift
                var remaining = _targetEndTime.Subtract(DateTime.Now);
                _remainingSeconds = (int)remaining.TotalSeconds;
                if (_remainingSeconds <= 0)
                {
                    _remainingSeconds = 0;
                    TimerCompleted?.Invoke(this, EventArgs.Empty);
                }
                else
                {
                    _isRunning = true;
                    _timer.Start();
                }
            }
        }

        public bool IsRunning => _isRunning;

        public int RemainingSeconds => _remainingSeconds;

        private void OnTimerTick(object? sender, object e)
        {
            if (_isRunning)
            {
                _remainingSeconds--;
                Tick?.Invoke(this, _remainingSeconds);

                if (_remainingSeconds <= 0)
                {
                    _timer.Stop();
                    _isRunning = false;
                    TimerCompleted?.Invoke(this, EventArgs.Empty);
                }
            }
        }
    }
}
