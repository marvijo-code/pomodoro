using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using UnoPomodoro.Data.Models;
using UnoPomodoro.Data.Repositories;
using UnoPomodoro.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace UnoPomodoro.ViewModels;

public partial class DashboardViewModel : ObservableObject
{
    private readonly ISessionRepository _sessionRepository;
    private readonly ITaskRepository _taskRepository;
    private readonly IStatisticsService _statisticsService;

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
    private ObservableCollection<DailyStats> _dailyStats = new();

    [ObservableProperty]
    private ObservableCollection<Session> _recentSessions = new();

    [ObservableProperty]
    private ObservableCollection<Achievement> _achievements = new();

    [ObservableProperty]
    private ObservableCollection<CategoryStats> _categoryStats = new();

    [ObservableProperty]
    private int _currentStreak;

    [ObservableProperty]
    private int _longestStreak;

    [ObservableProperty]
    private string _mostProductiveTime = "Morning";

    [ObservableProperty]
    private double _weeklyAverage;

    [ObservableProperty]
    private double _monthlyAverage;

    public DashboardViewModel(
        ISessionRepository sessionRepository,
        ITaskRepository taskRepository,
        IStatisticsService statisticsService)
    {
        _sessionRepository = sessionRepository;
        _taskRepository = taskRepository;
        _statisticsService = statisticsService;

        LoadData();
    }

    private async void LoadData()
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

        TotalFocusTime = TimeSpan.FromMinutes(stats.Sum(s => s.TotalMinutes));
        CompletedSessions = stats.Sum(s => s.SessionsCompleted);
        CompletedTasks = stats.Sum(s => s.TasksCompleted);
        ProductivityScore = CalculateProductivityScore();
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
    private async Task ExportReport()
    {
        await _statisticsService.ExportReportAsync(ReportFormat.Pdf);
    }

    [RelayCommand]
    private async Task UpdateGoals(int daily, int weekly, int monthly)
    {
        DailyGoal = daily;
        WeeklyGoal = weekly;
        MonthlyGoal = monthly;

        await _statisticsService.UpdateGoalsAsync(daily, weekly, monthly);
    }

    private double CalculateProductivityScore()
    {
        if (CompletedSessions == 0) return 0;

        var taskCompletionRate = CompletedTasks > 0 ? (double)CompletedTasks / (CompletedSessions * 3) : 0;
        var timeScore = Math.Min(TotalFocusTime.TotalMinutes / 480, 1.0); // 8 hours max
        var consistencyScore = Math.Min(CompletedSessions / 8.0, 1.0); // 8 sessions max

        return (taskCompletionRate * 0.4 + timeScore * 0.3 + consistencyScore * 0.3) * 100;
    }

    [RelayCommand]
    private void ChangeDate(int daysOffset)
    {
        SelectedDate = SelectedDate.AddDays(daysOffset);
        LoadData();
    }
}

public class DailyStats
{
    public DateTime Date { get; set; }
    public int TotalMinutes { get; set; }
    public int SessionsCompleted { get; set; }
    public int TasksCompleted { get; set; }
    public double ProductivityScore { get; set; }
}

public class Achievement
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime? UnlockedDate { get; set; }
    public int Progress { get; set; }
    public int MaxProgress { get; set; }
    public string Icon { get; set; } = "🏆";
}

public class CategoryStats
{
    public string Category { get; set; } = string.Empty;
    public int TaskCount { get; set; }
    public int CompletedCount { get; set; }
    public TimeSpan TotalTime { get; set; }
    public double CompletionRate => TaskCount > 0 ? (double)CompletedCount / TaskCount : 0;
}

public class StreakInfo
{
    public int CurrentStreak { get; set; }
    public int LongestStreak { get; set; }
    public string MostProductiveTime { get; set; } = "Morning";
}

public class AverageInfo
{
    public double WeeklyAverage { get; set; }
    public double MonthlyAverage { get; set; }
}

public class GoalsInfo
{
    public int DailyGoal { get; set; } = 120; // minutes
    public int WeeklyGoal { get; set; } = 840; // minutes
    public int MonthlyGoal { get; set; } = 3600; // minutes
}

public enum ReportFormat
{
    Pdf,
    Csv,
    Json
}