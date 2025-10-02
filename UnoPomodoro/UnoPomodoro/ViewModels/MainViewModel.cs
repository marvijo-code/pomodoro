using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using UnoPomodoro.Data.Models;
using UnoPomodoro.Data.Repositories;
using UnoPomodoro.Services;
using System.Diagnostics;
using System.Threading.Tasks;

namespace UnoPomodoro.ViewModels;

public partial class MainViewModel : ObservableObject
    {
        private readonly TimerService _timerService;
        private readonly ISessionRepository _sessionRepository;
        private readonly ITaskRepository _taskRepository;
        private readonly SoundService _soundService;
        private readonly NotificationService _notificationService;
        
        [ObservableProperty]
        private int _timeLeft;
        
        [ObservableProperty]
        private bool _isRunning;
        
        [ObservableProperty]
        private string _mode = "pomodoro";
        
        [ObservableProperty]
        private string _newTask = "";
        
        [ObservableProperty]
        private string? _sessionId;
        
        [ObservableProperty]
        private bool _showHistory;
        
        [ObservableProperty]
        private string? _expandedSession;
        
        [ObservableProperty]
        private bool _isSoundEnabled = true;
        
        [ObservableProperty]
        private bool _isRinging;
        
        private readonly Dictionary<string, int> _times = new Dictionary<string, int>
        {
            { "pomodoro", 25 * 60 },
            { "shortBreak", 5 * 60 },
            { "longBreak", 15 * 60 }
        };
        
        public ObservableCollection<TaskItem> Tasks { get; } = new ObservableCollection<TaskItem>();
        public ObservableCollection<Session> Sessions { get; } = new ObservableCollection<Session>();
        
        public MainViewModel(
            TimerService timerService, 
            ISessionRepository sessionRepository, 
            ITaskRepository taskRepository,
            SoundService soundService,
            NotificationService notificationService)
        {
            _timerService = timerService;
            _sessionRepository = sessionRepository;
            _taskRepository = taskRepository;
            _soundService = soundService;
            _notificationService = notificationService;
            
            _timerService.Tick += OnTimerTick;
            _timerService.TimerCompleted += OnTimerCompleted;
            
            TimeLeft = _times[Mode];
            SessionId = null;
        }
        
        private void OnTimerTick(object? sender, int remainingSeconds)
        {
            TimeLeft = remainingSeconds;
        }
        
        private async void OnTimerCompleted(object? sender, EventArgs e)
        {
            IsRunning = false;
            if (IsSoundEnabled)
            {
                IsRinging = true;
                _soundService?.PlayNotificationSound();
            }
            
            await Task.Run(async () => {
                await _notificationService.ShowNotificationAsync("Pomodoro Completed", "Your timer has finished!");
            });
        }
        
        [RelayCommand]
        private void ToggleTimer()
        {
            if (!IsRunning)
            {
                // Start a new session if not already running
                if (string.IsNullOrEmpty(SessionId))
                {
                    SessionId = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();
                    Tasks.Clear();
                    
                    // Create session in database
                    _ = _sessionRepository.CreateSession(SessionId, Mode, DateTime.Now);
                    _ = LoadSessions();
                }
                
                _timerService.Start(TimeLeft);
                IsRunning = true;
            }
            else
            {
                _timerService.Pause();
                IsRunning = false;
            }
        }
        
        [RelayCommand]
        private void ResetTimer()
        {
            _timerService.Reset(_times[Mode]);
            TimeLeft = _times[Mode];
            IsRunning = false;
        }
        
        [RelayCommand]
        private void ChangeMode(string newMode)
        {
            Mode = newMode;
            TimeLeft = _times[Mode];
            IsRunning = false;
            _timerService.Reset(TimeLeft);
        }
        
        [RelayCommand]
        private async Task AddTask()
        {
            if (string.IsNullOrWhiteSpace(NewTask) || string.IsNullOrEmpty(SessionId))
                return;
                
            var task = await _taskRepository.Add(NewTask, SessionId);
            if (task != null)
            {
                Tasks.Add(task);
            }
            NewTask = "";
        }
        
        [RelayCommand]
        private async Task DeleteTask(int taskId)
        {
            await _taskRepository.Delete(taskId);
            var task = Tasks.FirstOrDefault(t => t.Id == taskId);
            if (task != null)
            {
                Tasks.Remove(task);
            }
        }
        
        [RelayCommand]
        private async Task ToggleTask(int taskId)
        {
            var task = Tasks.FirstOrDefault(t => t.Id == taskId);
            if (task != null)
            {
                var completed = !task.Completed;
                await _taskRepository.ToggleCompleted(taskId, completed);
                task.Completed = completed;
                task.CompletedAt = completed ? DateTime.Now : (DateTime?)null;
            }
        }
        
        [RelayCommand]
        private async Task LoadSessions()
        {
            var sessions = await _sessionRepository.GetSessionsWithStats();
            Sessions.Clear();
            foreach (var session in sessions)
            {
                Sessions.Add(session);
            }
        }
        
        [RelayCommand]
        private void ToggleHistory()
        {
            ShowHistory = !ShowHistory;
        }
        
        [RelayCommand]
        private void ToggleSessionDetails(string sessionId)
        {
            ExpandedSession = ExpandedSession == sessionId ? null : sessionId;
        }
        
        [RelayCommand]
        private void ToggleSound()
        {
            IsSoundEnabled = !IsSoundEnabled;
        }
        
        [RelayCommand]
        private void StopAlarm()
        {
            IsRinging = false;
            _soundService?.StopNotificationSound();
        }
        
        public string FormatTime(int seconds)
        {
            var mins = seconds / 60;
            var secs = seconds % 60;
            return $"{mins:D2}:{secs:D2}";
        }
    }
