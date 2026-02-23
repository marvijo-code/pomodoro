using System;
using System.Threading.Tasks;

namespace UnoPomodoro.Services;

public class NotificationService : INotificationService
{
#if __ANDROID__
    private const string ChannelId = "pomodoro_notifications";
    private const string ChannelName = "Pomodoro Notifications";
    private const string ChannelDescription = "Notifications for Pomodoro timer events";
    private int _notificationId = 2000; // Start above the foreground service IDs

    public NotificationService()
    {
        CreateNotificationChannel();
    }

    private void CreateNotificationChannel()
    {
        if (OperatingSystem.IsAndroidVersionAtLeast(26))
        {
            var channel = new Android.App.NotificationChannel(
                ChannelId,
                ChannelName,
                Android.App.NotificationImportance.High)
            {
                Description = ChannelDescription
            };

            var notificationManager = Android.App.NotificationManager.FromContext(
                Android.App.Application.Context);
            notificationManager?.CreateNotificationChannel(channel);
        }
    }

    public Task ShowNotificationAsync(string title, string content)
    {
        try
        {
            var context = Android.App.Application.Context;

            var builder = new AndroidX.Core.App.NotificationCompat.Builder(context, ChannelId)
                .SetSmallIcon(Android.Resource.Drawable.IcDialogInfo)
                .SetContentTitle(title)
                .SetContentText(content)
                .SetPriority(AndroidX.Core.App.NotificationCompat.PriorityHigh)
                .SetAutoCancel(true);

            var notificationManager = AndroidX.Core.App.NotificationManagerCompat.From(context);
            notificationManager.Notify(_notificationId++, builder.Build());
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error showing notification: {ex.Message}");
        }

        return Task.CompletedTask;
    }
    
    public void DismissCompletionNotification()
    {
        try
        {
            UnoPomodoro.Platforms.Android.TimerForegroundService.DismissCompletionNotification();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error dismissing completion notification: {ex.Message}");
        }
    }
#else
    public Task ShowNotificationAsync(string title, string content)
    {
        System.Diagnostics.Debug.WriteLine($"Notification: {title} - {content}");
        return Task.CompletedTask;
    }
    
    public void DismissCompletionNotification()
    {
        // No-op on non-Android platforms
    }
#endif
}
