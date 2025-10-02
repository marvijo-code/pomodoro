using UnoPomodoro.Data.Models;

namespace UnoPomodoro.Data.Repositories
{
    public interface ISessionRepository
    {
        Task<Session> CreateSession(string sessionId, string mode, DateTime startTime);
        Task<List<Session>> GetSessionsWithStats();
        Task<Session?> CloseSession(string sessionId, DateTime endTime);
        Task<Session?> GetSessionById(string sessionId);
        Task<List<Session>> GetAllSessionsAsync();
        Task<List<Session>> GetRecentSessionsAsync(int count);
        Task<bool> EndSession(string sessionId, DateTime endTime);
        Task<int> DeleteSessionAsync(string sessionId);
    }
}
