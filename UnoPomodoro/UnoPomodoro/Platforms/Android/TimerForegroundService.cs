using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using AndroidX.Core.App;

namespace UnoPomodoro.Platforms.Android
{
    [Service]
    public class TimerForegroundService : Service
    {
        private const int NotificationId = 1001;
        private const string ChannelId = "TimerChannel";
        private const string ChannelName = "Timer Active";

        public override IBinder? OnBind(Intent? intent)
        {
            return null;
        }

        public override StartCommandResult OnStartCommand(Intent? intent, StartCommandFlags flags, int startId)
        {
            CreateNotificationChannel();

            var notification = new NotificationCompat.Builder(this, ChannelId)
                .SetContentTitle("Pomodoro Timer")
                .SetContentText("Timer is running...")
                .SetSmallIcon(global::Android.Resource.Drawable.IcMediaPlay)
                .SetOngoing(true)
                .Build();

            if (Build.VERSION.SdkInt >= BuildVersionCodes.UpsideDownCake) // Android 14
            {
                 StartForeground(NotificationId, notification, global::Android.Content.PM.ForegroundService.TypeSpecialUse);
            }
            else if (Build.VERSION.SdkInt >= BuildVersionCodes.Q)
            {
                 StartForeground(NotificationId, notification, global::Android.Content.PM.ForegroundService.TypeManifest);
            }
            else
            {
                StartForeground(NotificationId, notification);
            }

            return StartCommandResult.Sticky;
        }

        private void CreateNotificationChannel()
        {
            if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
            {
                var channel = new NotificationChannel(ChannelId, ChannelName, NotificationImportance.Low);
                var notificationManager = GetSystemService(NotificationService) as NotificationManager;
                notificationManager?.CreateNotificationChannel(channel);
            }
        }
        
        public override void OnDestroy()
        {
            base.OnDestroy();
            StopForeground(true);
        }
    }
}
