using SQLite;

namespace UnoPomodoro.Data.Models;

public class TaskItem
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }
    
    public string Text { get; set; } = string.Empty;
    
    public bool Completed { get; set; }
    
    public string SessionId { get; set; } = string.Empty;
    
    public DateTime? CompletedAt { get; set; }
    
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
