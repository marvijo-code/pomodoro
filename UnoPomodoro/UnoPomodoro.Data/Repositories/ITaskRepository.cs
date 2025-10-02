using UnoPomodoro.Data.Models;

namespace UnoPomodoro.Data.Repositories;

public interface ITaskRepository
{
    Task<List<TaskItem>> GetBySession(string sessionId);
    Task<TaskItem?> Add(string text, string sessionId);
    Task<TaskItem?> ToggleCompleted(int id, bool completed);
    Task Delete(int id);
    Task<List<TaskItem>> GetAllTasksAsync();
    Task<List<TaskItem>> GetTasksByDateRangeAsync(DateTime startDate, DateTime endDate);
    Task<int> GetCompletedTasksCountAsync(string sessionId);
    Task<int> GetTotalTasksCountAsync(string sessionId);
    Task<TaskItem?> GetTaskByIdAsync(int id);
    Task<bool> UpdateTaskAsync(TaskItem task);
}
