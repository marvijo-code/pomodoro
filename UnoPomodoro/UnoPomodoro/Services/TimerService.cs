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
        
        public DateTime TargetEndTime => _targetEndTime;
        
        public string CurrentMode { get; set; } = "pomodoro";
        public int TotalDurationSeconds { get; set; } = 25 * 60;

        public TimerService()
        {
            _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
        }

        public void Start(int seconds)
        {
            _remainingSeconds = seconds;
            _targetEndTime = DateTime.UtcNow.AddSeconds(seconds);
            _isRunning = true;
            TotalDurationSeconds = seconds;
            
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
                var remaining = _targetEndTime.Subtract(DateTime.UtcNow);
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
        
        /// <summary>
        /// Syncs the timer with the wall clock. Call this when app resumes or wakes from power saving.
        /// </summary>
        public void SyncWithWallClock()
        {
            if (_isRunning)
            {
                var remaining = _targetEndTime.Subtract(DateTime.UtcNow);
                var newRemainingSeconds = Math.Max(0, (int)remaining.TotalSeconds);
                
                if (newRemainingSeconds <= 0)
                {
                    _remainingSeconds = 0;
                    StopTimer();
                    _isRunning = false;
                    StopPlatformBackgroundService();
                    
                    _dispatcherQueue.TryEnqueue(() =>
                    {
                        Tick?.Invoke(this, 0);
                        TimerCompleted?.Invoke(this, EventArgs.Empty);
                    });
                }
                else if (Math.Abs(newRemainingSeconds - _remainingSeconds) > 1)
                {
                    // Only update if there's significant drift (more than 1 second)
                    _remainingSeconds = newRemainingSeconds;
                    
                    _dispatcherQueue.TryEnqueue(() =>
                    {
                        Tick?.Invoke(this, _remainingSeconds);
                    });
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
                // Always calculate remaining time from wall clock to handle power saving mode
                var remaining = _targetEndTime.Subtract(DateTime.UtcNow);
                _remainingSeconds = Math.Max(0, (int)remaining.TotalSeconds);
                
                // Update foreground service notification (runs on thread pool - handles lock screen)
                UpdatePlatformNotification(_remainingSeconds);
                
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
        partial void UpdatePlatformNotification(int remainingSeconds);
        
        public bool CompletionAlarmStartedByPlatform
        {
            get
            {
#if __ANDROID__
                return UnoPomodoro.Platforms.Android.TimerForegroundService.CompletionAlarmStarted;
#else
                return false;
#endif
            }
        }
        
        public void RegisterAlarmServices(
            ISoundService? soundService,
            IVibrationService? vibrationService,
            bool soundEnabled,
            bool vibrationEnabled,
            int vibrationDurationSeconds = 5)
        {
            RegisterAlarmServicesPlatform(soundService, vibrationService, soundEnabled, vibrationEnabled, vibrationDurationSeconds);
        }
        
        public void UpdateAlarmSettings(bool soundEnabled, bool vibrationEnabled, int vibrationDurationSeconds = 5)
        {
            UpdateAlarmSettingsPlatform(soundEnabled, vibrationEnabled, vibrationDurationSeconds);
        }
        
        partial void RegisterAlarmServicesPlatform(
            ISoundService? soundService,
            IVibrationService? vibrationService,
            bool soundEnabled,
            bool vibrationEnabled,
            int vibrationDurationSeconds);
        partial void UpdateAlarmSettingsPlatform(bool soundEnabled, bool vibrationEnabled, int vibrationDurationSeconds);
    }
}
