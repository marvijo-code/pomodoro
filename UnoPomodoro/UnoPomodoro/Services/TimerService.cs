using System;
using System.Threading;
using Microsoft.UI.Dispatching;

namespace UnoPomodoro.Services
{
    public partial class TimerService : ITimerService
    {
        private Timer? _timer;
        private DateTime _targetEndTime;
        private int _remainingSeconds;
        private bool _isRunning;
        private readonly DispatcherQueue _dispatcherQueue;

        public event EventHandler<int>? Tick;
        public event EventHandler? TimerCompleted;

        public TimerService()
        {
            _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
        }

        public void Start(int seconds)
        {
            _remainingSeconds = seconds;
            _targetEndTime = DateTime.Now.AddSeconds(seconds);
            _isRunning = true;
            
            StartTimer();
            StartPlatformBackgroundService();
        }

        public void Pause()
        {
            _isRunning = false;
            StopTimer();
            StopPlatformBackgroundService();
        }

        public void Reset(int seconds)
        {
            _isRunning = false;
            StopTimer();
            StopPlatformBackgroundService();
            
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
                    StartTimer();
                    StartPlatformBackgroundService();
                }
            }
        }

        public bool IsRunning => _isRunning;

        public int RemainingSeconds => _remainingSeconds;

        private void StartTimer()
        {
            _timer?.Dispose();
            _timer = new Timer(OnTimerTick, null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));
        }

        private void StopTimer()
        {
            _timer?.Change(Timeout.Infinite, Timeout.Infinite);
            _timer?.Dispose();
            _timer = null;
        }

        private void OnTimerTick(object? state)
        {
            if (_isRunning)
            {
                _remainingSeconds--;
                
                // Marshal to UI thread
                _dispatcherQueue.TryEnqueue(() =>
                {
                    Tick?.Invoke(this, _remainingSeconds);

                    if (_remainingSeconds <= 0)
                    {
                        StopTimer();
                        _isRunning = false;
                        StopPlatformBackgroundService();
                        TimerCompleted?.Invoke(this, EventArgs.Empty);
                    }
                });
            }
        }

        partial void StartPlatformBackgroundService();
        partial void StopPlatformBackgroundService();
    }
}
