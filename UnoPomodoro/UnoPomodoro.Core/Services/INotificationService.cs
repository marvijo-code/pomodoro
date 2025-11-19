using System.Threading.Tasks;

namespace UnoPomodoro.Services;

public interface INotificationService
{
    Task ShowNotificationAsync(string title, string content);
}
