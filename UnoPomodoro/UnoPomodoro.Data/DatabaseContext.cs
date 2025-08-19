using SQLite;
using UnoPomodoro.Data.Models;

namespace UnoPomodoro.Data
{
    public class DatabaseContext
    {
        private SQLiteConnection? _connection;
        private readonly string _databasePath;

        public DatabaseContext(string databasePath)
        {
            _databasePath = databasePath;
        }

        public void Initialize()
        {
            _connection = new SQLiteConnection(_databasePath);
            _connection.CreateTable<Session>();
            _connection.CreateTable<TaskItem>();
        }

        public SQLiteConnection GetConnection()
        {
            if (_connection == null)
            {
                Initialize();
            }
            return _connection!;
        }
    }
}
