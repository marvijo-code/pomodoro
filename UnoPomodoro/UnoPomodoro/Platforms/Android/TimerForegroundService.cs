using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using AndroidX.Core.App;
using UnoPomodoro.Services;

namespace UnoPomodoro.Platforms.Android
{
    [Service(
        Name = "com.marvijocode.pomodoro.TimerForegroundService",
        Exported = false,
        ForegroundServiceType = global::Android.Content.PM.ForegroundService.TypeSpecialUse)]
    public class TimerForegroundService : Service
    {
        private const int TimerNotificationId = 1001;
        public const int CompletionNotificationId = 1002;
        private const string TimerChannelId = "TimerChannel";
        private const string TimerChannelName = "Timer Active";
        private const string CompletionChannelId = "timer_completion";
        private const string CompletionChannelName = "Timer Completion";
        
        private PowerManager.WakeLock? _wakeLock;
        private static TimerForegroundService? _instance;
        private string _currentMode = "pomodoro";
        private int _totalSeconds = 25 * 60;
        private bool _completionFired;
        
        // Services for triggering vibration/sound directly from the service thread,
        // bypassing the UI dispatcher which is suspended when the phone is locked.
        private static IVibrationService? _vibrationService;
        private static ISoundService? _soundService;
        private static bool _soundEnabled;
        private static bool _vibrationEnabled;
        
        /// <summary>
        /// Whether the completion alarm (sound + vibration) was started by this service.
        /// The ViewModel checks this to avoid double-triggering when the UI resumes.
        /// </summary>
        public static bool CompletionAlarmStarted { get; private set; }
        
        public static TimerForegroundService? Instance => _instance;
        
        /// <summary>
        /// Registers the sound and vibration services so the foreground service can
        /// trigger them directly when the timer completes (even on the lock screen).
        /// </summary>
        public static void RegisterAlarmServices(
            ISoundService? soundService,
            IVibrationService? vibrationService,
            bool soundEnabled,
            bool vibrationEnabled)
        {
            _soundService = soundService;
            _vibrationService = vibrationService;
            _soundEnabled = soundEnabled;
            _vibrationEnabled = vibrationEnabled;
        }
        
        /// <summary>
        /// Updates the enabled state of sound/vibration (called when settings change).
        /// </summary>
        public static void UpdateAlarmSettings(bool soundEnabled, bool vibrationEnabled)
        {
            _soundEnabled = soundEnabled;
            _vibrationEnabled = vibrationEnabled;
        }

        public override IBinder? OnBind(Intent? intent)
        {
            return null;
        }
        
        public override void OnCreate()
        {
            base.OnCreate();
            _instance = this;
        }

        public override StartCommandResult OnStartCommand(Intent? intent, StartCommandFlags flags, int startId)
        {
            // Read mode and total duration from intent
            if (intent != null)
            {
                _currentMode = intent.GetStringExtra("mode") ?? "pomodoro";
                _totalSeconds = intent.GetIntExtra("totalSeconds", 25 * 60);
            }
            
            _completionFired = false;
            
            CreateNotificationChannels();
            AcquireWakeLock();

            var contentIntent = CreateOpenAppIntent();
            
            var modeTitle = GetModeTitle();
            var notification = new NotificationCompat.Builder(this, TimerChannelId)
                .SetContentTitle(modeTitle)
                .SetContentText("Timer is running...")
                .SetSmallIcon(GetModeIcon())
                .SetOngoing(true)
                .SetCategory(NotificationCompat.CategoryProgress)
                .SetPriority(NotificationCompat.PriorityLow)
                .SetProgress(_totalSeconds, 0, false)
                .SetContentIntent(contentIntent)
                .Build();

            if (OperatingSystem.IsAndroidVersionAtLeast(34))
            {
                StartForeground(TimerNotificationId, notification, global::Android.Content.PM.ForegroundService.TypeSpecialUse);
            }
            else
            {
                StartForeground(TimerNotificationId, notification);
            }

            return StartCommandResult.Sticky;
        }
        
        private PendingIntent? CreateOpenAppIntent()
        {
            var intent = new Intent(this, typeof(global::UnoPomodoro.Droid.MainActivity));
            intent.SetFlags(ActivityFlags.SingleTop | ActivityFlags.ClearTop);

            var flags = PendingIntentFlags.UpdateCurrent;
            if (OperatingSystem.IsAndroidVersionAtLeast(23))
            {
                flags |= PendingIntentFlags.Immutable;
            }

            return PendingIntent.GetActivity(this, 0, intent, flags);
        }
        
        private string GetModeTitle()
        {
            return _currentMode switch
            {
                "pomodoro" => "Focus Session",
                "shortBreak" => "Short Break",
                "longBreak" => "Long Break",
                _ => "Timer"
            };
        }
        
        private int GetModeIcon()
        {
            return _currentMode switch
            {
                "pomodoro" => global::Android.Resource.Drawable.IcMediaPlay,
                "shortBreak" => global::Android.Resource.Drawable.IcMediaPause,
                "longBreak" => global::Android.Resource.Drawable.IcMediaPause,
                _ => global::Android.Resource.Drawable.IcMediaPlay
            };
        }
        
        private void AcquireWakeLock()
        {
            if (_wakeLock == null)
            {
                var powerManager = GetSystemService(PowerService) as PowerManager;
                if (powerManager != null)
                {
                    var wakeLock = powerManager.NewWakeLock(
                        WakeLockFlags.Partial,
                        "UnoPomodoro::TimerWakeLock");
                    if (wakeLock != null)
                    {
                        wakeLock.SetReferenceCounted(false);
                        _wakeLock = wakeLock;
                    }
                }
            }
            
            if (_wakeLock != null && !_wakeLock.IsHeld)
            {
                // Acquire wake lock for up to 60 minutes (covers long break + buffer)
                _wakeLock.Acquire(60 * 60 * 1000);
            }
        }
        
        private void ReleaseWakeLock()
        {
            if (_wakeLock != null && _wakeLock.IsHeld)
            {
                try
                {
                    _wakeLock.Release();
                }
                catch (System.Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error releasing wake lock: {ex.Message}");
                }
            }
        }
        
        /// <summary>
        /// Updates the ongoing timer notification with remaining time and progress.
        /// When remaining reaches 0, fires a high-priority completion notification.
        /// This method is called from a thread pool thread, so it works even when the app is in background.
        /// </summary>
        public void UpdateNotification(int remainingSeconds)
        {
            if (remainingSeconds <= 0 && !_completionFired)
            {
                _completionFired = true;
                ShowCompletionNotification();
                return;
            }
            
            if (remainingSeconds <= 0)
            {
                return; // Already fired completion
            }
            
            var minutes = remainingSeconds / 60;
            var seconds = remainingSeconds % 60;
            var elapsed = _totalSeconds - remainingSeconds;
            var modeTitle = GetModeTitle();
            var contentIntent = CreateOpenAppIntent();
            
            var notification = new NotificationCompat.Builder(this, TimerChannelId)
                .SetContentTitle(modeTitle)
                .SetContentText($"{minutes:D2}:{seconds:D2} remaining")
                .SetSmallIcon(GetModeIcon())
                .SetOngoing(true)
                .SetCategory(NotificationCompat.CategoryProgress)
                .SetPriority(NotificationCompat.PriorityLow)
                .SetProgress(_totalSeconds, elapsed, false)
                .SetContentIntent(contentIntent)
                .SetShowWhen(false)
                .Build();
                
            var notificationManager = GetSystemService(NotificationService) as NotificationManager;
            notificationManager?.Notify(TimerNotificationId, notification);
        }
        
        /// <summary>
        /// Posts a high-priority completion notification with full-screen intent,
        /// and directly triggers vibration and sound from the service thread.
        /// This fires even when the phone is locked, bypassing the UI dispatcher.
        /// </summary>
        private void ShowCompletionNotification()
        {
            try
            {
                var isPomodoro = _currentMode == "pomodoro";
                var title = isPomodoro ? "Pomodoro Complete!" : "Break Over!";
                var message = isPomodoro 
                    ? "Great work! Time for a break." 
                    : "Break is over. Ready to focus?";

                var openIntent = new Intent(this, typeof(global::UnoPomodoro.Droid.MainActivity));
                openIntent.SetFlags(ActivityFlags.SingleTop | ActivityFlags.ClearTop);
                
                var fullScreenIntent = PendingIntent.GetActivity(
                    this, 1, openIntent,
                    GetPendingIntentFlags());
                 
                var contentIntent = PendingIntent.GetActivity(
                    this, 2, openIntent,
                    GetPendingIntentFlags());

                var notification = new NotificationCompat.Builder(this, CompletionChannelId)
                    .SetContentTitle(title)
                    .SetContentText(message)
                    .SetSmallIcon(global::Android.Resource.Drawable.IcDialogInfo)
                    .SetPriority(NotificationCompat.PriorityHigh)
                    .SetCategory(NotificationCompat.CategoryAlarm)
                    .SetFullScreenIntent(fullScreenIntent, true)
                    .SetContentIntent(contentIntent)
                    .SetAutoCancel(true)
                    .SetDefaults(NotificationCompat.DefaultAll)
                    .SetVibrate(new long[] { 0, 500, 300, 500, 300, 500, 300, 500 })
                    .Build();
                
                var notificationManager = GetSystemService(NotificationService) as NotificationManager;
                notificationManager?.Notify(CompletionNotificationId, notification);
                
                // Directly trigger repeating vibration and alarm sound from the
                // service thread. This is the key fix: these calls do NOT require
                // the UI dispatcher, so they work even when the phone is locked.
                StartCompletionAlarm();
                
                System.Diagnostics.Debug.WriteLine($"Completion notification posted: {title}");
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error showing completion notification: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Starts the repeating vibration and alarm sound directly from the service thread.
        /// Called when the timer hits zero, regardless of whether the UI is active.
        /// </summary>
        private void StartCompletionAlarm()
        {
            CompletionAlarmStarted = false;
            
            try
            {
                if (_vibrationEnabled && _vibrationService != null && _vibrationService.IsSupported)
                {
                    _vibrationService.VibratePattern(new long[] { 0, 400, 200, 400, 200, 400 }, true);
                    CompletionAlarmStarted = true;
                    System.Diagnostics.Debug.WriteLine("Completion vibration started from foreground service");
                }
                
                if (_soundEnabled && _soundService != null)
                {
                    _soundService.PlayNotificationSound();
                    CompletionAlarmStarted = true;
                    System.Diagnostics.Debug.WriteLine("Completion sound started from foreground service");
                }
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error starting completion alarm: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Dismisses the completion notification (called when user interacts with the in-app modal).
        /// </summary>
        public static void DismissCompletionNotification()
        {
            try
            {
                var context = global::Android.App.Application.Context;
                var notificationManager = NotificationManagerCompat.From(context);
                notificationManager.Cancel(CompletionNotificationId);
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error dismissing completion notification: {ex.Message}");
            }
        }

        private void CreateNotificationChannels()
        {
            if (OperatingSystem.IsAndroidVersionAtLeast(26))
            {
                var notificationManager = GetSystemService(NotificationService) as NotificationManager;
                
                // Low-priority channel for ongoing timer progress
                var timerChannel = new NotificationChannel(TimerChannelId, TimerChannelName, NotificationImportance.Low);
                timerChannel.SetShowBadge(false);
                notificationManager?.CreateNotificationChannel(timerChannel);
                
                // High-priority channel for timer completion alerts
                var completionChannel = new NotificationChannel(CompletionChannelId, CompletionChannelName, NotificationImportance.High)
                {
                    Description = "Alerts when a Pomodoro or break timer completes"
                };
                completionChannel.EnableVibration(true);
                completionChannel.SetVibrationPattern(new long[] { 0, 500, 300, 500, 300, 500 });
                completionChannel.EnableLights(true);
                completionChannel.LockscreenVisibility = NotificationVisibility.Public;
                notificationManager?.CreateNotificationChannel(completionChannel);
            }
        }

        private static PendingIntentFlags GetPendingIntentFlags()
        {
            var flags = PendingIntentFlags.UpdateCurrent;
            if (OperatingSystem.IsAndroidVersionAtLeast(23))
            {
                flags |= PendingIntentFlags.Immutable;
            }

            return flags;
        }
        
        public override void OnDestroy()
        {
            ReleaseWakeLock();
            CompletionAlarmStarted = false;
            _instance = null;
            base.OnDestroy();

            if (OperatingSystem.IsAndroidVersionAtLeast(24))
            {
                StopForeground(StopForegroundFlags.Remove);
            }
            else
            {
#pragma warning disable CS0618
                StopForeground(true);
#pragma warning restore CS0618
            }
        }
    }
}
