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
    
    /// <summary>
    /// Whether the platform's background service already started the completion alarm
    /// (vibration + sound). When true, the ViewModel should skip starting these itself
    /// to avoid double-triggering. On platforms without a background service, this
    /// always returns false.
    /// </summary>
    bool CompletionAlarmStartedByPlatform { get; }
    
    /// <summary>
    /// Registers the sound and vibration services with the platform's background service,
    /// enabling it to trigger alarms directly (e.g., when the phone is locked and the
    /// UI dispatcher is not processing). No-op on platforms without a background service.
    /// </summary>
    void RegisterAlarmServices(
        ISoundService? soundService,
        IVibrationService? vibrationService,
        bool soundEnabled,
        bool vibrationEnabled);
    
    /// <summary>
    /// Updates the platform background service with the current alarm settings.
    /// No-op on platforms without a background service.
    /// </summary>
    void UpdateAlarmSettings(bool soundEnabled, bool vibrationEnabled);
}
