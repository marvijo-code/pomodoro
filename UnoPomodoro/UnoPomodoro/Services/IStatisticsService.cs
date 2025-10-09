using System.Collections.Generic;
using System.Threading.Tasks;
using UnoPomodoro.ViewModels;

namespace UnoPomodoro.Services;

public interface IStatisticsService
{
    Task<List<DailyStats>> GetDailyStatsAsync();
    Task<List<Achievement>> GetAchievementsAsync();
    Task<List<CategoryStats>> GetCategoryStatsAsync();
    Task<GoalsInfo> GetGoalsAsync();
    Task<StreakInfo> GetStreaksAsync();
    Task<AverageInfo> GetAveragesAsync();
    Task ExportReportAsync(ReportFormat format);
    Task UpdateGoalsAsync(int daily, int weekly, int monthly);
    Task<List<WeeklyStats>> GetWeeklyStatsAsync();
    Task<List<MonthlyStats>> GetMonthlyStatsAsync();
    Task<ProductivityInsights> GetProductivityInsightsAsync();
}

public class WeeklyStats
{
    public int WeekNumber { get; set; }
    public int Year { get; set; }
    public int TotalMinutes { get; set; }
    public int SessionsCompleted { get; set; }
    public int TasksCompleted { get; set; }
    public double ProductivityScore { get; set; }
}

public class MonthlyStats
{
    public int Month { get; set; }
    public int Year { get; set; }
    public int TotalMinutes { get; set; }
    public int SessionsCompleted { get; set; }
    public int TasksCompleted { get; set; }
    public double ProductivityScore { get; set; }
}

public class ProductivityInsights
{
    public string MostProductiveDay { get; set; } = "Monday";
    public string MostProductiveHour { get; set; } = "09:00";
    public double AverageSessionLength { get; set; }
    public int AverageTasksPerSession { get; set; }
    public double TaskCompletionRate { get; set; }
    public List<string> Recommendations { get; set; } = new();
}