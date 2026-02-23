using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace UnoPomodoro.Services;

public class SettingsService : ISettingsService
{
    private const string SettingsFileName = "pomodoro_settings.json";

    private Dictionary<string, object> _settings = new();

    // Sound settings
    public bool IsSoundEnabled
    {
        get => GetValue<bool>("IsSoundEnabled", true);
        set => SetValue("IsSoundEnabled", value);
    }

    public double SoundVolume
    {
        get => GetValue<double>("SoundVolume", 100.0);
        set => SetValue("SoundVolume", value);
    }

    public int SoundDuration
    {
        get => GetValue<int>("SoundDuration", 5);
        set => SetValue("SoundDuration", value);
    }

    // Vibration settings
    public bool IsVibrationEnabled
    {
        get => GetValue<bool>("IsVibrationEnabled", true);
        set => SetValue("IsVibrationEnabled", value);
    }

    // Timer settings
    public int PomodoroDuration
    {
        get => GetValue<int>("PomodoroDuration", 25);
        set => SetValue("PomodoroDuration", value);
    }

    public int ShortBreakDuration
    {
        get => GetValue<int>("ShortBreakDuration", 5);
        set => SetValue("ShortBreakDuration", value);
    }

    public int LongBreakDuration
    {
        get => GetValue<int>("LongBreakDuration", 15);
        set => SetValue("LongBreakDuration", value);
    }

    public int PomodorosBeforeLongBreak
    {
        get => GetValue<int>("PomodorosBeforeLongBreak", 4);
        set => SetValue("PomodorosBeforeLongBreak", value);
    }

    // Auto-start settings
    public bool AutoStartBreaks
    {
        get => GetValue<bool>("AutoStartBreaks", false);
        set => SetValue("AutoStartBreaks", value);
    }

    public bool AutoStartPomodoros
    {
        get => GetValue<bool>("AutoStartPomodoros", false);
        set => SetValue("AutoStartPomodoros", value);
    }

    // Display settings
    public bool KeepScreenAwake
    {
        get => GetValue<bool>("KeepScreenAwake", true);
        set => SetValue("KeepScreenAwake", value);
    }

    // Notification settings
    public bool IsNotificationEnabled
    {
        get => GetValue<bool>("IsNotificationEnabled", true);
        set => SetValue("IsNotificationEnabled", value);
    }

    // Daily reminder
    public bool IsDailyReminderEnabled
    {
        get => GetValue<bool>("IsDailyReminderEnabled", false);
        set => SetValue("IsDailyReminderEnabled", value);
    }

    public int DailyReminderHour
    {
        get => GetValue<int>("DailyReminderHour", 9);
        set => SetValue("DailyReminderHour", value);
    }

    public int DailyReminderMinute
    {
        get => GetValue<int>("DailyReminderMinute", 0);
        set => SetValue("DailyReminderMinute", value);
    }

    // Productivity features
    public bool IsMidpointReminderEnabled
    {
        get => GetValue<bool>("IsMidpointReminderEnabled", false);
        set => SetValue("IsMidpointReminderEnabled", value);
    }

    public bool IsLastMinuteAlertEnabled
    {
        get => GetValue<bool>("IsLastMinuteAlertEnabled", true);
        set => SetValue("IsLastMinuteAlertEnabled", value);
    }

    public int AutoStartDelaySeconds
    {
        get => GetValue<int>("AutoStartDelaySeconds", 2);
        set => SetValue("AutoStartDelaySeconds", value);
    }

    public bool CarryIncompleteTasksToNextSession
    {
        get => GetValue<bool>("CarryIncompleteTasksToNextSession", false);
        set => SetValue("CarryIncompleteTasksToNextSession", value);
    }

    public bool AutoOpenTasksOnSessionStart
    {
        get => GetValue<bool>("AutoOpenTasksOnSessionStart", false);
        set => SetValue("AutoOpenTasksOnSessionStart", value);
    }

    public int SessionTaskGoal
    {
        get => GetValue<int>("SessionTaskGoal", 0);
        set => SetValue("SessionTaskGoal", value);
    }

    // Goals
    public int DailyGoal
    {
        get => GetValue<int>("DailyGoal", 120);
        set => SetValue("DailyGoal", value);
    }

    public int WeeklyGoal
    {
        get => GetValue<int>("WeeklyGoal", 840);
        set => SetValue("WeeklyGoal", value);
    }

    public int MonthlyGoal
    {
        get => GetValue<int>("MonthlyGoal", 3600);
        set => SetValue("MonthlyGoal", value);
    }

    public SettingsService()
    {
        LoadAsync().GetAwaiter().GetResult();
    }

    public async Task SaveAsync()
    {
        try
        {
#if __ANDROID__
            var prefs = Android.App.Application.Context.GetSharedPreferences(
                "PomodoroSettings", Android.Content.FileCreationMode.Private);
            var editor = prefs?.Edit();
            if (editor != null)
            {
                var json = JsonSerializer.Serialize(_settings);
                editor.PutString("settings_json", json);
                editor.Apply();
            }
#else
            var filePath = GetSettingsFilePath();
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var json = JsonSerializer.Serialize(_settings, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            await File.WriteAllTextAsync(filePath, json);
#endif
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error saving settings: {ex.Message}");
        }
    }

    public async Task LoadAsync()
    {
        try
        {
#if __ANDROID__
            var prefs = Android.App.Application.Context.GetSharedPreferences(
                "PomodoroSettings", Android.Content.FileCreationMode.Private);
            var json = prefs?.GetString("settings_json", null);
            if (!string.IsNullOrEmpty(json))
            {
                var loaded = JsonSerializer.Deserialize<Dictionary<string, object>>(json);
                if (loaded != null)
                {
                    _settings = loaded;
                }
            }
#else
            var filePath = GetSettingsFilePath();
            if (File.Exists(filePath))
            {
                var json = await File.ReadAllTextAsync(filePath);
                var loaded = JsonSerializer.Deserialize<Dictionary<string, object>>(json);
                if (loaded != null)
                {
                    _settings = loaded;
                }
            }
#endif
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading settings: {ex.Message}");
            _settings = new Dictionary<string, object>();
        }
    }

    private static string GetSettingsFilePath()
    {
        var folder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return Path.Combine(folder, SettingsFileName);
    }

    private T GetValue<T>(string key, T defaultValue)
    {
        if (!_settings.TryGetValue(key, out var value))
        {
            return defaultValue;
        }

        try
        {
            // System.Text.Json deserializes numbers as JsonElement, so we need to handle conversion
            if (value is JsonElement element)
            {
                if (typeof(T) == typeof(bool))
                {
                    return (T)(object)element.GetBoolean();
                }
                if (typeof(T) == typeof(int))
                {
                    return (T)(object)element.GetInt32();
                }
                if (typeof(T) == typeof(double))
                {
                    return (T)(object)element.GetDouble();
                }
                if (typeof(T) == typeof(string))
                {
                    return (T)(object)(element.GetString() ?? defaultValue?.ToString() ?? string.Empty);
                }
            }

            return (T)Convert.ChangeType(value, typeof(T));
        }
        catch
        {
            return defaultValue;
        }
    }

    private void SetValue(string key, object value)
    {
        _settings[key] = value;
    }
}
