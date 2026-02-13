using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using UnoPomodoro.Data.Models;
using UnoPomodoro.Data.Repositories;
using System.Globalization;
using UnoPomodoro.ViewModels;

namespace UnoPomodoro.Services;

public class StatisticsService : IStatisticsService
{
    private readonly ISessionRepository _sessionRepository;
    private readonly ITaskRepository _taskRepository;
    private readonly ISettingsService? _settingsService;

    private int _dailyGoal = 120;
    private int _weeklyGoal = 840;
    private int _monthlyGoal = 3600;

    public StatisticsService(
        ISessionRepository sessionRepository,
        ITaskRepository taskRepository)
    {
        _sessionRepository = sessionRepository;
        _taskRepository = taskRepository;
    }

    public StatisticsService(
        ISessionRepository sessionRepository,
        ITaskRepository taskRepository,
        ISettingsService settingsService)
    {
        _sessionRepository = sessionRepository;
        _taskRepository = taskRepository;
        _settingsService = settingsService;
    }

    public async Task<List<DailyStats>> GetDailyStatsAsync()
    {
        var sessions = await _sessionRepository.GetAllSessionsAsync();
        var tasks = await _taskRepository.GetAllTasksAsync();

        var dailyStats = sessions
            .GroupBy(s => s.StartTime.Date)
            .Select(g => new DailyStats
            {
                Date = g.Key,
                TotalMinutes = g.Sum(s => (int)((s.EndTime - s.StartTime)?.TotalMinutes ?? 0)),
                SessionsCompleted = g.Count(s => s.EndTime.HasValue),
                TasksCompleted = tasks.Count(t => t.Completed && g.Any(s => s.Id == t.SessionId)),
                ProductivityScore = CalculateDailyProductivityScore(g.ToList(), tasks.Where(t => g.Any(s => s.Id == t.SessionId)).ToList())
            })
            .OrderByDescending(s => s.Date)
            .Take(30)
            .ToList();

        return dailyStats;
    }

    public async Task<List<Achievement>> GetAchievementsAsync()
    {
        var sessions = await _sessionRepository.GetAllSessionsAsync();
        var tasks = await _taskRepository.GetAllTasksAsync();

        var achievements = new List<Achievement>();

        // First Session
        achievements.Add(new Achievement
        {
            Id = "first_session",
            Title = "First Steps",
            Description = "Complete your first Pomodoro session",
            Icon = "🎯",
            MaxProgress = 1,
            Progress = sessions.Count > 0 ? 1 : 0,
            UnlockedDate = sessions.FirstOrDefault()?.StartTime
        });

        // Early Bird
        achievements.Add(new Achievement
        {
            Id = "early_bird",
            Title = "Early Bird",
            Description = "Complete a session before 9 AM",
            Icon = "🌅",
            MaxProgress = 1,
            Progress = sessions.Any(s => s.StartTime.Hour < 9) ? 1 : 0,
            UnlockedDate = sessions.FirstOrDefault(s => s.StartTime.Hour < 9)?.StartTime
        });

        // Task Master
        achievements.Add(new Achievement
        {
            Id = "task_master",
            Title = "Task Master",
            Description = "Complete 50 tasks",
            Icon = "📋",
            MaxProgress = 50,
            Progress = tasks.Count(t => t.Completed)
        });

        // Marathon Runner
        achievements.Add(new Achievement
        {
            Id = "marathon_runner",
            Title = "Marathon Runner",
            Description = "Complete 100 sessions",
            Icon = "🏃",
            MaxProgress = 100,
            Progress = sessions.Count
        });

        // Week Warrior
        achievements.Add(new Achievement
        {
            Id = "week_warrior",
            Title = "Week Warrior",
            Description = "Complete sessions 7 days in a row",
            Icon = "🗓️",
            MaxProgress = 7,
            Progress = CalculateCurrentStreak(sessions)
        });

        // Productivity Champion
        achievements.Add(new Achievement
        {
            Id = "productivity_champion",
            Title = "Productivity Champion",
            Description = "Achieve 90%+ productivity score for a week",
            Icon = "🏆",
            MaxProgress = 1,
            Progress = await CheckHighProductivityWeekAsync(sessions, tasks) ? 1 : 0
        });

        return achievements;
    }

    public async Task<List<CategoryStats>> GetCategoryStatsAsync()
    {
        var tasks = await _taskRepository.GetAllTasksAsync();
        var sessions = await _sessionRepository.GetAllSessionsAsync();

        // Simple categorization based on task content
        var categoryStats = tasks
            .GroupBy(t => CategorizeTask(t.Text))
            .Select(g => new CategoryStats
            {
                Category = g.Key,
                TaskCount = g.Count(),
                CompletedCount = g.Count(t => t.Completed),
                TotalTime = TimeSpan.FromMinutes(sessions
                    .Where(s => g.Any(t => t.SessionId == s.Id))
                    .Sum(s => (int)((s.EndTime - s.StartTime)?.TotalMinutes ?? 0)))
            })
            .ToList();

        return categoryStats;
    }

    public async Task<GoalsInfo> GetGoalsAsync()
    {
        if (_settingsService != null)
        {
            await _settingsService.LoadAsync();
            return new GoalsInfo
            {
                DailyGoal = _settingsService.DailyGoal,
                WeeklyGoal = _settingsService.WeeklyGoal,
                MonthlyGoal = _settingsService.MonthlyGoal
            };
        }

        return new GoalsInfo
        {
            DailyGoal = _dailyGoal,
            WeeklyGoal = _weeklyGoal,
            MonthlyGoal = _monthlyGoal
        };
    }

    public async Task<StreakInfo> GetStreaksAsync()
    {
        var sessions = await _sessionRepository.GetAllSessionsAsync();

        var currentStreak = CalculateCurrentStreak(sessions);
        var longestStreak = CalculateLongestStreak(sessions);
        var mostProductiveTime = CalculateMostProductiveTime(sessions);

        return new StreakInfo
        {
            CurrentStreak = currentStreak,
            LongestStreak = longestStreak,
            MostProductiveTime = mostProductiveTime
        };
    }

    public async Task<AverageInfo> GetAveragesAsync()
    {
        var sessions = await _sessionRepository.GetAllSessionsAsync();
        var today = DateTime.Today;

        var weeklySessions = sessions.Where(s => s.StartTime >= today.AddDays(-7)).ToList();
        var monthlySessions = sessions.Where(s => s.StartTime >= today.AddDays(-30)).ToList();

        return new AverageInfo
        {
            WeeklyAverage = weeklySessions.Any() ?
                weeklySessions.Average(s => (s.EndTime - s.StartTime)?.TotalMinutes ?? 0) : 0,
            MonthlyAverage = monthlySessions.Any() ?
                monthlySessions.Average(s => (s.EndTime - s.StartTime)?.TotalMinutes ?? 0) : 0
        };
    }

    public async Task ExportReportAsync(ReportFormat format)
    {
        var sessions = await _sessionRepository.GetAllSessionsAsync();
        var tasks = await _taskRepository.GetAllTasksAsync();

        var report = new
        {
            GeneratedAt = DateTime.UtcNow,
            TotalSessions = sessions.Count,
            CompletedTasks = tasks.Count(t => t.Completed),
            TotalFocusTime = TimeSpan.FromMinutes(sessions.Sum(s => (int)((s.EndTime - s.StartTime)?.TotalMinutes ?? 0))),
            AverageSessionLength = sessions.Any() ?
                sessions.Average(s => (s.EndTime - s.StartTime)?.TotalMinutes ?? 0) : 0,
            Sessions = sessions,
            Tasks = tasks
        };

        var fileName = $"pomodoro_report_{DateTime.Now:yyyyMMdd_HHmmss}";
        var outputDir = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var filePath = Path.Combine(outputDir, fileName);

        switch (format)
        {
            case ReportFormat.Json:
                var json = JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true });
                await File.WriteAllTextAsync($"{filePath}.json", json);
                break;
            case ReportFormat.Csv:
                await ExportToCsv(report, filePath);
                break;
            case ReportFormat.Pdf:
                // PDF export would require a PDF library
                // For now, we'll create a detailed text report
                await ExportToTextReport(report, filePath);
                break;
        }
    }

    public async Task UpdateGoalsAsync(int daily, int weekly, int monthly)
    {
        _dailyGoal = daily;
        _weeklyGoal = weekly;
        _monthlyGoal = monthly;

        if (_settingsService != null)
        {
            _settingsService.DailyGoal = daily;
            _settingsService.WeeklyGoal = weekly;
            _settingsService.MonthlyGoal = monthly;
            await _settingsService.SaveAsync();
        }
    }

    public async Task<List<WeeklyStats>> GetWeeklyStatsAsync()
    {
        var sessions = await _sessionRepository.GetAllSessionsAsync();
        var tasks = await _taskRepository.GetAllTasksAsync();

        return sessions
            .GroupBy(s => new { Week = ISOWeek.GetWeekOfYear(s.StartTime), Year = s.StartTime.Year })
            .Select(g => new WeeklyStats
            {
                WeekNumber = g.Key.Week,
                Year = g.Key.Year,
                TotalMinutes = g.Sum(s => (int)((s.EndTime - s.StartTime)?.TotalMinutes ?? 0)),
                SessionsCompleted = g.Count(s => s.EndTime.HasValue),
                TasksCompleted = tasks.Count(t => t.Completed && g.Any(s => s.Id == t.SessionId)),
                ProductivityScore = CalculateWeeklyProductivityScore(g.ToList(), tasks.Where(t => g.Any(s => s.Id == t.SessionId)).ToList())
            })
            .OrderByDescending(w => w.Year)
            .ThenByDescending(w => w.WeekNumber)
            .Take(12)
            .ToList();
    }

    public async Task<List<MonthlyStats>> GetMonthlyStatsAsync()
    {
        var sessions = await _sessionRepository.GetAllSessionsAsync();
        var tasks = await _taskRepository.GetAllTasksAsync();

        return sessions
            .GroupBy(s => new { Month = s.StartTime.Month, Year = s.StartTime.Year })
            .Select(g => new MonthlyStats
            {
                Month = g.Key.Month,
                Year = g.Key.Year,
                TotalMinutes = g.Sum(s => (int)((s.EndTime - s.StartTime)?.TotalMinutes ?? 0)),
                SessionsCompleted = g.Count(s => s.EndTime.HasValue),
                TasksCompleted = tasks.Count(t => t.Completed && g.Any(s => s.Id == t.SessionId)),
                ProductivityScore = CalculateMonthlyProductivityScore(g.ToList(), tasks.Where(t => g.Any(s => s.Id == t.SessionId)).ToList())
            })
            .OrderByDescending(m => m.Year)
            .ThenByDescending(m => m.Month)
            .Take(12)
            .ToList();
    }

    public async Task<ProductivityInsights> GetProductivityInsightsAsync()
    {
        var sessions = await _sessionRepository.GetAllSessionsAsync();
        var tasks = await _taskRepository.GetAllTasksAsync();

        var mostProductiveDay = sessions
            .GroupBy(s => s.StartTime.DayOfWeek)
            .OrderByDescending(g => g.Count())
            .FirstOrDefault()?.Key.ToString() ?? "Monday";

        var mostProductiveHour = sessions
            .GroupBy(s => s.StartTime.Hour)
            .OrderByDescending(g => g.Count())
            .FirstOrDefault()?.Key.ToString("00") + ":00";

        var averageSessionLength = sessions.Any() ?
            sessions.Average(s => (s.EndTime - s.StartTime)?.TotalMinutes ?? 0) : 0;

        var averageTasksPerSession = sessions.Any() ?
            tasks.GroupBy(t => t.SessionId).Average(g => g.Count()) : 0;

        var taskCompletionRate = tasks.Any() ?
            (double)tasks.Count(t => t.Completed) / tasks.Count : 0;

        var recommendations = GenerateRecommendations(sessions, tasks);

        return new ProductivityInsights
        {
            MostProductiveDay = mostProductiveDay,
            MostProductiveHour = mostProductiveHour,
            AverageSessionLength = averageSessionLength,
            AverageTasksPerSession = averageTasksPerSession,
            TaskCompletionRate = taskCompletionRate,
            Recommendations = recommendations
        };
    }

    private double CalculateDailyProductivityScore(List<Session> sessions, List<TaskItem> tasks)
    {
        if (sessions.Count == 0) return 0;

        var sessionScore = Math.Min(sessions.Count / 8.0, 1.0) * 40; // Max 40 points
        var taskScore = tasks.Any() ? Math.Min((double)tasks.Count(t => t.Completed) / tasks.Count, 1.0) * 40 : 0; // Max 40 points
        var timeScore = Math.Min(sessions.Sum(s => ((s.EndTime - s.StartTime)?.TotalMinutes ?? 0)) / 480, 1.0) * 20; // Max 20 points

        return sessionScore + taskScore + timeScore;
    }

    private double CalculateWeeklyProductivityScore(List<Session> sessions, List<TaskItem> tasks)
    {
        if (sessions.Count == 0) return 0;
        return CalculateDailyProductivityScore(sessions, tasks);
    }

    private double CalculateMonthlyProductivityScore(List<Session> sessions, List<TaskItem> tasks)
    {
        if (sessions.Count == 0) return 0;
        return CalculateDailyProductivityScore(sessions, tasks);
    }

    private int CalculateCurrentStreak(List<Session> sessions)
    {
        var completedSessions = sessions.Where(s => s.EndTime.HasValue).ToList();
        if (completedSessions.Count == 0) return 0;

        var uniqueDays = completedSessions.Select(s => s.StartTime.Date).Distinct().OrderByDescending(d => d).ToList();
        var streak = 0;
        var currentDate = DateTime.Today;

        // Allow starting from yesterday if no session today
        if (uniqueDays.Count > 0 && uniqueDays[0] < currentDate)
        {
            currentDate = currentDate.AddDays(-1);
        }

        foreach (var day in uniqueDays)
        {
            if (day == currentDate)
            {
                streak++;
                currentDate = day.AddDays(-1);
            }
            else
            {
                break;
            }
        }

        return streak;
    }

    private int CalculateLongestStreak(List<Session> sessions)
    {
        var completedSessions = sessions.Where(s => s.EndTime.HasValue).ToList();
        if (completedSessions.Count == 0) return 0;

        var uniqueDays = completedSessions.Select(s => s.StartTime.Date).Distinct().OrderBy(d => d).ToList();
        var longestStreak = 0;
        var currentStreak = 0;
        var lastDate = DateTime.MinValue;

        foreach (var day in uniqueDays)
        {
            if (day == lastDate.AddDays(1) || lastDate == DateTime.MinValue)
            {
                currentStreak++;
                lastDate = day;
                longestStreak = Math.Max(longestStreak, currentStreak);
            }
            else
            {
                currentStreak = 1;
                lastDate = day;
            }
        }

        return longestStreak;
    }

    private string CalculateMostProductiveTime(List<Session> sessions)
    {
        if (sessions.Count == 0) return "Morning";

        var morningSessions = sessions.Count(s => s.StartTime.Hour >= 6 && s.StartTime.Hour < 12);
        var afternoonSessions = sessions.Count(s => s.StartTime.Hour >= 12 && s.StartTime.Hour < 18);
        var eveningSessions = sessions.Count(s => s.StartTime.Hour >= 18 && s.StartTime.Hour < 24);
        var nightSessions = sessions.Count(s => s.StartTime.Hour >= 0 && s.StartTime.Hour < 6);

        var maxCount = new[] { morningSessions, afternoonSessions, eveningSessions, nightSessions }.Max();

        if (maxCount == morningSessions) return "Morning";
        if (maxCount == afternoonSessions) return "Afternoon";
        if (maxCount == eveningSessions) return "Evening";
        return "Night";
    }

    private string CategorizeTask(string taskText)
    {
        var text = taskText.ToLower();

        if (text.Contains("work") || text.Contains("project") || text.Contains("task")) return "Work";
        if (text.Contains("study") || text.Contains("learn") || text.Contains("read")) return "Study";
        if (text.Contains("exercise") || text.Contains("workout") || text.Contains("health")) return "Health";
        if (text.Contains("clean") || text.Contains("organize") || text.Contains("home")) return "Home";
        if (text.Contains("code") || text.Contains("program") || text.Contains("develop")) return "Development";
        if (text.Contains("write") || text.Contains("document") || text.Contains("report")) return "Writing";

        return "General";
    }

    private List<string> GenerateRecommendations(List<Session> sessions, List<TaskItem> tasks)
    {
        var recommendations = new List<string>();

        var averageSessionLength = sessions.Any() ?
            sessions.Average(s => (s.EndTime - s.StartTime)?.TotalMinutes ?? 0) : 0;

        if (averageSessionLength < 20)
        {
            recommendations.Add("Try to maintain focus for longer sessions to improve productivity");
        }

        var taskCompletionRate = tasks.Any() ?
            (double)tasks.Count(t => t.Completed) / tasks.Count : 0;

        if (taskCompletionRate < 0.7)
        {
            recommendations.Add("Break down large tasks into smaller, manageable pieces");
        }

        var recentActivity = sessions.Count(s => s.StartTime >= DateTime.Now.AddDays(-7));
        if (recentActivity < 5)
        {
            recommendations.Add("Maintain consistency with regular Pomodoro sessions");
        }

        if (recommendations.Count == 0)
        {
            recommendations.Add("Great job! Keep up the excellent work habits");
        }

        return recommendations;
    }

    private async Task ExportToCsv(object report, string fileName)
    {
        // Simplified CSV export implementation
        var csvContent = "Date,Duration,Mode,Tasks Completed\n";

        await File.WriteAllTextAsync($"{fileName}.csv", csvContent);
    }

    private async Task ExportToTextReport(object report, string fileName)
    {
        var reportContent = $@"
POMODORO PRODUCTIVITY REPORT
Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}

=== SUMMARY ===
Total Sessions: {report.GetType().GetProperty("TotalSessions")?.GetValue(report)}
Completed Tasks: {report.GetType().GetProperty("CompletedTasks")?.GetValue(report)}
Total Focus Time: {report.GetType().GetProperty("TotalFocusTime")?.GetValue(report)}
Average Session Length: {report.GetType().GetProperty("AverageSessionLength")?.GetValue(report):F1} minutes

=== RECOMMENDATIONS ===
- Maintain consistent daily sessions
- Take regular breaks to avoid burnout
- Break down complex tasks into smaller ones
- Review and adjust your goals regularly

=== END REPORT ===
";

        await File.WriteAllTextAsync($"{fileName}.txt", reportContent);
    }

    private async Task<bool> CheckHighProductivityWeekAsync(List<Session> sessions, List<TaskItem> tasks)
    {
        var lastWeekSessions = sessions.Where(s => s.StartTime >= DateTime.Now.AddDays(-7)).ToList();
        var lastWeekTasks = tasks.Where(t => lastWeekSessions.Any(s => s.Id == t.SessionId)).ToList();

        if (lastWeekSessions.Count == 0) return false;

        var productivityScore = CalculateWeeklyProductivityScore(lastWeekSessions, lastWeekTasks);
        return productivityScore >= 90;
    }
}