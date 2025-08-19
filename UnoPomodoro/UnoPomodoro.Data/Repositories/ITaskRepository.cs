using UnoPomodoro.Data.Models;

namespace UnoPomodoro.Data.Repositories;

public interface ITaskRepository
{
    Task<List<TaskItem>> GetBySession(string sessionId);
    Task<TaskItem?> Add(string text, string sessionId);
    Task<TaskItem?> ToggleCompleted(int id, bool completed);
    Task Delete(int id);
}
