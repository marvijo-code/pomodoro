using SQLite;

namespace UnoPomodoro.Data.Models;

public class Session
{
    [PrimaryKey]
    public string Id { get; set; } = string.Empty;
    
    public DateTime StartTime { get; set; }
    
    public DateTime? EndTime { get; set; }
    
    public string Mode { get; set; } = string.Empty;
    
    /// <summary>
    /// User-assigned tag/label for the session (e.g., "Project Alpha", "Study", "Deep Work").
    /// </summary>
    public string Tag { get; set; } = string.Empty;
    
    /// <summary>
    /// User rating of the session quality (0 = not rated, 1-5 stars).
    /// Part of the session retrospective feature.
    /// </summary>
    public int Rating { get; set; }
    
    /// <summary>
    /// Free-text retrospective note written after session completion.
    /// </summary>
    public string RetroNote { get; set; } = string.Empty;
    
    /// <summary>
    /// Number of distractions/interruptions logged during the session.
    /// </summary>
    public int DistractionCount { get; set; }
    
    // Additional properties for computed stats
    [Ignore]
    public int TotalTasks { get; set; }
    
    [Ignore]
    public int CompletedTasks { get; set; }
    
    [Ignore]
    public List<TaskItem> Tasks { get; set; } = new List<TaskItem>();
    
    public Session()
    {
        // Parameterless constructor for SQLite
    }
    
    public Session(string id, string mode, DateTime startTime)
    {
        Id = id;
        Mode = mode;
        StartTime = startTime;
    }
}
