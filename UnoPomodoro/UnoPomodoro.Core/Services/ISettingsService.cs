using System.Threading.Tasks;

namespace UnoPomodoro.Services;

public interface ISettingsService
{
    // Sound settings
    bool IsSoundEnabled { get; set; }
    double SoundVolume { get; set; }
    int SoundDuration { get; set; }

    // Vibration settings
    bool IsVibrationEnabled { get; set; }

    // Timer settings
    int PomodoroDuration { get; set; }      // minutes
    int ShortBreakDuration { get; set; }     // minutes
    int LongBreakDuration { get; set; }      // minutes
    int PomodorosBeforeLongBreak { get; set; }

    // Auto-start settings
    bool AutoStartBreaks { get; set; }
    bool AutoStartPomodoros { get; set; }

    // Display settings
    bool KeepScreenAwake { get; set; }

    // Notification settings
    bool IsNotificationEnabled { get; set; }

    // Daily reminder
    bool IsDailyReminderEnabled { get; set; }
    int DailyReminderHour { get; set; }
    int DailyReminderMinute { get; set; }

    // Productivity features
    bool IsMidpointReminderEnabled { get; set; }
    bool IsLastMinuteAlertEnabled { get; set; }
    int AutoStartDelaySeconds { get; set; }
    bool CarryIncompleteTasksToNextSession { get; set; }
    bool AutoOpenTasksOnSessionStart { get; set; }
    int SessionTaskGoal { get; set; }

    // Goals (persisted)
    int DailyGoal { get; set; }
    int WeeklyGoal { get; set; }
    int MonthlyGoal { get; set; }

    // Focus Streak Protection
    bool IsStreakProtectionEnabled { get; set; }

    // Daily Focus Quota (minutes, 0 = unlimited)
    int DailyFocusQuotaMinutes { get; set; }

    // Default task priority for new tasks
    int DefaultTaskPriority { get; set; }

    // Break activity suggestions enabled
    bool IsBreakSuggestionsEnabled { get; set; }

    // Session retrospective prompt enabled
    bool IsRetroPromptEnabled { get; set; }

    /// <summary>
    /// Saves all settings to persistent storage.
    /// </summary>
    Task SaveAsync();

    /// <summary>
    /// Loads all settings from persistent storage.
    /// </summary>
    Task LoadAsync();
}
