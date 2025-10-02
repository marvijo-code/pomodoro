using SQLite;

namespace UnoPomodoro.Data.Models;

public class Session
{
    [PrimaryKey]
    public string Id { get; set; } = string.Empty;
    
    public DateTime StartTime { get; set; }
    
    public DateTime? EndTime { get; set; }
    
    public string Mode { get; set; } = string.Empty;
    
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
