using System;

namespace UnoPomodoro.Services;

public interface ITimerService
{
    event EventHandler<int>? Tick;
    event EventHandler? TimerCompleted;

    void Start(int durationSeconds);
    void Pause();
    void Reset(int durationSeconds);
    void Resume();
    void SyncWithWallClock();

    bool IsRunning { get; }
    int RemainingSeconds { get; }
    DateTime TargetEndTime { get; }
    
    /// <summary>
    /// The current timer mode (e.g., "pomodoro", "shortBreak", "longBreak").
    /// Used by the platform to display mode-aware notifications.
    /// </summary>
    string CurrentMode { get; set; }
    
    /// <summary>
    /// The total duration in seconds for the current timer session.
    /// Used by the platform to show progress in notifications.
    /// </summary>
    int TotalDurationSeconds { get; set; }
}
