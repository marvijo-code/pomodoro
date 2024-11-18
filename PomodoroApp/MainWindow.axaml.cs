using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PomodoroApp
{
    public class MainWindow : Window
    {
        private int timeLeft;
        private bool isRunning;
        private string mode;
        private List<TaskItem> tasks;
        private string newTask;
        private string sessionId;
        private List<Session> sessions;
        private bool showHistory;
        private string expandedSession;
        private Dictionary<string, List<TaskItem>> sessionTasks;
        private bool isSoundEnabled;
        private bool isRinging;

        private readonly Dictionary<string, int> times = new Dictionary<string, int>
        {
            { "pomodoro", 25 * 60 },
            { "shortBreak", 5 * 60 },
            { "longBreak", 15 * 60 }
        };

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;
            timeLeft = times["pomodoro"];
            mode = "pomodoro";
            tasks = new List<TaskItem>();
            newTask = string.Empty;
            sessionId = null;
            sessions = new List<Session>();
            showHistory = false;
            expandedSession = null;
            sessionTasks = new Dictionary<string, List<TaskItem>>();
            isSoundEnabled = true;
            isRinging = false;
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private string FormatTime(int seconds)
        {
            var mins = seconds / 60;
            var secs = seconds % 60;
            return $"{mins:D2}:{secs:D2}";
        }

        private void ResetTimer()
        {
            timeLeft = times[mode];
            isRunning = false;
        }

        private async void ToggleTimer()
        {
            if (!isRunning)
            {
                sessionId = DateTime.Now.Ticks.ToString();
                tasks.Clear();
                try
                {
                    await ApiService.CreateSession(sessionId, mode);
                    await FetchSessions();
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine("Error creating session: " + ex.Message);
                }
            }
            isRunning = !isRunning;
        }

        private async Task FetchSessions()
        {
            try
            {
                sessions = await ApiService.GetSessions();
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Error fetching sessions: " + ex.Message);
            }
        }

        private void ChangeMode(string newMode)
        {
            mode = newMode;
            timeLeft = times[newMode];
            isRunning = false;
        }

        private void StartRinging()
        {
            isRinging = true;
            var audio = new MediaPlayer();
            audio.Open(new Uri("notification.wav", UriKind.Relative));
            audio.IsLooping = true;
            audio.Play();
        }

        private void StopRinging()
        {
            isRinging = false;
        }

        private async void AddTask(string taskText)
        {
            if (string.IsNullOrWhiteSpace(taskText)) return;

            try
            {
                var taskItem = await ApiService.AddTask(taskText, sessionId);
                tasks.Add(taskItem);
                newTask = string.Empty;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Error adding task: " + ex.Message);
            }
        }

        private async void DeleteTask(string taskId)
        {
            try
            {
                await ApiService.DeleteTask(taskId);
                tasks = tasks.Where(t => t.Id != taskId).ToList();
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Error deleting task: " + ex.Message);
            }
        }

        private async void ToggleTask(string taskId, bool completed)
        {
            try
            {
                await ApiService.ToggleTask(taskId, completed);
                tasks = tasks.Select(t => t.Id == taskId ? new TaskItem { Id = t.Id, Text = t.Text, Completed = !t.Completed } : t).ToList();
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Error toggling task: " + ex.Message);
            }
        }

        private void TestSound()
        {
            if (isSoundEnabled)
            {
                var audio = new MediaPlayer();
                audio.Open(new Uri("notification.wav", UriKind.Relative));
                audio.Play();
            }
        }

        private async void FetchTasks()
        {
            if (sessionId == null) return;
            try
            {
                tasks = await ApiService.GetTasks(sessionId);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Error fetching tasks: " + ex.Message);
            }
        }

        private async void ViewSessionTasks(string sessionId)
        {
            if (expandedSession == sessionId)
            {
                expandedSession = null;
            }
            else
            {
                expandedSession = sessionId;
                try
                {
                    var sessionTasksList = await ApiService.GetTasks(sessionId);
                    sessionTasks[sessionId] = sessionTasksList;
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine("Error fetching session tasks: " + ex.Message);
                }
            }
        }

        private void OnLoaded(object sender, EventArgs e)
        {
            DispatcherTimer timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(1);
            timer.Tick += (s, ev) =>
            {
                if (isRunning && timeLeft > 0)
                {
                    timeLeft--;
                    if (timeLeft == 0 && isSoundEnabled)
                    {
                        StartRinging();
                    }
                }
            };
            timer.Start();
        }
    }

    public class TaskItem
    {
        public string Id { get; set; }
        public string Text { get; set; }
        public bool Completed { get; set; }
    }

    public class Session
    {
        public string Id { get; set; }
        public string Mode { get; set; }
        public DateTime StartTime { get; set; }
        public int CompletedTasks { get; set; }
        public int TotalTasks { get; set; }
    }

    public static class ApiService
    {
        private static readonly string ApiUrl = "http://your-api-url";

        public static async Task<Session> CreateSession(string sessionId, string mode)
        {
            // Implement API call to create session
            return new Session();
        }

        public static async Task<List<Session>> GetSessions()
        {
            // Implement API call to get sessions
            return new List<Session>();
        }

        public static async Task<TaskItem> AddTask(string taskText, string sessionId)
        {
            // Implement API call to add task
            return new TaskItem();
        }

        public static async Task DeleteTask(string taskId)
        {
            // Implement API call to delete task
        }

        public static async Task ToggleTask(string taskId, bool completed)
        {
            // Implement API call to toggle task
        }

        public static async Task<List<TaskItem>> GetTasks(string sessionId)
        {
            // Implement API call to get tasks
            return new List<TaskItem>();
        }
    }
}
