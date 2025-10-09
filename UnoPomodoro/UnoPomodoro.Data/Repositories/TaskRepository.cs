using SQLite;
using UnoPomodoro.Data.Models;

namespace UnoPomodoro.Data.Repositories
{
    public class TaskRepository : ITaskRepository
    {
        private readonly SQLiteConnection _connection;

        public TaskRepository(SQLiteConnection connection)
        {
            _connection = connection;
        }

        public Task<List<TaskItem>> GetBySession(string sessionId)
        {
            var tasks = _connection.Table<TaskItem>().Where(t => t.SessionId == sessionId).OrderBy(t => t.Id).ToList();
            return Task.FromResult(tasks);
        }

        public Task<TaskItem?> Add(string text, string sessionId)
        {
            var task = new TaskItem
            {
                Text = text,
                SessionId = sessionId,
                Completed = false,
                CompletedAt = null
            };

            _connection.Insert(task);
            return Task.FromResult<TaskItem?>(task);
        }

        public Task<TaskItem?> ToggleCompleted(int id, bool completed)
        {
            var task = _connection.Table<TaskItem>().FirstOrDefault(t => t.Id == id);
            if (task != null)
            {
                task.Completed = completed;
                task.CompletedAt = completed ? DateTime.Now : (DateTime?)null;
                _connection.Update(task);
            }
            return Task.FromResult(task);
        }

        public Task Delete(int id)
        {
            _connection.Delete<TaskItem>(id);
            return Task.CompletedTask;
        }

        public Task<List<TaskItem>> GetAllTasksAsync()
        {
            var tasks = _connection.Table<TaskItem>().OrderByDescending(t => t.Id).ToList();
            return Task.FromResult(tasks);
        }

        public Task<List<TaskItem>> GetTasksByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            var sessions = _connection.Table<Session>()
                .Where(s => s.StartTime >= startDate && s.StartTime <= endDate)
                .Select(s => s.Id)
                .ToList();

            var tasks = _connection.Table<TaskItem>()
                .Where(t => sessions.Contains(t.SessionId))
                .OrderByDescending(t => t.Id)
                .ToList();

            return Task.FromResult(tasks);
        }

        public Task<int> GetCompletedTasksCountAsync(string sessionId)
        {
            var count = _connection.Table<TaskItem>()
                .Count(t => t.SessionId == sessionId && t.Completed);
            return Task.FromResult(count);
        }

        public Task<int> GetTotalTasksCountAsync(string sessionId)
        {
            var count = _connection.Table<TaskItem>()
                .Count(t => t.SessionId == sessionId);
            return Task.FromResult(count);
        }

        public Task<TaskItem?> GetTaskByIdAsync(int id)
        {
            var task = _connection.Table<TaskItem>().FirstOrDefault(t => t.Id == id);
            return Task.FromResult<TaskItem?>(task);
        }

        public Task<bool> UpdateTaskAsync(TaskItem task)
        {
            try
            {
                _connection.Update(task);
                return Task.FromResult(true);
            }
            catch
            {
                return Task.FromResult(false);
            }
        }
    }
}
