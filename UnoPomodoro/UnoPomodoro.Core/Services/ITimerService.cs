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
}
