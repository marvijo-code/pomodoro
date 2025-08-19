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
    }
}
