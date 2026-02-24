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
        IsStreakProtectionEnabled = _settingsService.IsStreakProtectionEnabled;
        DailyFocusQuotaMinutes = _settingsService.DailyFocusQuotaMinutes;
        DefaultTaskPriority = _settingsService.DefaultTaskPriority;
        IsBreakSuggestionsEnabled = _settingsService.IsBreakSuggestionsEnabled;
        IsRetroPromptEnabled = _settingsService.IsRetroPromptEnabled;

        // Initialize built-in templates
        InitializeDefaultTemplates();
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
        _timerService.UpdateAlarmSettings(value, IsVibrationEnabled);
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
        _timerService.UpdateAlarmSettings(IsSoundEnabled, value);
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

        // Sound notification (repeating until dismissed)
        if (IsSoundEnabled && !alarmAlreadyStarted)
        {
            _soundService?.PlayNotificationSound();
        }
        if (IsSoundEnabled)
        {
            IsRinging = true;
        }

        // Vibration notification (repeating until dismissed)
        if (IsVibrationEnabled && _vibrationService.IsSupported && !alarmAlreadyStarted)
        {
            _vibrationService.VibratePattern(new long[] { 0, 400, 200, 400, 200, 400 }, true);
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
        ShowCompletionDialog = true;
    }

    [RelayCommand]
    private async Task DismissCompletion()
    {
        // Stop alarm (sound + vibration)
        StopAlarm();

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
