using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
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
    [NotifyPropertyChangedFor(nameof(CanChangeMode))]
    private bool _isRunning;

    public bool CanChangeMode => !IsRunning;

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
    private double _soundVolume = 100;

    [ObservableProperty]
    private int _soundDuration = 5;

    [ObservableProperty]
    private bool _isRinging;

    [ObservableProperty]
    private bool _showTasks = false;

    [ObservableProperty]
    private double _progressPercentage = 0;

    [ObservableProperty]
    private string _sessionInfo = "Ready to start";

    // Dashboard stats (inline)
    [ObservableProperty]
    private int _todaySessions;

    [ObservableProperty]
    private int _todayFocusMinutes;

    [ObservableProperty]
    private int _todayTasksCompleted;

    [ObservableProperty]
    private int _currentStreak;

    // Dashboard Properties
    [ObservableProperty]
    private bool _showDashboard = false;

    [ObservableProperty]
    private bool _showEditGoals = false;

    [ObservableProperty]
    private DateTime _selectedDate = DateTime.Today;

    [ObservableProperty]
    private TimeSpan _totalFocusTime;

    [ObservableProperty]
    private int _completedSessions;

    [ObservableProperty]
    private int _completedTasks;

    [ObservableProperty]
    private double _productivityScore;

    [ObservableProperty]
    private int _dailyGoal;

    [ObservableProperty]
    private int _weeklyGoal;

    [ObservableProperty]
    private int _monthlyGoal;

    [ObservableProperty]
    private int _longestStreak;

    [ObservableProperty]
    private string _mostProductiveTime = "Morning";

    [ObservableProperty]
    private double _weeklyAverage;

    [ObservableProperty]
    private double _monthlyAverage;

    public ObservableCollection<DailyStats> DailyStats { get; } = new();
    public ObservableCollection<Session> RecentSessions { get; } = new();
    public ObservableCollection<Achievement> Achievements { get; } = new();
    public ObservableCollection<CategoryStats> CategoryStats { get; } = new();

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
        
        // Load dashboard stats
        _ = LoadDashboardStatsAsync();

        // Initialize sound settings
        _soundService.Volume = SoundVolume / 100.0;
        _soundService.Duration = SoundDuration;
    }

    partial void OnSoundVolumeChanged(double value)
    {
        if (_soundService != null)
        {
            _soundService.Volume = value / 100.0;
        }
    }

    partial void OnSoundDurationChanged(int value)
    {
        if (_soundService != null)
        {
            _soundService.Duration = value;
        }
    }

    private async Task LoadDashboardStatsAsync()
    {
        try
        {
            var dailyStats = await _statisticsService.GetDailyStatsAsync();
            var todayStats = dailyStats.FirstOrDefault(s => s.Date.Date == DateTime.Today);
            
            if (todayStats != null)
            {
                TodaySessions = todayStats.SessionsCompleted;
                TodayFocusMinutes = todayStats.TotalMinutes;
                TodayTasksCompleted = todayStats.TasksCompleted;
            }
            else
            {
                TodaySessions = 0;
                TodayFocusMinutes = 0;
                TodayTasksCompleted = 0;
            }

            var streaks = await _statisticsService.GetStreaksAsync();
            CurrentStreak = streaks.CurrentStreak;
        }
        catch
        {
            // If stats fail to load, just use defaults
            TodaySessions = 0;
            TodayFocusMinutes = 0;
            TodayTasksCompleted = 0;
            CurrentStreak = 0;
        }
    }

    private void OnTimerTick(object? sender, int remainingSeconds)
    {
        TimeLeft = remainingSeconds;
        UpdateProgressPercentage();
        UpdateSessionInfo();
        OnPropertyChanged(nameof(CanAddMinute));
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

        // Refresh dashboard stats
        await LoadDashboardStatsAsync();

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
        if (IsRunning) return;

        Mode = newMode;
        TimeLeft = _times[Mode];
        IsRunning = false;
        _timerService.Reset(TimeLeft);
        UpdateProgressPercentage();
        UpdateSessionInfo();
    }

    [RelayCommand]
    private void AddOneMinute()
    {
        TimeLeft += 60;
        _timerService.Reset(TimeLeft);
        if (IsRunning)
        {
            _timerService.Start(TimeLeft);
        }
        UpdateProgressPercentage();
        UpdateSessionInfo();
    }

    public bool CanAddMinute => TimeLeft < 180; // Less than 3 minutes

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
        
        // Refresh dashboard stats
        await LoadDashboardStatsAsync();
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

    [RelayCommand]
    private async Task ToggleDashboard()
    {
        ShowDashboard = !ShowDashboard;
        if (ShowDashboard)
        {
            await LoadDashboardData();
        }
    }

    private async Task LoadDashboardData()
    {
        await LoadDailyStats();
        await LoadRecentSessions();
        await LoadAchievements();
        await LoadCategoryStats();
        await LoadGoals();
        await LoadStreaks();
        await LoadAverages();
    }

    [RelayCommand]
    private async Task LoadDailyStats()
    {
        var stats = await _statisticsService.GetDailyStatsAsync();
        DailyStats.Clear();
        foreach (var stat in stats)
        {
            DailyStats.Add(stat);
        }

        var selectedStat = stats.FirstOrDefault(s => s.Date.Date == SelectedDate.Date);

        if (selectedStat != null)
        {
            TotalFocusTime = TimeSpan.FromMinutes(selectedStat.TotalMinutes);
            CompletedSessions = selectedStat.SessionsCompleted;
            CompletedTasks = selectedStat.TasksCompleted;
            ProductivityScore = selectedStat.ProductivityScore;
        }
        else
        {
            TotalFocusTime = TimeSpan.Zero;
            CompletedSessions = 0;
            CompletedTasks = 0;
            ProductivityScore = 0;
        }
    }

    [RelayCommand]
    private async Task LoadRecentSessions()
    {
        var sessions = await _sessionRepository.GetRecentSessionsAsync(10);
        RecentSessions.Clear();
        foreach (var session in sessions)
        {
            RecentSessions.Add(session);
        }
    }

    [RelayCommand]
    private async Task LoadAchievements()
    {
        var achievements = await _statisticsService.GetAchievementsAsync();
        Achievements.Clear();
        foreach (var achievement in achievements)
        {
            Achievements.Add(achievement);
        }
    }

    [RelayCommand]
    private async Task LoadCategoryStats()
    {
        var stats = await _statisticsService.GetCategoryStatsAsync();
        CategoryStats.Clear();
        foreach (var stat in stats)
        {
            CategoryStats.Add(stat);
        }
    }

    [RelayCommand]
    private async Task LoadGoals()
    {
        var goals = await _statisticsService.GetGoalsAsync();
        DailyGoal = goals.DailyGoal;
        WeeklyGoal = goals.WeeklyGoal;
        MonthlyGoal = goals.MonthlyGoal;
    }

    [RelayCommand]
    private async Task LoadStreaks()
    {
        var streaks = await _statisticsService.GetStreaksAsync();
        CurrentStreak = streaks.CurrentStreak;
        LongestStreak = streaks.LongestStreak;
        MostProductiveTime = streaks.MostProductiveTime;
    }

    [RelayCommand]
    private async Task LoadAverages()
    {
        var averages = await _statisticsService.GetAveragesAsync();
        WeeklyAverage = averages.WeeklyAverage;
        MonthlyAverage = averages.MonthlyAverage;
    }

    [RelayCommand]
    private async Task ExportReport(string format)
    {
        if (Enum.TryParse<ReportFormat>(format, true, out var reportFormat))
        {
            await _statisticsService.ExportReportAsync(reportFormat);
        }
    }

    [RelayCommand]
    private async Task UpdateGoals()
    {
        await _statisticsService.UpdateGoalsAsync(DailyGoal, WeeklyGoal, MonthlyGoal);
        ShowEditGoals = false;
        await LoadGoals(); // Refresh to ensure UI is in sync
    }

    [RelayCommand]
    private void ToggleEditGoals()
    {
        ShowEditGoals = !ShowEditGoals;
    }

    [RelayCommand]
    private void ChangeDate(int daysOffset)
    {
        SelectedDate = SelectedDate.AddDays(daysOffset);
        _ = LoadDashboardData();
    }
}