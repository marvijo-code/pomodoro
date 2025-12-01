namespace UnoPomodoro.Services;

public interface ISoundService
{
    double Volume { get; set; }
    int Duration { get; set; } // Duration in seconds
    void PlayNotificationSound();
    void StopNotificationSound();
}
