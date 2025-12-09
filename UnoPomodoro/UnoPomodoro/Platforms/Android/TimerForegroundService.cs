using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using AndroidX.Core.App;

namespace UnoPomodoro.Platforms.Android
{
    [Service(
        Name = "com.marvijocode.pomodoro.TimerForegroundService",
        Exported = false,
        ForegroundServiceType = global::Android.Content.PM.ForegroundService.TypeSpecialUse)]
    public class TimerForegroundService : Service
    {
        private const int NotificationId = 1001;
        private const string ChannelId = "TimerChannel";
        private const string ChannelName = "Timer Active";
        
        private PowerManager.WakeLock? _wakeLock;
        private static TimerForegroundService? _instance;
        
        public static TimerForegroundService? Instance => _instance;

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
            CreateNotificationChannel();
            AcquireWakeLock();

            var notification = new NotificationCompat.Builder(this, ChannelId)
                .SetContentTitle("Pomodoro Timer")
                .SetContentText("Timer is running...")
                .SetSmallIcon(global::Android.Resource.Drawable.IcMediaPlay)
                .SetOngoing(true)
                .SetCategory(NotificationCompat.CategoryProgress)
                .SetPriority(NotificationCompat.PriorityLow)
                .Build();

            if (Build.VERSION.SdkInt >= BuildVersionCodes.UpsideDownCake) // Android 14
            {
                 StartForeground(NotificationId, notification, global::Android.Content.PM.ForegroundService.TypeSpecialUse);
            }
            else
            {
                StartForeground(NotificationId, notification);
            }

            return StartCommandResult.Sticky;
        }
        
        private void AcquireWakeLock()
        {
            if (_wakeLock == null)
            {
                var powerManager = GetSystemService(PowerService) as PowerManager;
                if (powerManager != null)
                {
                    _wakeLock = powerManager.NewWakeLock(
                        WakeLockFlags.Partial,
                        "UnoPomodoro::TimerWakeLock");
                    _wakeLock.SetReferenceCounted(false);
                }
            }
            
            if (_wakeLock != null && !_wakeLock.IsHeld)
            {
                // Acquire wake lock for up to 30 minutes (max pomodoro session)
                _wakeLock.Acquire(30 * 60 * 1000);
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
        
        public void UpdateNotification(int remainingSeconds)
        {
            var minutes = remainingSeconds / 60;
            var seconds = remainingSeconds % 60;
            var timeText = $"{minutes:D2}:{seconds:D2} remaining";
            
            var notification = new NotificationCompat.Builder(this, ChannelId)
                .SetContentTitle("Pomodoro Timer")
                .SetContentText(timeText)
                .SetSmallIcon(global::Android.Resource.Drawable.IcMediaPlay)
                .SetOngoing(true)
                .SetCategory(NotificationCompat.CategoryProgress)
                .SetPriority(NotificationCompat.PriorityLow)
                .Build();
                
            var notificationManager = GetSystemService(NotificationService) as NotificationManager;
            notificationManager?.Notify(NotificationId, notification);
        }

        private void CreateNotificationChannel()
        {
            if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
            {
                var channel = new NotificationChannel(ChannelId, ChannelName, NotificationImportance.Low);
                channel.SetShowBadge(false);
                var notificationManager = GetSystemService(NotificationService) as NotificationManager;
                notificationManager?.CreateNotificationChannel(channel);
            }
        }
        
        public override void OnDestroy()
        {
            ReleaseWakeLock();
            _instance = null;
            base.OnDestroy();
            StopForeground(StopForegroundFlags.Remove);
        }
    }
}
