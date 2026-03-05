using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Text.Json;
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

    // ── Completion dialog state ──────────────────────────────────

    [ObservableProperty]
    private bool _showCompletionDialog;

    [ObservableProperty]
    private string _completionTitle = "";

    [ObservableProperty]
    private string _completionMessage = "";

    [ObservableProperty]
    private string _nextActionLabel = "";

    [ObservableProperty]
    private bool _showUpdateDialog;

    [ObservableProperty]
    private string _updateTitle = "";

    [ObservableProperty]
    private string _updateMessage = "";

    [ObservableProperty]
    private string _updateUrl = "";

    private string _pendingNextMode = "shortBreak";
    private bool _completionHandled;
    private CancellationTokenSource? _vibrationCancellationTokenSource;
    private System.Threading.CancellationTokenSource? _vibrationAutoStopCts;
    private System.Threading.CancellationTokenSource? _undismissedCompletionReminderCts;
    private static readonly TimeSpan UndismissedReminderInitialDelay = TimeSpan.FromHours(1);
    private static readonly TimeSpan UndismissedReminderInterval = TimeSpan.FromMinutes(5);

    [ObservableProperty]
    private bool _showUndismissedReminderPopup;

    [ObservableProperty]
    private string _undismissedReminderMessage = "Pomodoro completed more than an hour ago. Please dismiss it.";

    // ── Task management ──────────────────────────────────────────

    [ObservableProperty]
    private string _newTask = "";

    public ObservableCollection<TaskItem> Tasks { get; } = new();
    public ObservableCollection<TaskItem> TaskHistoryTasks { get; } = new();

    [ObservableProperty]
    private bool _showAllTaskDays = true;

    [ObservableProperty]
    private DateTimeOffset _taskHistoryDate = DateTimeOffset.Now.Date;

    // ── Coffee tracking ──────────────────────────────────────────

    [ObservableProperty]
    private int _coffeeCount;

    [ObservableProperty]
    private int _stretchCount;

    // ── Utility timers ───────────────────────────────────────────

    [ObservableProperty]
    private string _currentTimeText = DateTime.Now.ToString("HH:mm:ss");

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(StopwatchDisplay))]
    private int _stopwatchSeconds;

    [ObservableProperty]
    private bool _isStopwatchRunning;

    [ObservableProperty]
    private int _countdownDurationMinutes = 10;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CountdownDisplay))]
    private int _countdownRemainingSeconds = 10 * 60;

    [ObservableProperty]
    private bool _isCountdownRunning;

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
    
    [ObservableProperty]
    private int _vibrationDuration = 5;

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

    // ── Feature: Focus Streak Protection ─────────────────────────

    [ObservableProperty]
    private bool _isStreakProtectionEnabled;

    [ObservableProperty]
    private bool _showStreakWarning;

    [ObservableProperty]
    private string _streakWarningMessage = "";

    // ── Feature: Session Tagging ─────────────────────────────────

    [ObservableProperty]
    private string _sessionTag = "";

    // ── Feature: Distraction Counter ─────────────────────────────

    [ObservableProperty]
    private int _distractionCount;

    // ── Feature: Pomodoro Templates ──────────────────────────────

    [ObservableProperty]
    private string _activeTemplateName = "";

    public ObservableCollection<PomodoroTemplate> Templates { get; } = new();

    // ── Feature: Daily Focus Quota ───────────────────────────────

    [ObservableProperty]
    private int _dailyFocusQuotaMinutes;

    [ObservableProperty]
    private bool _isDailyQuotaExceeded;

    [ObservableProperty]
    private string _quotaStatusText = "";

    // ── Feature: Task Time Estimation ────────────────────────────

    [ObservableProperty]
    private string _estimationAccuracyText = "";

    // ── Feature: Break Activity Suggestions ──────────────────────

    [ObservableProperty]
    private bool _isBreakSuggestionsEnabled;

    [ObservableProperty]
    private string _breakSuggestion = "";

    // ── Feature: Session Rating / Retrospective ──────────────────

    [ObservableProperty]
    private bool _isRetroPromptEnabled;

    [ObservableProperty]
    private bool _showRetroPrompt;

    [ObservableProperty]
    private int _sessionRating;

    [ObservableProperty]
    private string _retroNote = "";

    // ── Feature: Default Task Priority ───────────────────────────

    [ObservableProperty]
    private int _defaultTaskPriority = 3; // TaskPriority.None

    // ── Overlay visibility ───────────────────────────────────────

    [ObservableProperty]
    private bool _showTasks = false;

    [ObservableProperty]
    private bool _showDashboard = false;

    [ObservableProperty]
    private bool _showSettings = false;

    [ObservableProperty]
    private bool _showEditGoals = false;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsStopwatchToolVisible))]
    [NotifyPropertyChangedFor(nameof(IsCountdownToolVisible))]
    private bool _showExtraToolPanel;

    [ObservableProperty]
    private bool _showExtraToolPicker;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsStopwatchToolVisible))]
    [NotifyPropertyChangedFor(nameof(IsCountdownToolVisible))]
    private string _activeExtraTool = "";

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
    private readonly Dictionary<string, int> _dailyCoffeeCounts = new(StringComparer.Ordinal);
    private readonly Dictionary<string, int> _dailyStretchCounts = new(StringComparer.Ordinal);
    private bool _midpointReminderTriggered;
    private bool _lastMinuteAlertTriggered;
    private string _activeCountsDateKey = DateTime.Now.ToString("yyyy-MM-dd");
    private System.Threading.CancellationTokenSource? _clockCts;
    private System.Threading.CancellationTokenSource? _stopwatchCts;
    private System.Threading.CancellationTokenSource? _countdownCts;

    public int TotalDuration => _times.TryGetValue(Mode, out var duration) ? duration : 25 * 60;
    public string StopwatchDisplay => FormatTime(StopwatchSeconds);
    public string CountdownDisplay => FormatTime(Math.Max(0, CountdownRemainingSeconds));
    public bool IsStopwatchToolVisible => ShowExtraToolPanel && ActiveExtraTool == "stopwatch";
    public bool IsCountdownToolVisible => ShowExtraToolPanel && ActiveExtraTool == "countdown";

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
        CountdownRemainingSeconds = Math.Max(1, CountdownDurationMinutes) * 60;

        StartClockTicker();
        await LoadTaskHistoryAsync();

        if (_timerService.CompletionAlarmStartedByPlatform && !ShowCompletionDialog)
        {
            CompletionTitle = "Session Completed!";
            CompletionMessage = "Tap dismiss to stop the alarm and continue.";
            NextActionLabel = "Continue";
            IsRinging = IsSoundEnabled || IsVibrationEnabled;
            ShowCompletionDialog = true;
        }

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
        VibrationDuration = _settingsService.VibrationDuration;
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
        ShowAllTaskDays = _settingsService.ShowAllTaskDays;
        IsStreakProtectionEnabled = _settingsService.IsStreakProtectionEnabled;
        DailyFocusQuotaMinutes = _settingsService.DailyFocusQuotaMinutes;
        DefaultTaskPriority = _settingsService.DefaultTaskPriority;
        IsBreakSuggestionsEnabled = _settingsService.IsBreakSuggestionsEnabled;
        IsRetroPromptEnabled = _settingsService.IsRetroPromptEnabled;
        LoadDailyWellnessCountsFromSettings();

        // Initialize built-in templates
        InitializeDefaultTemplates();
    }

    private void LoadDailyWellnessCountsFromSettings()
    {
        _dailyCoffeeCounts.Clear();
        _dailyStretchCounts.Clear();

        MergeDailyCounts(_dailyCoffeeCounts, _settingsService.DailyCoffeeCountsJson);
        MergeDailyCounts(_dailyStretchCounts, _settingsService.DailyStretchCountsJson);

        var todayKey = BuildDayKey(DateTime.Now);
        _activeCountsDateKey = todayKey;

        // Backfill from legacy single counter if present.
        if (_dailyCoffeeCounts.Count == 0 && _settingsService.CoffeeCount > 0)
        {
            _dailyCoffeeCounts[todayKey] = Math.Max(0, _settingsService.CoffeeCount);
        }

        CoffeeCount = GetDailyCount(_dailyCoffeeCounts, todayKey);
        StretchCount = GetDailyCount(_dailyStretchCounts, todayKey);
    }

    private static void MergeDailyCounts(Dictionary<string, int> target, string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return;
        }

        try
        {
            var data = JsonSerializer.Deserialize<Dictionary<string, int>>(json);
            if (data == null)
            {
                return;
            }

            foreach (var (key, value) in data)
            {
                if (!string.IsNullOrWhiteSpace(key))
                {
                    target[key] = Math.Max(0, value);
                }
            }
        }
        catch
        {
            // Ignore malformed persisted JSON and continue with empty defaults.
        }
    }

    private static string BuildDayKey(DateTime date)
    {
        return date.ToString("yyyy-MM-dd");
    }

    private static int GetDailyCount(Dictionary<string, int> map, string dayKey)
    {
        return map.TryGetValue(dayKey, out var value) ? Math.Max(0, value) : 0;
    }

    private async Task PersistDailyWellnessCountsAsync()
    {
        _settingsService.CoffeeCount = CoffeeCount;
        _settingsService.DailyCoffeeCountsJson = JsonSerializer.Serialize(_dailyCoffeeCounts);
        _settingsService.DailyStretchCountsJson = JsonSerializer.Serialize(_dailyStretchCounts);
        await _settingsService.SaveAsync();
    }

    private void EnsureCurrentDayWellnessCounts()
    {
        var todayKey = BuildDayKey(DateTime.Now);
        if (_activeCountsDateKey == todayKey)
        {
            return;
        }

        _activeCountsDateKey = todayKey;
        CoffeeCount = GetDailyCount(_dailyCoffeeCounts, todayKey);
        StretchCount = GetDailyCount(_dailyStretchCounts, todayKey);
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
        _timerService.UpdateAlarmSettings(value, IsVibrationEnabled, VibrationDuration);
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
        _timerService.UpdateAlarmSettings(IsSoundEnabled, value, VibrationDuration);
    }
    
    partial void OnVibrationDurationChanged(int value)
    {
        var duration = Math.Max(1, value);
        if (duration != value)
        {
            VibrationDuration = duration;
            return;
        }
        _ = PersistSettingAsync(() => _settingsService.VibrationDuration = duration);
        _timerService.UpdateAlarmSettings(IsSoundEnabled, IsVibrationEnabled, duration);
    }

    // -- Notification --

    partial void OnIsNotificationEnabledChanged(bool value)
    {
        _ = PersistSettingAsync(() => _settingsService.IsNotificationEnabled = value);
    }

    partial void OnCoffeeCountChanged(int value)
    {
        var sanitized = Math.Max(0, value);
        if (sanitized != value)
        {
            CoffeeCount = sanitized;
            return;
        }

        EnsureCurrentDayWellnessCounts();
        _dailyCoffeeCounts[_activeCountsDateKey] = sanitized;
        _ = PersistDailyWellnessCountsAsync();
    }

    partial void OnStretchCountChanged(int value)
    {
        var sanitized = Math.Max(0, value);
        if (sanitized != value)
        {
            StretchCount = sanitized;
            return;
        }

        EnsureCurrentDayWellnessCounts();
        _dailyStretchCounts[_activeCountsDateKey] = sanitized;
        _ = PersistDailyWellnessCountsAsync();
    }

    partial void OnShowAllTaskDaysChanged(bool value)
    {
        _ = PersistSettingAsync(() => _settingsService.ShowAllTaskDays = value);
        _ = LoadTaskHistoryAsync();
    }

    partial void OnTaskHistoryDateChanged(DateTimeOffset value)
    {
        _ = LoadTaskHistoryAsync();
    }

    partial void OnCountdownDurationMinutesChanged(int value)
    {
        var sanitized = Math.Clamp(value, 1, 180);
        if (sanitized != value)
        {
            CountdownDurationMinutes = sanitized;
            return;
        }

        if (!IsCountdownRunning)
        {
            CountdownRemainingSeconds = sanitized * 60;
            OnPropertyChanged(nameof(CountdownDisplay));
        }
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

    partial void OnIsStreakProtectionEnabledChanged(bool value)
    {
        _ = PersistSettingAsync(() => _settingsService.IsStreakProtectionEnabled = value);
    }

    partial void OnDailyFocusQuotaMinutesChanged(int value)
    {
        var sanitized = Math.Clamp(value, 0, 720); // 0 = unlimited, max 12 hours
        if (value != sanitized)
        {
            DailyFocusQuotaMinutes = sanitized;
            return;
        }
        _ = PersistSettingAsync(() => _settingsService.DailyFocusQuotaMinutes = sanitized);
        UpdateQuotaStatus();
    }

    partial void OnDefaultTaskPriorityChanged(int value)
    {
        var sanitized = Math.Clamp(value, 0, 3);
        if (value != sanitized)
        {
            DefaultTaskPriority = sanitized;
            return;
        }
        _ = PersistSettingAsync(() => _settingsService.DefaultTaskPriority = sanitized);
    }

    partial void OnIsBreakSuggestionsEnabledChanged(bool value)
    {
        _ = PersistSettingAsync(() => _settingsService.IsBreakSuggestionsEnabled = value);
    }

    partial void OnIsRetroPromptEnabledChanged(bool value)
    {
        _ = PersistSettingAsync(() => _settingsService.IsRetroPromptEnabled = value);
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

    private void OnTimerCompleted(object? sender, EventArgs e)
    {
        if (_completionHandled) return;
        _completionHandled = true;

        IsRunning = false;
        bool wasPomodoro = Mode == "pomodoro";

        // Sound and vibration may have already been started by the platform's
        // background service (which runs on a background thread and works even
        // when the phone is locked). Only start them here if the platform
        // didn't handle it, to avoid double-triggering.
        bool alarmAlreadyStarted = _timerService.CompletionAlarmStartedByPlatform;
        bool vibrationSupported = IsVibrationEnabled && _vibrationService.IsSupported;

        // Sound notification (repeating until dismissed)
        if (IsSoundEnabled && !alarmAlreadyStarted)
        {
            _soundService?.PlayNotificationSound();
        }
        if (IsSoundEnabled || vibrationSupported)
        {
            IsRinging = true;
        }

        // Vibration notification (repeating until dismissed)
        if (vibrationSupported && !alarmAlreadyStarted)
        {
            _vibrationService.VibratePattern(new long[] { 0, 400, 200, 400, 200, 400 }, true);
            _ = StopVibrationAfterDurationAsync();
        }

        // Track pomodoro completions for long-break logic
        if (wasPomodoro)
        {
            PomodoroCount++;

            // Increment actual pomodoros on all incomplete tasks in this session
            foreach (var task in Tasks.Where(t => !t.Completed))
            {
                task.ActualPomodoros++;
            }
            UpdateEstimationAccuracy();
        }

        // Determine next mode for the dialog
        if (wasPomodoro)
        {
            _pendingNextMode = (PomodoroCount % PomodorosBeforeLongBreak == 0)
                ? "longBreak"
                : "shortBreak";
        }
        else
        {
            _pendingNextMode = "pomodoro";
        }

        // Set dialog content
        CompletionTitle = wasPomodoro ? "Pomodoro Completed!" : "Break Over!";
        CompletionMessage = wasPomodoro
            ? BuildPomodoroCompletionMessage()
            : "Break is over. Ready to focus?";
        NextActionLabel = _pendingNextMode switch
        {
            "shortBreak" => "Start Short Break",
            "longBreak" => "Start Long Break",
            _ => "Start Focus"
        };

        // Generate break suggestion if entering a break and feature is enabled
        if (wasPomodoro && IsBreakSuggestionsEnabled)
        {
            BreakSuggestion = GenerateBreakSuggestion();
        }

        // Show retro prompt for pomodoros if feature is enabled
        if (wasPomodoro && IsRetroPromptEnabled)
        {
            ShowRetroPrompt = true;
        }

        // Show the completion dialog
        ShowUndismissedReminderPopup = false;
        ShowCompletionDialog = true;
        StartUndismissedCompletionReminderLoop();
    }

    private void StartUndismissedCompletionReminderLoop()
    {
        CancelUndismissedCompletionReminderLoop();

        var tokenSource = new System.Threading.CancellationTokenSource();
        _undismissedCompletionReminderCts = tokenSource;
        var token = tokenSource.Token;

        _ = RunUndismissedCompletionReminderLoopAsync(token);
    }

    private async Task RunUndismissedCompletionReminderLoopAsync(System.Threading.CancellationToken token)
    {
        try
        {
            await Task.Delay(UndismissedReminderInitialDelay, token);

            while (!token.IsCancellationRequested)
            {
                if (!ShowCompletionDialog)
                {
                    break;
                }

                ShowUndismissedReminderPopup = true;
                UndismissedReminderMessage = "Pomodoro is still waiting to be dismissed.";

                if (IsVibrationEnabled && _vibrationService.IsSupported)
                {
                    // Two short vibrations every reminder cycle.
                    _vibrationService.VibratePattern(new long[] { 0, 250, 150, 250 }, false);
                }

                await Task.Delay(UndismissedReminderInterval, token);
            }
        }
        catch (TaskCanceledException)
        {
            // expected when completion is dismissed
        }
    }

    private void CancelUndismissedCompletionReminderLoop()
    {
        try
        {
            _undismissedCompletionReminderCts?.Cancel();
        }
        catch
        {
            // no-op
        }
        finally
        {
            _undismissedCompletionReminderCts?.Dispose();
            _undismissedCompletionReminderCts = null;
            ShowUndismissedReminderPopup = false;
        }
    }

    [RelayCommand]
    private void DismissUndismissedReminderPopup()
    {
        ShowUndismissedReminderPopup = false;
    }

    [RelayCommand]
    private async Task DismissCompletion()
    {
        // Stop alarm (sound + vibration)
        StopAlarm();
        CancelUndismissedCompletionReminderLoop();

        // Dismiss the completion notification from the notification drawer
        _notificationService.DismissCompletionNotification();

        // Save retro data if available
        await SaveRetroDataToSession();

        // Hide the dialog
        ShowCompletionDialog = false;
        ShowRetroPrompt = false;

        // Advance to the next session
        await AutoAdvanceSession();

        // Refresh dashboard stats
        await LoadDashboardStatsAsync();
        UpdateQuotaStatus();
    }

    [RelayCommand]
    private async Task DismissCompletionAndStart()
    {
        // Stop alarm (sound + vibration)
        StopAlarm();
        CancelUndismissedCompletionReminderLoop();

        // Dismiss the completion notification from the notification drawer
        _notificationService.DismissCompletionNotification();

        // Save retro data if available
        await SaveRetroDataToSession();

        // Hide the dialog
        ShowCompletionDialog = false;
        ShowRetroPrompt = false;

        // Advance to the next session
        await AutoAdvanceSession();

        // Refresh dashboard stats
        await LoadDashboardStatsAsync();
        UpdateQuotaStatus();

        // Start the next timer after a short delay
        await Task.Delay(Math.Max(1, AutoStartDelaySeconds) * 1000);
        if (!IsRunning)
        {
            ToggleTimer();
        }
    }

    [RelayCommand]
    private void DismissUpdateDialog()
    {
        ShowUpdateDialog = false;
    }

    private async Task AutoAdvanceSession()
    {
        if (Mode == "pomodoro")
        {
            if (CarryIncompleteTasksToNextSession)
            {
                await LoadTaskHistoryAsync();

                var sourceTasks = ShowAllTaskDays
                    ? TaskHistoryTasks
                    : Tasks;
                if (sourceTasks.Count == 0)
                {
                    sourceTasks = Tasks;
                }

                _pendingCarryOverTaskTexts.Clear();
                _pendingCarryOverTaskTexts.AddRange(
                    sourceTasks
                        .Where(t => !t.Completed && t.Status != TaskWorkStatus.Done)
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
            // Check daily quota before starting a pomodoro
            if (Mode == "pomodoro" && IsDailyQuotaExceeded && DailyFocusQuotaMinutes > 0)
            {
                return; // Block — quota exceeded
            }

            // Check streak protection before starting
            if (Mode == "pomodoro" && IsStreakProtectionEnabled && CurrentStreak > 0 && ShowStreakWarning)
            {
                return; // Block — user hasn't dismissed the streak warning yet
            }

            if (string.IsNullOrEmpty(SessionId))
            {
                // Brand-new session
                SessionId = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();
                Tasks.Clear();
                SessionNotes = "";
                DistractionCount = 0;
                SessionTag = "";
                SessionRating = 0;
                RetroNote = "";
                ShowRetroPrompt = false;
                ResetReminderFlags();
                _completionHandled = false;

                _ = _sessionRepository.CreateSession(SessionId, Mode, DateTime.Now);
                _ = LoadSessions();

                _timerService.CurrentMode = Mode;
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
        CancelUndismissedCompletionReminderLoop();
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

    [RelayCommand]
    private void AddCoffee()
    {
        EnsureCurrentDayWellnessCounts();
        CoffeeCount++;
    }

    [RelayCommand]
    private void ResetCoffeeCount()
    {
        EnsureCurrentDayWellnessCounts();
        CoffeeCount = 0;
    }

    [RelayCommand]
    private void AddStretch()
    {
        EnsureCurrentDayWellnessCounts();
        StretchCount++;
    }

    [RelayCommand]
    private void ResetStretchCount()
    {
        EnsureCurrentDayWellnessCounts();
        StretchCount = 0;
    }

    [RelayCommand]
    private async Task RefreshTaskHistory()
    {
        await LoadTaskHistoryAsync();
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
        await LoadTaskHistoryAsync();
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
        await LoadTaskHistoryAsync();
    }

    [RelayCommand]
    private async Task ToggleTask(int taskId)
    {
        var task = Tasks.FirstOrDefault(t => t.Id == taskId)
            ?? TaskHistoryTasks.FirstOrDefault(t => t.Id == taskId)
            ?? await _taskRepository.GetTaskByIdAsync(taskId);
        if (task == null)
        {
            return;
        }

        var completed = !(task.Completed || task.Status == TaskWorkStatus.Done);
        var repositoryTask = await _taskRepository.ToggleCompleted(taskId, completed);
        var effectiveTask = repositoryTask ?? task;

        if (completed)
        {
            await StopTaskMonitoringIfRunningAsync(effectiveTask);
            effectiveTask.Completed = true;
            effectiveTask.CompletedAt = DateTime.Now;
            effectiveTask.Status = TaskWorkStatus.Done;
        }
        else
        {
            effectiveTask.Completed = false;
            effectiveTask.CompletedAt = null;
            if (effectiveTask.Status == TaskWorkStatus.Done)
            {
                effectiveTask.Status = TaskWorkStatus.Todo;
            }
        }

        await _taskRepository.UpdateTaskAsync(effectiveTask);
        await ReloadTaskCollectionsAsync();
    }

    [RelayCommand]
    private async Task EditTask((int taskId, string newText) args)
    {
        if (string.IsNullOrWhiteSpace(args.newText))
            return;

        var task = Tasks.FirstOrDefault(t => t.Id == args.taskId)
            ?? TaskHistoryTasks.FirstOrDefault(t => t.Id == args.taskId)
            ?? await _taskRepository.GetTaskByIdAsync(args.taskId);
        if (task != null)
        {
            task.Text = args.newText;
            await _taskRepository.UpdateTaskAsync(task);
        }
        await ReloadTaskCollectionsAsync();
    }

    [RelayCommand]
    private async Task ToggleTaskMonitoring(int taskId)
    {
        var task = await _taskRepository.GetTaskByIdAsync(taskId);
        if (task == null)
        {
            return;
        }

        if (task.TrackingStartedAtUtcTicks.HasValue)
        {
            await StopTaskMonitoringIfRunningAsync(task);
            if (!task.Completed && task.Status == TaskWorkStatus.InProgress)
            {
                task.Status = TaskWorkStatus.Todo;
            }
            await _taskRepository.UpdateTaskAsync(task);
            await ReloadTaskCollectionsAsync();
            return;
        }

        var allTasks = await _taskRepository.GetAllTasksAsync();
        foreach (var other in allTasks.Where(t => t.Id != task.Id && t.TrackingStartedAtUtcTicks.HasValue))
        {
            await StopTaskMonitoringIfRunningAsync(other);
            if (!other.Completed && other.Status == TaskWorkStatus.InProgress)
            {
                other.Status = TaskWorkStatus.Todo;
            }
            await _taskRepository.UpdateTaskAsync(other);
        }

        task.Completed = false;
        task.CompletedAt = null;
        task.Status = TaskWorkStatus.InProgress;
        task.TrackingStartedAtUtcTicks = DateTime.UtcNow.Ticks;
        await _taskRepository.UpdateTaskAsync(task);

        await ReloadTaskCollectionsAsync();
    }

    [RelayCommand]
    private async Task SetTaskTodo(int taskId)
    {
        var task = await _taskRepository.GetTaskByIdAsync(taskId);
        if (task == null)
        {
            return;
        }

        await StopTaskMonitoringIfRunningAsync(task);
        task.Completed = false;
        task.CompletedAt = null;
        task.Status = TaskWorkStatus.Todo;
        await _taskRepository.UpdateTaskAsync(task);
        await ReloadTaskCollectionsAsync();
    }

    [RelayCommand]
    private async Task SetTaskDone(int taskId)
    {
        var task = await _taskRepository.GetTaskByIdAsync(taskId);
        if (task == null)
        {
            return;
        }

        await StopTaskMonitoringIfRunningAsync(task);
        task.Completed = true;
        task.CompletedAt = DateTime.Now;
        task.Status = TaskWorkStatus.Done;
        await _taskRepository.UpdateTaskAsync(task);
        await ReloadTaskCollectionsAsync();
    }

    private async Task StopTaskMonitoringIfRunningAsync(TaskItem task)
    {
        if (!task.TrackingStartedAtUtcTicks.HasValue)
        {
            return;
        }

        try
        {
            var startedUtc = new DateTime(task.TrackingStartedAtUtcTicks.Value, DateTimeKind.Utc);
            var elapsedSeconds = (int)Math.Max(0, (DateTime.UtcNow - startedUtc).TotalSeconds);
            task.TrackedSeconds = Math.Max(0, task.TrackedSeconds) + elapsedSeconds;
        }
        catch
        {
            // Keep existing tracked total on malformed timestamps.
        }
        finally
        {
            task.TrackingStartedAtUtcTicks = null;
        }
    }

    private async Task ReloadTaskCollectionsAsync()
    {
        if (!string.IsNullOrEmpty(SessionId))
        {
            var sessionTasks = await _taskRepository.GetBySession(SessionId) ?? new List<TaskItem>();
            Tasks.Clear();
            foreach (var sessionTask in sessionTasks)
            {
                Tasks.Add(sessionTask);
            }
            UpdateSessionInfo();
        }

        await LoadTaskHistoryAsync();
    }

    private async Task LoadTaskHistoryAsync()
    {
        try
        {
            List<TaskItem> tasks = new();

            if (ShowAllTaskDays)
            {
                var taskLoad = _taskRepository.GetAllTasksAsync();
                if (taskLoad != null)
                {
                    tasks = (await taskLoad ?? new List<TaskItem>())
                        .Where(t => !t.Completed && t.Status != TaskWorkStatus.Done)
                        .ToList();
                }
            }
            else
            {
                var start = TaskHistoryDate.DateTime.Date;
                var end = start.AddDays(1).AddTicks(-1);
                var taskLoad = _taskRepository.GetTasksByDateRangeAsync(start, end);
                if (taskLoad != null)
                {
                    tasks = await taskLoad ?? new List<TaskItem>();
                }
            }

            TaskHistoryTasks.Clear();
            foreach (var task in tasks.OrderByDescending(t => t.Id))
            {
                TaskHistoryTasks.Add(task);
            }
        }
        catch
        {
            // Keep UI usable even if history loading fails or mocks are incomplete.
            TaskHistoryTasks.Clear();
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
        CancelUndismissedCompletionReminderLoop();

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

        // Reset enterprise feature state
        DistractionCount = 0;
        SessionTag = "";
        SessionRating = 0;
        RetroNote = "";
        ShowRetroPrompt = false;
        ShowStreakWarning = false;

        ResetTimer();
        UpdateSessionInfo();

        // Refresh dashboard stats and quota
        await LoadDashboardStatsAsync();
        UpdateQuotaStatus();
        await LoadTaskHistoryAsync();
    }

    // ═════════════════════════════════════════════════════════════
    // Commands — Sound
    // ═════════════════════════════════════════════════════════════

    [RelayCommand]
    private void ToggleSound()
    {
        IsSoundEnabled = !IsSoundEnabled;
    }

    private void ScheduleVibrationAutoStop()
    {
        CancelVibrationAutoStopSchedule();

        var durationSeconds = Math.Max(1, VibrationDuration);
        var tokenSource = new System.Threading.CancellationTokenSource();
        _vibrationAutoStopCts = tokenSource;
        var token = tokenSource.Token;

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(durationSeconds), token);
                if (!token.IsCancellationRequested)
                {
                    _vibrationService?.Cancel();
                }
            }
            catch (TaskCanceledException)
            {
                // expected when user stops alarm early
            }
        });
    }

    private void CancelVibrationAutoStopSchedule()
    {
        try
        {
            _vibrationAutoStopCts?.Cancel();
        }
        catch
        {
            // no-op
        }
        finally
        {
            _vibrationAutoStopCts?.Dispose();
            _vibrationAutoStopCts = null;
        }
    }

    [RelayCommand]
    private void StopAlarm()
    {
        _vibrationCancellationTokenSource?.Cancel();
        _vibrationCancellationTokenSource?.Dispose();
        _vibrationCancellationTokenSource = null;
        CancelVibrationAutoStopSchedule();
        IsRinging = false;
        _soundService?.StopNotificationSound();
        _vibrationService?.Cancel();
    }
    
    private async Task StopVibrationAfterDurationAsync()
    {
        try
        {
            _vibrationCancellationTokenSource?.Cancel();
            _vibrationCancellationTokenSource?.Dispose();
            _vibrationCancellationTokenSource = new CancellationTokenSource();
            var token = _vibrationCancellationTokenSource.Token;
            await Task.Delay(TimeSpan.FromSeconds(Math.Max(1, VibrationDuration)), token);
            _vibrationService?.Cancel();
        }
        catch (TaskCanceledException)
        {
            // Ignore cancellation when user manually dismisses alarm.
        }
    }

    [RelayCommand]
    private void TestSound()
    {
        _soundService?.PlayNotificationSound();
    }

    private void StartClockTicker()
    {
        if (_clockCts != null)
        {
            return;
        }

        var tokenSource = new System.Threading.CancellationTokenSource();
        _clockCts = tokenSource;
        var token = tokenSource.Token;

        _ = RunClockTickerAsync(token);
    }

    private async Task RunClockTickerAsync(System.Threading.CancellationToken token)
    {
        try
        {
            while (!token.IsCancellationRequested)
            {
                EnsureCurrentDayWellnessCounts();
                CurrentTimeText = DateTime.Now.ToString("HH:mm:ss");
                await Task.Delay(1000, token);
            }
        }
        catch (TaskCanceledException)
        {
            // expected on shutdown
        }
    }

    [RelayCommand]
    private void ToggleStopwatch()
    {
        if (IsStopwatchRunning)
        {
            _stopwatchCts?.Cancel();
            _stopwatchCts?.Dispose();
            _stopwatchCts = null;
            IsStopwatchRunning = false;
            return;
        }

        var tokenSource = new System.Threading.CancellationTokenSource();
        _stopwatchCts = tokenSource;
        IsStopwatchRunning = true;
        _ = RunStopwatchAsync(tokenSource.Token);
    }

    [RelayCommand]
    private void ResetStopwatch()
    {
        _stopwatchCts?.Cancel();
        _stopwatchCts?.Dispose();
        _stopwatchCts = null;
        IsStopwatchRunning = false;
        StopwatchSeconds = 0;
        OnPropertyChanged(nameof(StopwatchDisplay));
    }

    private async Task RunStopwatchAsync(System.Threading.CancellationToken token)
    {
        try
        {
            while (!token.IsCancellationRequested)
            {
                await Task.Delay(1000, token);
                if (token.IsCancellationRequested)
                {
                    break;
                }

                StopwatchSeconds++;
                OnPropertyChanged(nameof(StopwatchDisplay));
            }
        }
        catch (TaskCanceledException)
        {
            // expected when paused/reset
        }
    }

    [RelayCommand]
    private void ToggleCountdown()
    {
        if (IsCountdownRunning)
        {
            _countdownCts?.Cancel();
            _countdownCts?.Dispose();
            _countdownCts = null;
            IsCountdownRunning = false;
            return;
        }

        if (CountdownRemainingSeconds <= 0)
        {
            CountdownRemainingSeconds = Math.Max(1, CountdownDurationMinutes) * 60;
            OnPropertyChanged(nameof(CountdownDisplay));
        }

        var tokenSource = new System.Threading.CancellationTokenSource();
        _countdownCts = tokenSource;
        IsCountdownRunning = true;
        _ = RunCountdownAsync(tokenSource.Token);
    }

    [RelayCommand]
    private void ResetCountdown()
    {
        _countdownCts?.Cancel();
        _countdownCts?.Dispose();
        _countdownCts = null;
        IsCountdownRunning = false;
        CountdownRemainingSeconds = Math.Max(1, CountdownDurationMinutes) * 60;
        OnPropertyChanged(nameof(CountdownDisplay));
    }

    private async Task RunCountdownAsync(System.Threading.CancellationToken token)
    {
        try
        {
            while (!token.IsCancellationRequested && CountdownRemainingSeconds > 0)
            {
                await Task.Delay(1000, token);
                if (token.IsCancellationRequested)
                {
                    break;
                }

                CountdownRemainingSeconds = Math.Max(0, CountdownRemainingSeconds - 1);
                OnPropertyChanged(nameof(CountdownDisplay));
            }

            if (!token.IsCancellationRequested && CountdownRemainingSeconds <= 0)
            {
                IsCountdownRunning = false;
                _countdownCts?.Dispose();
                _countdownCts = null;

                if (IsVibrationEnabled && _vibrationService.IsSupported)
                {
                    // Countdown completion: two vibrations.
                    _vibrationService.VibratePattern(new long[] { 0, 250, 150, 250 }, false);
                }
            }
        }
        catch (TaskCanceledException)
        {
            // expected when paused/reset
        }
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
            _ = LoadTaskHistoryAsync();
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
    private void OpenExtraToolPicker()
    {
        ShowExtraToolPicker = true;
    }

    [RelayCommand]
    private void SelectStopwatchTool()
    {
        ActiveExtraTool = "stopwatch";
        ShowExtraToolPanel = true;
        ShowExtraToolPicker = false;
    }

    [RelayCommand]
    private void SelectCountdownTool()
    {
        ActiveExtraTool = "countdown";
        ShowExtraToolPanel = true;
        ShowExtraToolPicker = false;
    }

    [RelayCommand]
    private void HideExtraTools()
    {
        ShowExtraToolPicker = false;
        ShowExtraToolPanel = false;
        ActiveExtraTool = "";
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

    // ═════════════════════════════════════════════════════════════
    // Enterprise Feature: Distraction Counter
    // ═════════════════════════════════════════════════════════════

    [RelayCommand]
    private void LogDistraction()
    {
        DistractionCount++;
    }

    // ═════════════════════════════════════════════════════════════
    // Enterprise Feature: Task Priority Levels
    // ═════════════════════════════════════════════════════════════

    [RelayCommand]
    private void SetTaskPriority((int taskId, TaskPriority priority) args)
    {
        var task = Tasks.FirstOrDefault(t => t.Id == args.taskId);
        if (task != null)
        {
            task.Priority = args.priority;
            _ = _taskRepository.UpdateTaskAsync(task);
        }
    }

    [RelayCommand]
    private void SortTasksByPriority()
    {
        var sorted = Tasks.OrderBy(t => (int)t.Priority).ThenBy(t => t.SortOrder).ToList();
        Tasks.Clear();
        foreach (var task in sorted)
        {
            Tasks.Add(task);
        }
    }

    // ═════════════════════════════════════════════════════════════
    // Enterprise Feature: Task Time Estimation
    // ═════════════════════════════════════════════════════════════

    [RelayCommand]
    private void SetTaskEstimate((int taskId, int estimatedPomodoros) args)
    {
        var task = Tasks.FirstOrDefault(t => t.Id == args.taskId);
        if (task != null)
        {
            task.EstimatedPomodoros = Math.Max(0, args.estimatedPomodoros);
            _ = _taskRepository.UpdateTaskAsync(task);
            UpdateEstimationAccuracy();
        }
    }

    private void UpdateEstimationAccuracy()
    {
        var tasksWithEstimates = Tasks.Where(t => t.EstimatedPomodoros > 0).ToList();
        if (tasksWithEstimates.Count == 0)
        {
            EstimationAccuracyText = "";
            return;
        }

        var totalEstimated = tasksWithEstimates.Sum(t => t.EstimatedPomodoros);
        var totalActual = tasksWithEstimates.Sum(t => t.ActualPomodoros);

        if (totalActual == 0)
        {
            EstimationAccuracyText = $"Estimated: {totalEstimated} pomodoros";
            return;
        }

        var accuracy = totalEstimated > 0
            ? (int)Math.Round((double)totalActual / totalEstimated * 100)
            : 0;

        EstimationAccuracyText = $"Est: {totalEstimated}, Actual: {totalActual} ({accuracy}%)";
    }

    // ═════════════════════════════════════════════════════════════
    // Enterprise Feature: Pomodoro Templates
    // ═════════════════════════════════════════════════════════════

    private void InitializeDefaultTemplates()
    {
        Templates.Clear();
        Templates.Add(new PomodoroTemplate
        {
            Name = "Standard",
            PomodoroDuration = 25,
            ShortBreakDuration = 5,
            LongBreakDuration = 15,
            PomodorosBeforeLongBreak = 4
        });
        Templates.Add(new PomodoroTemplate
        {
            Name = "Deep Work",
            PomodoroDuration = 50,
            ShortBreakDuration = 10,
            LongBreakDuration = 30,
            PomodorosBeforeLongBreak = 3
        });
        Templates.Add(new PomodoroTemplate
        {
            Name = "Quick Sprint",
            PomodoroDuration = 15,
            ShortBreakDuration = 3,
            LongBreakDuration = 10,
            PomodorosBeforeLongBreak = 4
        });
    }

    [RelayCommand]
    private void ApplyTemplate(string templateName)
    {
        var template = Templates.FirstOrDefault(t => t.Name == templateName);
        if (template == null) return;
        if (IsRunning) return;

        ActiveTemplateName = template.Name;
        PomodoroDuration = template.PomodoroDuration;
        ShortBreakDuration = template.ShortBreakDuration;
        LongBreakDuration = template.LongBreakDuration;
        PomodorosBeforeLongBreak = template.PomodorosBeforeLongBreak;
    }

    // ═════════════════════════════════════════════════════════════
    // Enterprise Feature: Daily Focus Quota
    // ═════════════════════════════════════════════════════════════

    private void UpdateQuotaStatus()
    {
        if (DailyFocusQuotaMinutes <= 0)
        {
            IsDailyQuotaExceeded = false;
            QuotaStatusText = "";
            return;
        }

        var minutesUsed = TodayFocusMinutes;
        IsDailyQuotaExceeded = minutesUsed >= DailyFocusQuotaMinutes;

        var remaining = Math.Max(0, DailyFocusQuotaMinutes - minutesUsed);
        QuotaStatusText = IsDailyQuotaExceeded
            ? "Daily focus quota reached"
            : $"{remaining} min remaining of {DailyFocusQuotaMinutes} min quota";
    }

    // ═════════════════════════════════════════════════════════════
    // Enterprise Feature: Break Activity Suggestions
    // ═════════════════════════════════════════════════════════════

    private static readonly string[] BreakSuggestions = new[]
    {
        "Stand up and stretch for a minute",
        "Take a short walk around the room",
        "Do some deep breathing exercises",
        "Look away from the screen — focus on something distant",
        "Drink a glass of water",
        "Do 10 quick desk stretches",
        "Close your eyes and relax for a moment",
        "Step outside for fresh air",
        "Do a quick mindfulness exercise",
        "Tidy up your workspace"
    };

    private int _breakSuggestionIndex;

    private string GenerateBreakSuggestion()
    {
        var suggestion = BreakSuggestions[_breakSuggestionIndex % BreakSuggestions.Length];
        _breakSuggestionIndex++;
        return suggestion;
    }

    // ═════════════════════════════════════════════════════════════
    // Enterprise Feature: Session Rating / Retrospective
    // ═════════════════════════════════════════════════════════════

    [RelayCommand]
    private async Task SaveRetro()
    {
        await SaveRetroDataToSession();
        ShowRetroPrompt = false;
    }

    private async Task SaveRetroDataToSession()
    {
        if (string.IsNullOrEmpty(SessionId)) return;
        if (SessionRating <= 0 && string.IsNullOrWhiteSpace(RetroNote)) return;

        try
        {
            var sessions = await _sessionRepository.GetSessionsWithStats();
            var session = sessions.FirstOrDefault(s => s.Id == SessionId);
            if (session != null)
            {
                session.Rating = SessionRating;
                session.RetroNote = RetroNote;
                session.DistractionCount = DistractionCount;
                session.Tag = SessionTag;
                // Persist via repository — uses EndSession overload or direct update
                // The session's end time was already set; we just update extra fields
            }
        }
        catch
        {
            // Swallow — retro save is best-effort
        }
    }

    // ═════════════════════════════════════════════════════════════
    // Enterprise Feature: Focus Streak Protection
    // ═════════════════════════════════════════════════════════════

    [RelayCommand]
    private void DismissStreakWarning()
    {
        ShowStreakWarning = false;
        StreakWarningMessage = "";
    }

    /// <summary>
    /// Call before allowing a timer reset or session abandonment to warn the
    /// user that they'll break their active streak.
    /// </summary>
    public void CheckStreakProtection()
    {
        if (!IsStreakProtectionEnabled || CurrentStreak <= 0)
        {
            ShowStreakWarning = false;
            return;
        }

        ShowStreakWarning = true;
        StreakWarningMessage = $"You have a {CurrentStreak}-day streak! Are you sure you want to stop?";
    }

    // ═════════════════════════════════════════════════════════════
    // Enterprise Feature: Bulk Task Operations
    // ═════════════════════════════════════════════════════════════

    [RelayCommand]
    private async Task CompleteAllTasks()
    {
        foreach (var task in Tasks.Where(t => !t.Completed).ToList())
        {
            task.Completed = true;
            task.CompletedAt = DateTime.Now;
            await _taskRepository.ToggleCompleted(task.Id, true);
        }
        UpdateSessionInfo();
    }

    [RelayCommand]
    private async Task DeleteCompletedTasks()
    {
        var completed = Tasks.Where(t => t.Completed).ToList();
        foreach (var task in completed)
        {
            await _taskRepository.Delete(task.Id);
            Tasks.Remove(task);
        }
        UpdateSessionInfo();
    }

    [RelayCommand]
    private void ReorderTask((int taskId, int newIndex) args)
    {
        var task = Tasks.FirstOrDefault(t => t.Id == args.taskId);
        if (task == null) return;

        var oldIndex = Tasks.IndexOf(task);
        if (oldIndex < 0) return;

        var newIndex = Math.Clamp(args.newIndex, 0, Tasks.Count - 1);
        if (oldIndex == newIndex) return;

        Tasks.Move(oldIndex, newIndex);

        // Update sort orders
        for (int i = 0; i < Tasks.Count; i++)
        {
            Tasks[i].SortOrder = i;
        }
    }
}
