using System;
using System.Threading;
using Microsoft.UI.Dispatching;

namespace UnoPomodoro.Services
{
    public partial class TimerService : ITimerService
    {
        private const int MaxSignalDurationMs = 5_000;
        private const int MaxSignalIntervalMs = 5 * 60 * 1_000;

        private Timer? _timer;
        private DateTime _targetEndTime;
        private int _remainingSeconds;
        private bool _isRunning;
        private readonly DispatcherQueue _dispatcherQueue;
        private CancellationTokenSource? _signalLoopCts;
        private ISoundService? _registeredSoundService;
        private IVibrationService? _registeredVibrationService;
        private bool _isRepeatingSignalLoopRunning;

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
            if (_isRunning)
            {
                var remaining = _targetEndTime.Subtract(DateTime.UtcNow);
                _remainingSeconds = Math.Max(0, (int)Math.Ceiling(remaining.TotalSeconds));
                _targetEndTime = DateTime.UtcNow.AddSeconds(_remainingSeconds);
            }

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
            _targetEndTime = DateTime.UtcNow.AddSeconds(seconds);
            TotalDurationSeconds = Math.Max(1, seconds);
            Tick?.Invoke(this, _remainingSeconds);
        }

        public void Resume()
        {
            if (!_isRunning)
            {
                // Recompute remaining seconds from wall clock to avoid drift
                var remaining = _targetEndTime.Subtract(DateTime.UtcNow);
                _remainingSeconds = Math.Max(0, (int)Math.Ceiling(remaining.TotalSeconds));
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

        public void RestoreState(int remainingSeconds, DateTime targetEndTimeUtc, bool isRunning, int totalDurationSeconds, string mode)
        {
            StopTimer();
            StopPlatformBackgroundService();

            _remainingSeconds = Math.Max(0, remainingSeconds);
            _targetEndTime = targetEndTimeUtc.Kind == DateTimeKind.Utc
                ? targetEndTimeUtc
                : targetEndTimeUtc.ToUniversalTime();
            _isRunning = isRunning && _remainingSeconds > 0;
            TotalDurationSeconds = Math.Max(1, totalDurationSeconds);
            CurrentMode = string.IsNullOrWhiteSpace(mode) ? "pomodoro" : mode;

            if (_isRunning)
            {
                StartTimer();
                StartPlatformBackgroundService();
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
        public bool IsRepeatingSignalLoopRunning
        {
            get
            {
#if __ANDROID__
                return _isRepeatingSignalLoopRunning || UnoPomodoro.Platforms.Android.SignalForegroundService.IsSignalLoopRunning;
#else
                return _isRepeatingSignalLoopRunning;
#endif
            }
        }

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
            _registeredSoundService = soundService;
            _registeredVibrationService = vibrationService;
            RegisterAlarmServicesPlatform(soundService, vibrationService, soundEnabled, vibrationEnabled, vibrationDurationSeconds);
        }
        
        public void UpdateAlarmSettings(bool soundEnabled, bool vibrationEnabled, int vibrationDurationSeconds = 5)
        {
            UpdateAlarmSettingsPlatform(soundEnabled, vibrationEnabled, vibrationDurationSeconds);
        }

        public void StartRepeatingSignalLoop(bool useSound, bool useVibration, int durationMs, int intervalMs, double soundVolumePercent = 100)
        {
            var sanitizedDurationMs = Math.Clamp(durationMs, 100, MaxSignalDurationMs);
            var sanitizedIntervalMs = Math.Clamp(intervalMs, 0, MaxSignalIntervalMs);
            var sanitizedSoundVolume = Math.Clamp(soundVolumePercent, 0, 100);

            if (!useSound && !useVibration)
            {
                return;
            }

#if __ANDROID__
            // The Android foreground service updates its own active loop when it
            // receives a fresh start intent, so sending a stop first only creates
            // a race where the service can be torn down before startForeground().
            StartPlatformSignalLoop(useSound, useVibration, sanitizedDurationMs, sanitizedIntervalMs, sanitizedSoundVolume);
            _isRepeatingSignalLoopRunning = true;
#else
            StopRepeatingSignalLoop();

            if ((useSound && _registeredSoundService == null)
                || (useVibration && (_registeredVibrationService == null || !_registeredVibrationService.IsSupported)))
            {
                return;
            }

            if (useSound && _registeredSoundService != null)
            {
                _registeredSoundService.Volume = sanitizedSoundVolume / 100.0;
            }

            var tokenSource = new CancellationTokenSource();
            _signalLoopCts = tokenSource;
            _isRepeatingSignalLoopRunning = true;
            _ = RunRepeatingSignalLoopAsync(useSound, useVibration, sanitizedDurationMs, sanitizedIntervalMs, tokenSource);
#endif
        }

        public void StopRepeatingSignalLoop()
        {
#if __ANDROID__
            if (_isRepeatingSignalLoopRunning || UnoPomodoro.Platforms.Android.SignalForegroundService.IsSignalLoopRunning)
            {
                StopPlatformSignalLoop();
            }
#endif

            var tokenSource = _signalLoopCts;
            _signalLoopCts = null;

            if (tokenSource != null)
            {
                tokenSource.Cancel();
                tokenSource.Dispose();
            }

            _registeredSoundService?.StopNotificationSound();
            _registeredVibrationService?.Cancel();
            _isRepeatingSignalLoopRunning = false;
        }

#if !__ANDROID__
        private async System.Threading.Tasks.Task RunRepeatingSignalLoopAsync(
            bool useSound,
            bool useVibration,
            int durationMs,
            int intervalMs,
            CancellationTokenSource tokenSource)
        {
            var token = tokenSource.Token;

            try
            {
                while (!token.IsCancellationRequested)
                {
                    if (useSound)
                    {
                        _registeredSoundService?.PlayNotificationSound(durationMs);
                    }

                    if (useVibration)
                    {
                        _registeredVibrationService?.Vibrate(durationMs);
                    }

                    await System.Threading.Tasks.Task.Delay(durationMs, token);

                    if (intervalMs > 0)
                    {
                        await System.Threading.Tasks.Task.Delay(intervalMs, token);
                    }
                }
            }
            catch (TaskCanceledException)
            {
                // expected when stopping the signal loop
            }
            finally
            {
                if (ReferenceEquals(_signalLoopCts, tokenSource))
                {
                    _signalLoopCts = null;
                    _isRepeatingSignalLoopRunning = false;
                }
            }
        }
#endif
        
        partial void RegisterAlarmServicesPlatform(
            ISoundService? soundService,
            IVibrationService? vibrationService,
            bool soundEnabled,
            bool vibrationEnabled,
            int vibrationDurationSeconds);
        partial void UpdateAlarmSettingsPlatform(bool soundEnabled, bool vibrationEnabled, int vibrationDurationSeconds);
#if __ANDROID__
        partial void StartPlatformSignalLoop(bool useSound, bool useVibration, int durationMs, int intervalMs, double soundVolumePercent);
        partial void StopPlatformSignalLoop();
#endif
    }
}
