using System.Threading.Tasks;

namespace UnoPomodoro.Services;

public interface INotificationService
{
    Task ShowNotificationAsync(string title, string content);
    
    /// <summary>
    /// Dismisses the timer completion notification (if any).
    /// </summary>
    void DismissCompletionNotification();
}
