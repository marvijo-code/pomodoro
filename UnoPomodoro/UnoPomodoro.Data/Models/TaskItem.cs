using SQLite;

namespace UnoPomodoro.Data.Models;

/// <summary>
/// Priority levels for task items, ordered so that higher priority = lower numeric value
/// (for natural ascending sort).
/// </summary>
public enum TaskPriority
{
    High = 0,
    Medium = 1,
    Low = 2,
    None = 3
}

public enum TaskWorkStatus
{
    Todo = 0,
    InProgress = 1,
    Done = 2
}

public class TaskItem
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }
    
    public string Text { get; set; } = string.Empty;
    
    public bool Completed { get; set; }
    
    public string SessionId { get; set; } = string.Empty;
    
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// Work state of the task. TODO and InProgress are non-completed states.
    /// Done aligns with Completed = true.
    /// </summary>
    public TaskWorkStatus Status { get; set; } = TaskWorkStatus.Todo;

    /// <summary>
    /// Total accumulated tracked work time for this task.
    /// </summary>
    public int TrackedSeconds { get; set; }

    /// <summary>
    /// UTC ticks when active time monitoring started for this task.
    /// Null means not currently being monitored.
    /// </summary>
    public long? TrackingStartedAtUtcTicks { get; set; }
    
    /// <summary>
    /// Task priority level (High, Medium, Low, None). Used for sorting tasks.
    /// </summary>
    public TaskPriority Priority { get; set; } = TaskPriority.None;
    
    /// <summary>
    /// Estimated number of pomodoros to complete this task (0 = no estimate).
    /// Part of the time estimation vs actual tracking feature.
    /// </summary>
    public int EstimatedPomodoros { get; set; }
    
    /// <summary>
    /// Actual number of pomodoros spent on this task.
    /// Incremented when a pomodoro completes while this task is in the session.
    /// </summary>
    public int ActualPomodoros { get; set; }
    
    /// <summary>
    /// Sort order for manual reordering within a session.
    /// </summary>
    public int SortOrder { get; set; }
    
    public TaskItem()
    {
        // Parameterless constructor for SQLite
    }
    
    public TaskItem(string text, string sessionId)
    {
        Text = text;
        SessionId = sessionId;
    }
}
