using System;
using System.Threading.Tasks;
using Uno.Extensions;

namespace UnoPomodoro.Services;

public class NotificationService : INotificationService
{
    public async Task ShowNotificationAsync(string title, string content)
    {
        // On Android, we can use the Uno implementation directly
        // This will show a toast notification
        try
        {
            // Create a simple notification - implementation will depend on Uno Platform specifics
            System.Diagnostics.Debug.WriteLine($"Notification: {title} - {content}");

            // For now, we'll just log the notification
            // In a real implementation, we would use Android's NotificationManager
            await Task.Delay(100); // Simulate async work
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error showing notification: {ex.Message}");
        }
    }
}
