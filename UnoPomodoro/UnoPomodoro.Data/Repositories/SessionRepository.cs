using SQLite;
using UnoPomodoro.Data.Models;

namespace UnoPomodoro.Data.Repositories
{
    public class SessionRepository : ISessionRepository
    {
        private readonly SQLiteConnection _connection;

        public SessionRepository(SQLiteConnection connection)
        {
            _connection = connection;
        }

        public Task<Session> CreateSession(string sessionId, string mode, DateTime startTime)
        {
            var session = new Session
            {
                Id = sessionId,
                Mode = mode,
                StartTime = startTime,
                EndTime = null
            };

            _connection.Insert(session);
            return Task.FromResult(session);
        }

        public Task<Session?> CloseSession(string sessionId, DateTime endTime)
        {
            var session = _connection.Table<Session>().FirstOrDefault(s => s.Id == sessionId);
            if (session != null)
            {
                session.EndTime = endTime;
                _connection.Update(session);
            }
            return Task.FromResult<Session?>(session);
        }

        public Task<Session?> GetSessionById(string sessionId)
        {
            var session = _connection.Table<Session>().FirstOrDefault(s => s.Id == sessionId);
            return Task.FromResult<Session?>(session);
        }

        public Task<List<Session>> GetSessionsWithStats()
        {
            // Get all sessions ordered by start time descending
            var sessions = _connection.Table<Session>().OrderByDescending(s => s.StartTime).ToList();
            
            // For each session, get task stats
            foreach (var session in sessions)
            {
                var tasks = _connection.Table<TaskItem>().Where(t => t.SessionId == session.Id).ToList();
                session.TotalTasks = tasks.Count;
                session.CompletedTasks = tasks.Count(t => t.Completed);
                session.Tasks = tasks;
            }
            
            return Task.FromResult(sessions);
        }
    }
}
