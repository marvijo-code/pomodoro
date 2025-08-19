using System.IO;

namespace UnoPomodoro.Data.Services
{
    public class DatabaseInitializer
    {
        private readonly string _databasePath;
        
        public DatabaseInitializer(string databasePath)
        {
            _databasePath = databasePath;
        }
        
        public void EnsureDatabaseCreated()
        {
            // Create directory if it doesn't exist
            var directory = Path.GetDirectoryName(_databasePath);
            if (directory != null && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            
            // Database will be created when DatabaseContext.Initialize() is called
        }
    }
}
