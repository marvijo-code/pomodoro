namespace UnoPomodoro.Services;

public sealed class AppRuntimeState
{
    public string Mode { get; set; } = "pomodoro";
    public int TimeLeftSeconds { get; set; }
    public bool IsRunning { get; set; }
    public long TargetEndUtcTicks { get; set; }
    public int TotalDurationSeconds { get; set; }
    public string? SessionId { get; set; }
    public int PomodoroCount { get; set; }
    public bool CompletionPending { get; set; }
    public string PendingNextMode { get; set; } = "shortBreak";
    public string CompletionTitle { get; set; } = string.Empty;
    public string CompletionMessage { get; set; } = string.Empty;
    public string NextActionLabel { get; set; } = string.Empty;
    public string SessionNotes { get; set; } = string.Empty;
    public int DistractionCount { get; set; }
    public string SessionTag { get; set; } = string.Empty;
    public int SessionRating { get; set; }
    public string RetroNote { get; set; } = string.Empty;
    public bool ShowRetroPrompt { get; set; }
    public string BreakSuggestion { get; set; } = string.Empty;
    public bool ShowTasks { get; set; }
    public bool ShowExtraToolPanel { get; set; }
    public string ActiveExtraTool { get; set; } = string.Empty;
}
