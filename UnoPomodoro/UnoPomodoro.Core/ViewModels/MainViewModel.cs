using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using UnoPomodoro.Data.Models;
using UnoPomodoro.Data.Repositories;
using UnoPomodoro.Services;

namespace UnoPomodoro.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly ITimerService _timerService;
    private readonly ISessionRepository _sessionRepository;
    private readonly ITaskRepository _taskRepository;
    private readonly ISoundService _soundService;
    private readonly INotificationService _notificationService;
    private readonly IStatisticsService _statisticsService;
    private readonly IVibrationService _vibrationService;
    private readonly ISettingsService _settingsService;

    // ── Timer state ──────────────────────────────────────────────

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
    private double _progressPercentage = 0;

    [ObservableProperty]
    private string _sessionInfo = "Ready to start";

    [ObservableProperty]
    private string? _sessionId;

    [ObservableProperty]
    private bool _isRinging;

    // ── Task management ──────────────────────────────────────────

    [ObservableProperty]
    private string _newTask = "";

    public ObservableCollection<TaskItem> Tasks { get; } = new();

    // ── Session history ──────────────────────────────────────────

    [ObservableProperty]
    private bool _showHistory;

    [ObservableProperty]
    private string? _expandedSession;

    public ObservableCollection<Session> Sessions { get; } = new();

    // ── Session notes (in-memory) ────────────────────────────────

    [ObservableProperty]
    private string _sessionNotes = "";

    // ── Pomodoro cycle tracking ──────────────────────────────────

    [ObservableProperty]
    private int _pomodoroCount;

    // ── Sound settings ───────────────────────────────────────────

    [ObservableProperty]
    private bool _isSoundEnabled = true;

    [ObservableProperty]
    private double _soundVolume = 100;

    [ObservableProperty]
    private int _soundDuration = 5;

    // ── Vibration settings ───────────────────────────────────────

    [ObservableProperty]
    private bool _isVibrationEnabled;

    // ── Notification settings ────────────────────────────────────

    [ObservableProperty]
    private bool _isNotificationEnabled = true;

    // ── Timer duration settings (minutes) ────────────────────────

    [ObservableProperty]
    private int _pomodoroDuration = 25;

    [ObservableProperty]
    private int _shortBreakDuration = 5;

    [ObservableProperty]
    private int _longBreakDuration = 15;

    [ObservableProperty]
    private int _pomodorosBeforeLongBreak = 4;

    // ── Auto-start settings ──────────────────────────────────────

    [ObservableProperty]
    private bool _autoStartBreaks;

    [ObservableProperty]
    private bool _autoStartPomodoros;

    // ── Display settings ─────────────────────────────────────────

    [ObservableProperty]
    private bool _keepScreenAwake;

    // ── Daily reminder settings ──────────────────────────────────

    [ObservableProperty]
    private bool _dailyReminderEnabled;

    [ObservableProperty]
    private int _dailyReminderHour = 9;

    [ObservableProperty]
    private int _dailyReminderMinute = 0;

    // ── Productivity features ────────────────────────────────────

    [ObservableProperty]
    private bool _isMidpointReminderEnabled;

    [ObservableProperty]
    private bool _isLastMinuteAlertEnabled = true;

    [ObservableProperty]
    private int _autoStartDelaySeconds = 2;

    [ObservableProperty]
    private bool _carryIncompleteTasksToNextSession;

    [ObservableProperty]
    private bool _autoOpenTasksOnSessionStart;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SessionGoalProgressText))]
    private int _sessionTaskGoal;

    // ── Overlay visibility ───────────────────────────────────────

    [ObservableProperty]
    private bool _showTasks = false;

    [ObservableProperty]
    private bool _showDashboard = false;

    [ObservableProperty]
    private bool _showSettings = false;

    [ObservableProperty]
    private bool _showEditGoals = false;

    // ── Dashboard inline stats ───────────────────────────────────

    [ObservableProperty]
    private int _todaySessions;

    [ObservableProperty]
    private int _todayFocusMinutes;

    [ObservableProperty]
    private int _todayTasksCompleted;

    [ObservableProperty]
    private int _currentStreak;

    // ── Dashboard detail stats ───────────────────────────────────

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

    // ── Computed properties ──────────────────────────────────────

    private Dictionary<string, int> _times = new()
    {
        { "pomodoro", 25 * 60 },
        { "shortBreak", 5 * 60 },
        { "longBreak", 15 * 60 }
    };
    private readonly List<string> _pendingCarryOverTaskTexts = new();
    private bool _midpointReminderTriggered;
    private bool _lastMinuteAlertTriggered;

    public int TotalDuration => _times.TryGetValue(Mode, out var duration) ? duration : 25 * 60;

    public bool CanAddMinute => TimeLeft < 180; // Less than 3 minutes
    public string SessionGoalProgressText =>
        SessionTaskGoal <= 0
            ? "Session goal is off"
            : $"{Tasks.Count(t => t.Completed)}/{SessionTaskGoal} goal tasks";

    // ── Public service access for navigation ─────────────────────

    public ISessionRepository SessionRepository => _sessionRepository;
    public ITaskRepository TaskRepository => _taskRepository;
    public IStatisticsService StatisticsService => _statisticsService;

    // ═════════════════════════════════════════════════════════════
    // Constructor
    // ═════════════════════════════════════════════════════════════

    public MainViewModel(
        ITimerService timerService,
        ISessionRepository sessionRepository,
        ITaskRepository taskRepository,
        ISoundService soundService,
        INotificationService notificationService,
        IStatisticsService statisticsService,
        IVibrationService vibrationService,
        ISettingsService settingsService)
    {
        _timerService = timerService;
        _sessionRepository = sessionRepository;
        _taskRepository = taskRepository;
        _soundService = soundService;
        _notificationService = notificationService;
        _statisticsService = statisticsService;
        _vibrationService = vibrationService;
        _settingsService = settingsService;

        _timerService.Tick += OnTimerTick;
        _timerService.TimerCompleted += OnTimerCompleted;

        // Load settings then initialize timer
        _ = InitializeFromSettingsAsync();
    }

    private async Task InitializeFromSettingsAsync()
    {
        await _settingsService.LoadAsync();
        ApplySettingsToViewModel();
        RebuildTimeDictionary();

        TimeLeft = _times[Mode];
        SessionId = null;
        UpdateProgressPercentage();
        UpdateSessionInfo();

        // Initialize sound service from settings
        _soundService.Volume = SoundVolume / 100.0;
        _soundService.Duration = SoundDuration;

        // Load dashboard stats
        await LoadDashboardStatsAsync();
    }

    /// <summary>
    /// Copies all persisted settings from ISettingsService into the ViewModel's
    /// observable properties so the UI reflects stored values.
    /// </summary>
    private void ApplySettingsToViewModel()
    {
        IsSoundEnabled = _settingsService.IsSoundEnabled;
        SoundVolume = _settingsService.SoundVolume;
        SoundDuration = _settingsService.SoundDuration;
        IsVibrationEnabled = _settingsService.IsVibrationEnabled;
        PomodoroDuration = _settingsService.PomodoroDuration;
        ShortBreakDuration = _settingsService.ShortBreakDuration;
        LongBreakDuration = _settingsService.LongBreakDuration;
        PomodorosBeforeLongBreak = _settingsService.PomodorosBeforeLongBreak;
        AutoStartBreaks = _settingsService.AutoStartBreaks;
        AutoStartPomodoros = _settingsService.AutoStartPomodoros;
        KeepScreenAwake = _settingsService.KeepScreenAwake;
        IsNotificationEnabled = _settingsService.IsNotificationEnabled;
        DailyReminderEnabled = _settingsService.IsDailyReminderEnabled;
        DailyReminderHour = _settingsService.DailyReminderHour;
        DailyReminderMinute = _settingsService.DailyReminderMinute;
        IsMidpointReminderEnabled = _settingsService.IsMidpointReminderEnabled;
        IsLastMinuteAlertEnabled = _settingsService.IsLastMinuteAlertEnabled;
        AutoStartDelaySeconds = _settingsService.AutoStartDelaySeconds;
        CarryIncompleteTasksToNextSession = _settingsService.CarryIncompleteTasksToNextSession;
        AutoOpenTasksOnSessionStart = _settingsService.AutoOpenTasksOnSessionStart;
        SessionTaskGoal = _settingsService.SessionTaskGoal;
        DailyGoal = _settingsService.DailyGoal;
        WeeklyGoal = _settingsService.WeeklyGoal;
        MonthlyGoal = _settingsService.MonthlyGoal;
    }

    /// <summary>
    /// Rebuilds the _times dictionary from the current duration settings.
    /// </summary>
    private void RebuildTimeDictionary()
    {
        _times["pomodoro"] = PomodoroDuration * 60;
        _times["shortBreak"] = ShortBreakDuration * 60;
        _times["longBreak"] = LongBreakDuration * 60;
        OnPropertyChanged(nameof(TotalDuration));
    }

    /// <summary>
    /// Persists a single setting change: updates the ISettingsService property,
    /// then saves all settings.
    /// </summary>
    private async Task PersistSettingAsync(Action applyToService)
    {
        applyToService();
        await _settingsService.SaveAsync();
    }

    // ═════════════════════════════════════════════════════════════
    // Property-change hooks (partial methods from CommunityToolkit)
    // ═════════════════════════════════════════════════════════════

    // -- Sound --

    partial void OnIsSoundEnabledChanged(bool value)
    {
        _ = PersistSettingAsync(() => _settingsService.IsSoundEnabled = value);
    }

    partial void OnSoundVolumeChanged(double value)
    {
        if (_soundService != null)
        {
            _soundService.Volume = value / 100.0;
        }
        _ = PersistSettingAsync(() => _settingsService.SoundVolume = value);
    }

    partial void OnSoundDurationChanged(int value)
    {
        if (_soundService != null)
        {
            _soundService.Duration = value;
        }
        _ = PersistSettingAsync(() => _settingsService.SoundDuration = value);
    }

    // -- Vibration --

    partial void OnIsVibrationEnabledChanged(bool value)
    {
        _ = PersistSettingAsync(() => _settingsService.IsVibrationEnabled = value);
    }

    // -- Notification --

    partial void OnIsNotificationEnabledChanged(bool value)
    {
        _ = PersistSettingAsync(() => _settingsService.IsNotificationEnabled = value);
    }

    // -- Timer durations --

    partial void OnPomodoroDurationChanged(int value)
    {
        RebuildTimeDictionary();
        if (!IsRunning && Mode == "pomodoro")
        {
            TimeLeft = _times["pomodoro"];
            _timerService.Reset(TimeLeft);
            UpdateProgressPercentage();
        }
        _ = PersistSettingAsync(() => _settingsService.PomodoroDuration = value);
    }

    partial void OnShortBreakDurationChanged(int value)
    {
        RebuildTimeDictionary();
        if (!IsRunning && Mode == "shortBreak")
        {
            TimeLeft = _times["shortBreak"];
            _timerService.Reset(TimeLeft);
            UpdateProgressPercentage();
        }
        _ = PersistSettingAsync(() => _settingsService.ShortBreakDuration = value);
    }

    partial void OnLongBreakDurationChanged(int value)
    {
        RebuildTimeDictionary();
        if (!IsRunning && Mode == "longBreak")
        {
            TimeLeft = _times["longBreak"];
            _timerService.Reset(TimeLeft);
            UpdateProgressPercentage();
        }
        _ = PersistSettingAsync(() => _settingsService.LongBreakDuration = value);
    }

    partial void OnPomodorosBeforeLongBreakChanged(int value)
    {
        _ = PersistSettingAsync(() => _settingsService.PomodorosBeforeLongBreak = value);
    }

    // -- Auto-start --

    partial void OnAutoStartBreaksChanged(bool value)
    {
        _ = PersistSettingAsync(() => _settingsService.AutoStartBreaks = value);
    }

    partial void OnAutoStartPomodorosChanged(bool value)
    {
        _ = PersistSettingAsync(() => _settingsService.AutoStartPomodoros = value);
    }

    // -- Display --

    partial void OnKeepScreenAwakeChanged(bool value)
    {
        _ = PersistSettingAsync(() => _settingsService.KeepScreenAwake = value);
    }

    // -- Daily reminder --

    partial void OnDailyReminderEnabledChanged(bool value)
    {
        _ = PersistSettingAsync(() => _settingsService.IsDailyReminderEnabled = value);
    }

    partial void OnDailyReminderHourChanged(int value)
    {
        _ = PersistSettingAsync(() => _settingsService.DailyReminderHour = value);
    }

    partial void OnDailyReminderMinuteChanged(int value)
    {
        _ = PersistSettingAsync(() => _settingsService.DailyReminderMinute = value);
    }

    // -- Productivity features --

    partial void OnIsMidpointReminderEnabledChanged(bool value)
    {
        _ = PersistSettingAsync(() => _settingsService.IsMidpointReminderEnabled = value);
    }

    partial void OnIsLastMinuteAlertEnabledChanged(bool value)
    {
        _ = PersistSettingAsync(() => _settingsService.IsLastMinuteAlertEnabled = value);
    }

    partial void OnAutoStartDelaySecondsChanged(int value)
    {
        var sanitized = Math.Clamp(value, 1, 10);
        if (value != sanitized)
        {
            AutoStartDelaySeconds = sanitized;
            return;
        }

        _ = PersistSettingAsync(() => _settingsService.AutoStartDelaySeconds = sanitized);
    }

    partial void OnCarryIncompleteTasksToNextSessionChanged(bool value)
    {
        _ = PersistSettingAsync(() => _settingsService.CarryIncompleteTasksToNextSession = value);
    }

    partial void OnAutoOpenTasksOnSessionStartChanged(bool value)
    {
        _ = PersistSettingAsync(() => _settingsService.AutoOpenTasksOnSessionStart = value);
    }

    partial void OnSessionTaskGoalChanged(int value)
    {
        var sanitized = Math.Clamp(value, 0, 10);
        if (value != sanitized)
        {
            SessionTaskGoal = sanitized;
            return;
        }

        _ = PersistSettingAsync(() => _settingsService.SessionTaskGoal = sanitized);
        UpdateSessionInfo();
    }

    // ═════════════════════════════════════════════════════════════
    // Timer events
    // ═════════════════════════════════════════════════════════════

    private void OnTimerTick(object? sender, int remainingSeconds)
    {
        TimeLeft = remainingSeconds;
        UpdateProgressPercentage();
        UpdateSessionInfo();
        OnPropertyChanged(nameof(CanAddMinute));

        _ = MaybeSendFocusRemindersAsync(remainingSeconds);
    }

    private async void OnTimerCompleted(object? sender, EventArgs e)
    {
        IsRunning = false;
        bool wasPomodoro = Mode == "pomodoro";

        // Sound notification
        if (IsSoundEnabled)
        {
            IsRinging = true;
            _soundService?.PlayNotificationSound();
        }

        // Vibration notification
        if (IsVibrationEnabled && _vibrationService.IsSupported)
        {
            _vibrationService.VibratePattern(new long[] { 0, 400, 200, 400, 200, 400 }, false);
        }

        // System notification
        if (IsNotificationEnabled)
        {
            var title = wasPomodoro ? "Pomodoro Completed" : "Break Over";
            var content = wasPomodoro
                ? BuildPomodoroCompletionMessage()
                : "Break is over. Ready to focus?";
            await _notificationService.ShowNotificationAsync(title, content);
        }

        // Track pomodoro completions for long-break logic
        if (wasPomodoro)
        {
            PomodoroCount++;
        }

        // Auto-advance to next session type
        await AutoAdvanceSession();

        // Refresh dashboard stats
        await LoadDashboardStatsAsync();

        // Auto-start next session if enabled
        bool shouldAutoStart = wasPomodoro
            ? AutoStartBreaks   // after a pomodoro, auto-start the break
            : AutoStartPomodoros; // after a break, auto-start the pomodoro

        if (shouldAutoStart)
        {
            // Short delay so the user sees the transition
            await Task.Delay(Math.Max(1, AutoStartDelaySeconds) * 1000);
            if (!IsRunning) // guard in case user manually started
            {
                ToggleTimer();
            }
        }
    }

    private async Task AutoAdvanceSession()
    {
        if (Mode == "pomodoro")
        {
            if (CarryIncompleteTasksToNextSession)
            {
                _pendingCarryOverTaskTexts.Clear();
                _pendingCarryOverTaskTexts.AddRange(
                    Tasks
                        .Where(t => !t.Completed)
                        .Select(t => t.Text?.Trim())
                        .Where(text => !string.IsNullOrWhiteSpace(text))
                        .Select(text => text!)
                        .Distinct(StringComparer.OrdinalIgnoreCase));
            }
            else
            {
                _pendingCarryOverTaskTexts.Clear();
            }
        }

        // End current session
        if (!string.IsNullOrEmpty(SessionId))
        {
            await _sessionRepository.EndSession(SessionId, DateTime.Now);
        }

        // Determine next mode
        string nextMode;
        if (Mode == "pomodoro")
        {
            // After N pomodoros, switch to long break instead of short break
            nextMode = (PomodoroCount % PomodorosBeforeLongBreak == 0)
                ? "longBreak"
                : "shortBreak";
        }
        else
        {
            // After any break, go back to pomodoro
            nextMode = "pomodoro";
        }

        // Start new session with new mode
        SessionId = null;
        Tasks.Clear();
        SessionNotes = "";

        ChangeMode(nextMode);
        await RestoreCarryOverTasksIfNeededAsync(nextMode);
        UpdateSessionInfo();
    }

    // ═════════════════════════════════════════════════════════════
    // Dashboard stats (inline on main page)
    // ═════════════════════════════════════════════════════════════

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

    // ═════════════════════════════════════════════════════════════
    // Commands — Timer
    // ═════════════════════════════════════════════════════════════

    [RelayCommand]
    private void ToggleTimer()
    {
        if (!IsRunning)
        {
            if (string.IsNullOrEmpty(SessionId))
            {
                // Brand-new session
                SessionId = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();
                Tasks.Clear();
                SessionNotes = "";
                ResetReminderFlags();

                _ = _sessionRepository.CreateSession(SessionId, Mode, DateTime.Now);
                _ = LoadSessions();

                _timerService.Start(TimeLeft);

                if (AutoOpenTasksOnSessionStart && Mode == "pomodoro")
                {
                    CloseAllOverlays();
                    ShowTasks = true;
                }
            }
            else
            {
                // BUG FIX: Resuming a paused session — use Resume() instead of
                // Start() so the wall-clock sync (TargetEndTime) is maintained.
                _timerService.Resume();
            }

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
        ResetReminderFlags();
        _timerService.Reset(_times[Mode]);
        TimeLeft = _times[Mode];
        IsRunning = false;
        UpdateProgressPercentage();
        UpdateSessionInfo();
    }

    [RelayCommand]
    private async Task SkipSession()
    {
        await AutoAdvanceSession();
    }

    [RelayCommand]
    private void ChangeMode(string newMode)
    {
        if (IsRunning) return;

        // Close any open overlays
        CloseAllOverlays();

        Mode = newMode;
        ResetReminderFlags();
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

    // ═════════════════════════════════════════════════════════════
    // Commands — Tasks
    // ═════════════════════════════════════════════════════════════

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
    private async Task EditTask((int taskId, string newText) args)
    {
        if (string.IsNullOrWhiteSpace(args.newText))
            return;

        var task = Tasks.FirstOrDefault(t => t.Id == args.taskId);
        if (task != null)
        {
            task.Text = args.newText;
            await _taskRepository.UpdateTaskAsync(task);
        }
    }

    // ═════════════════════════════════════════════════════════════
    // Commands — Sessions & History
    // ═════════════════════════════════════════════════════════════

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
        SessionNotes = "";
        _pendingCarryOverTaskTexts.Clear();

        ResetTimer();
        UpdateSessionInfo();

        // Refresh dashboard stats
        await LoadDashboardStatsAsync();
    }

    // ═════════════════════════════════════════════════════════════
    // Commands — Sound
    // ═════════════════════════════════════════════════════════════

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
        _vibrationService?.Cancel();
    }

    [RelayCommand]
    private void TestSound()
    {
        _soundService?.PlayNotificationSound();
    }

    // ═════════════════════════════════════════════════════════════
    // Commands — Overlay toggles (mutual exclusion)
    // ═════════════════════════════════════════════════════════════

    /// <summary>
    /// Closes all overlay panels.
    /// </summary>
    private void CloseAllOverlays()
    {
        ShowTasks = false;
        ShowDashboard = false;
        ShowSettings = false;
    }

    [RelayCommand]
    private void ToggleTasks()
    {
        if (ShowTasks)
        {
            ShowTasks = false;
        }
        else
        {
            CloseAllOverlays();
            ShowTasks = true;
        }
    }

    [RelayCommand]
    private async Task ToggleDashboard()
    {
        if (ShowDashboard)
        {
            ShowDashboard = false;
        }
        else
        {
            CloseAllOverlays();
            ShowDashboard = true;
            await LoadDashboardData();
        }
    }

    [RelayCommand]
    private void ToggleSettings()
    {
        if (ShowSettings)
        {
            ShowSettings = false;
        }
        else
        {
            CloseAllOverlays();
            ShowSettings = true;
        }
    }

    [RelayCommand]
    private void ToggleEditGoals()
    {
        ShowEditGoals = !ShowEditGoals;
    }

    // ═════════════════════════════════════════════════════════════
    // Commands — Dashboard data loading
    // ═════════════════════════════════════════════════════════════

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

        // Also persist goals through settings
        _settingsService.DailyGoal = DailyGoal;
        _settingsService.WeeklyGoal = WeeklyGoal;
        _settingsService.MonthlyGoal = MonthlyGoal;
        await _settingsService.SaveAsync();

        ShowEditGoals = false;
        await LoadGoals(); // Refresh to ensure UI is in sync
    }

    [RelayCommand]
    private void ChangeDate(int daysOffset)
    {
        SelectedDate = SelectedDate.AddDays(daysOffset);
        _ = LoadDashboardData();
    }

    // ═════════════════════════════════════════════════════════════
    // Helpers
    // ═════════════════════════════════════════════════════════════

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

        OnPropertyChanged(nameof(SessionGoalProgressText));
    }

    private string BuildPomodoroCompletionMessage()
    {
        if (SessionTaskGoal <= 0)
        {
            return "Great work! Time for a break.";
        }

        var completedTasks = Tasks.Count(t => t.Completed);
        if (completedTasks >= SessionTaskGoal)
        {
            return $"Great work! Goal hit: {completedTasks}/{SessionTaskGoal} tasks.";
        }

        var remaining = SessionTaskGoal - completedTasks;
        var suffix = remaining == 1 ? "" : "s";
        return $"Break time. {remaining} more task{suffix} to hit your goal.";
    }

    private void ResetReminderFlags()
    {
        _midpointReminderTriggered = false;
        _lastMinuteAlertTriggered = false;
    }

    private async Task MaybeSendFocusRemindersAsync(int remainingSeconds)
    {
        if (!IsRunning || Mode != "pomodoro")
        {
            return;
        }

        var totalFocusSeconds = _times["pomodoro"];

        if (IsMidpointReminderEnabled &&
            !_midpointReminderTriggered &&
            totalFocusSeconds >= 120 &&
            remainingSeconds <= totalFocusSeconds / 2)
        {
            _midpointReminderTriggered = true;

            if (IsNotificationEnabled)
            {
                await _notificationService.ShowNotificationAsync(
                    "Halfway There",
                    "You're halfway through this focus block.");
            }
        }

        if (IsLastMinuteAlertEnabled &&
            !_lastMinuteAlertTriggered &&
            totalFocusSeconds >= 120 &&
            remainingSeconds <= 60)
        {
            _lastMinuteAlertTriggered = true;

            if (IsNotificationEnabled)
            {
                await _notificationService.ShowNotificationAsync(
                    "Final Minute",
                    "One minute left. Wrap up your current task.");
            }

            if (IsVibrationEnabled && _vibrationService.IsSupported)
            {
                _vibrationService.Vibrate(200);
            }
        }
    }

    private async Task RestoreCarryOverTasksIfNeededAsync(string nextMode)
    {
        if (nextMode != "pomodoro" ||
            !CarryIncompleteTasksToNextSession ||
            _pendingCarryOverTaskTexts.Count == 0)
        {
            return;
        }

        var nextSessionId = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();
        SessionId = nextSessionId;
        await _sessionRepository.CreateSession(nextSessionId, nextMode, DateTime.Now);

        foreach (var taskText in _pendingCarryOverTaskTexts)
        {
            var task = await _taskRepository.Add(taskText, nextSessionId);
            if (task != null)
            {
                Tasks.Add(task);
            }
        }

        _pendingCarryOverTaskTexts.Clear();
    }

    public string FormatTime(int seconds)
    {
        var mins = seconds / 60;
        var secs = seconds % 60;
        return $"{mins:D2}:{secs:D2}";
    }
}
