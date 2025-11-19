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
    private readonly ITimerService _timerService;
    private readonly ISessionRepository _sessionRepository;
    private readonly ITaskRepository _taskRepository;
    private readonly ISoundService _soundService;
    private readonly INotificationService _notificationService;
    private readonly IStatisticsService _statisticsService;

    [ObservableProperty]
    private int _timeLeft;

    [ObservableProperty]
    private bool _isRunning;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TotalDuration))]
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

    [ObservableProperty]
    private bool _showTasks = false;

    [ObservableProperty]
    private double _progressPercentage = 0;

    [ObservableProperty]
    private string _sessionInfo = "Ready to start";

    private readonly Dictionary<string, int> _times = new Dictionary<string, int>
    {
        { "pomodoro", 25 * 60 },
        { "shortBreak", 5 * 60 },
        { "longBreak", 15 * 60 }
    };

    public int TotalDuration => _times.TryGetValue(Mode, out var duration) ? duration : 25 * 60;

    public ObservableCollection<TaskItem> Tasks { get; } = new ObservableCollection<TaskItem>();
    public ObservableCollection<Session> Sessions { get; } = new ObservableCollection<Session>();

    // Public access to services for navigation
    public ISessionRepository SessionRepository => _sessionRepository;
    public ITaskRepository TaskRepository => _taskRepository;
    public IStatisticsService StatisticsService => _statisticsService;

    public MainViewModel(
        ITimerService timerService,
        ISessionRepository sessionRepository,
        ITaskRepository taskRepository,
        ISoundService soundService,
        INotificationService notificationService,
        IStatisticsService statisticsService)
    {
        _timerService = timerService;
        _sessionRepository = sessionRepository;
        _taskRepository = taskRepository;
        _soundService = soundService;
        _notificationService = notificationService;
        _statisticsService = statisticsService;

        _timerService.Tick += OnTimerTick;
        _timerService.TimerCompleted += OnTimerCompleted;

        TimeLeft = _times[Mode];
        SessionId = null;
        UpdateProgressPercentage();
        UpdateSessionInfo();
    }

    private void OnTimerTick(object? sender, int remainingSeconds)
    {
        TimeLeft = remainingSeconds;
        UpdateProgressPercentage();
        UpdateSessionInfo();
    }

    private async void OnTimerCompleted(object? sender, EventArgs e)
    {
        IsRunning = false;
        if (IsSoundEnabled)
        {
            IsRinging = true;
            _soundService?.PlayNotificationSound();
        }

        // Auto-advance to next session type
        await AutoAdvanceSession();

        await _notificationService.ShowNotificationAsync("Pomodoro Completed", "Your timer has finished!");
    }

    private async Task AutoAdvanceSession()
    {
        // End current session
        if (!string.IsNullOrEmpty(SessionId))
        {
            await _sessionRepository.EndSession(SessionId, DateTime.Now);
        }

        // Determine next mode
        string nextMode = Mode switch
        {
            "pomodoro" => "shortBreak",
            "shortBreak" => "pomodoro",
            "longBreak" => "pomodoro",
            _ => "pomodoro"
        };

        // Start new session with new mode
        SessionId = null;
        Tasks.Clear();

        ChangeMode(nextMode);
        UpdateSessionInfo();
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
            UpdateSessionInfo();
        }
        else
        {
            _timerService.Pause();
            IsRunning = false;
            UpdateSessionInfo();
        }
    }

    [RelayCommand]
    private void ResetTimer()
    {
        _timerService.Reset(_times[Mode]);
        TimeLeft = _times[Mode];
        IsRunning = false;
        UpdateProgressPercentage();
        UpdateSessionInfo();
    }

    [RelayCommand]
    private async Task SkipSession()
    {
        // Skip current session and move to next
        await AutoAdvanceSession();
    }

    [RelayCommand]
    private void ChangeMode(string newMode)
    {
        Mode = newMode;
        TimeLeft = _times[Mode];
        IsRunning = false;
        _timerService.Reset(TimeLeft);
        UpdateProgressPercentage();
        UpdateSessionInfo();
    }

    [RelayCommand]
    private async Task AddTask()
    {
        if (string.IsNullOrWhiteSpace(NewTask))
            return;

        if (string.IsNullOrEmpty(SessionId))
        {
            SessionId = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();
            await _sessionRepository.CreateSession(SessionId, Mode, DateTime.Now);
            await LoadSessions();
        }

        var task = await _taskRepository.Add(NewTask, SessionId);
        if (task != null)
        {
            Tasks.Add(task);
            UpdateSessionInfo();
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
            UpdateSessionInfo();
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
            UpdateSessionInfo();
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

    [RelayCommand]
    private void ToggleTasks()
    {
        ShowTasks = !ShowTasks;
    }

    [RelayCommand]
    private async Task StartNewSession()
    {
        // End current session if running
        if (!string.IsNullOrEmpty(SessionId))
        {
            await _sessionRepository.EndSession(SessionId, DateTime.Now);
        }

        // Start new session
        SessionId = null;
        Tasks.Clear();

        ResetTimer();
        UpdateSessionInfo();
    }

    [RelayCommand]
    private void TestSound()
    {
        _soundService?.PlayNotificationSound();
    }

    private void UpdateProgressPercentage()
    {
        var totalTime = _times[Mode];
        var remaining = TimeLeft;
        ProgressPercentage = ((double)(totalTime - remaining) / totalTime) * 100;
    }

    private void UpdateSessionInfo()
    {
        if (string.IsNullOrEmpty(SessionId))
        {
            SessionInfo = "Ready to start";
        }
        else
        {
            var totalTasks = Tasks.Count;
            var completedTasks = Tasks.Count(t => t.Completed);

            if (totalTasks == 0)
            {
                SessionInfo = "0/0 ✅";
            }
            else if (IsRunning)
            {
                SessionInfo = $"{completedTasks}/{totalTasks} tasks completed";
            }
            else
            {
                SessionInfo = $"{completedTasks}/{totalTasks} tasks ready";
            }
        }
    }

    public string FormatTime(int seconds)
    {
        var mins = seconds / 60;
        var secs = seconds % 60;
        return $"{mins:D2}:{secs:D2}";
    }
}