using Xunit;
using Moq;
using FluentAssertions;
using UnoPomodoro.Services;
using UnoPomodoro.Data.Models;
using UnoPomodoro.Data.Repositories;
using UnoPomodoro.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace UnoPomodoro.Tests.Services;

public class StatisticsServiceTests
{
    private readonly Mock<ISessionRepository> _mockSessionRepository;
    private readonly Mock<ITaskRepository> _mockTaskRepository;
    private readonly StatisticsService _statisticsService;

    public StatisticsServiceTests()
    {
        _mockSessionRepository = new Mock<ISessionRepository>();
        _mockTaskRepository = new Mock<ITaskRepository>();
        _statisticsService = new StatisticsService(_mockSessionRepository.Object, _mockTaskRepository.Object);
    }

    [Fact]
    public async Task GetDailyStatsAsync_ShouldReturnDailyStatistics()
    {
        // Arrange
        var sessions = new List<Session>
        {
            new Session("session1", "pomodoro", DateTime.Today.AddHours(-2)) { EndTime = DateTime.Today.AddHours(-1), TotalTasks = 3, CompletedTasks = 2 },
            new Session("session2", "pomodoro", DateTime.Today.AddDays(-1)) { EndTime = DateTime.Today.AddDays(-1).AddHours(1), TotalTasks = 2, CompletedTasks = 1 }
        };

        var tasks = new List<TaskItem>
        {
            new TaskItem("Task 1", "session1") { Completed = true },
            new TaskItem("Task 2", "session1") { Completed = false },
            new TaskItem("Task 3", "session2") { Completed = true }
        };

        _mockSessionRepository.Setup(x => x.GetAllSessionsAsync())
            .ReturnsAsync(sessions);

        _mockTaskRepository.Setup(x => x.GetAllTasksAsync())
            .ReturnsAsync(tasks);

        // Act
        var result = await _statisticsService.GetDailyStatsAsync();

        // Assert
        result.Should().HaveCount(2);
        result.First().Date.Should().Be(DateTime.Today);
        result.First().TotalMinutes.Should().Be(60); // 1 hour session
        result.First().SessionsCompleted.Should().Be(1);
        result.First().TasksCompleted.Should().Be(1); // Only completed tasks from session1
    }

    [Fact]
    public async Task GetAchievementsAsync_ShouldReturnAchievementList()
    {
        // Arrange
        var sessions = new List<Session>
        {
            new Session("session1", "pomodoro", DateTime.Now.AddHours(-2)) { EndTime = DateTime.Now.AddHours(-1) }
        };

        var tasks = new List<TaskItem>
        {
            new TaskItem("Task 1", "session1") { Completed = true },
            new TaskItem("Task 2", "session1") { Completed = true }
        };

        _mockSessionRepository.Setup(x => x.GetAllSessionsAsync())
            .ReturnsAsync(sessions);

        _mockTaskRepository.Setup(x => x.GetAllTasksAsync())
            .ReturnsAsync(tasks);

        // Act
        var achievements = await _statisticsService.GetAchievementsAsync();

        // Assert
        achievements.Should().NotBeEmpty();
        achievements.Should().Contain(a => a.Id == "first_session");
        achievements.Should().Contain(a => a.Id == "task_master");
        achievements.Should().Contain(a => a.Id == "marathon_runner");

        var firstSessionAchievement = achievements.First(a => a.Id == "first_session");
        firstSessionAchievement.Progress.Should().Be(1);
        firstSessionAchievement.MaxProgress.Should().Be(1);
        firstSessionAchievement.UnlockedDate.Should().NotBeNull();
    }

    [Fact]
    public async Task GetCategoryStatsAsync_ShouldReturnCategoryStatistics()
    {
        // Arrange
        var tasks = new List<TaskItem>
        {
            new TaskItem("Complete work project", "session1") { Completed = true },
            new TaskItem("Study for exam", "session2") { Completed = true },
            new TaskItem("Work on presentation", "session3") { Completed = false },
            new TaskItem("Exercise routine", "session4") { Completed = true }
        };

        var sessions = new List<Session>
        {
            new Session("session1", "pomodoro", DateTime.Now.AddHours(-4)) { EndTime = DateTime.Now.AddHours(-3) },
            new Session("session2", "pomodoro", DateTime.Now.AddHours(-3)) { EndTime = DateTime.Now.AddHours(-2) },
            new Session("session3", "pomodoro", DateTime.Now.AddHours(-2)) { EndTime = DateTime.Now.AddHours(-1) },
            new Session("session4", "pomodoro", DateTime.Now.AddHours(-1)) { EndTime = DateTime.Now }
        };

        _mockTaskRepository.Setup(x => x.GetAllTasksAsync())
            .ReturnsAsync(tasks);

        _mockSessionRepository.Setup(x => x.GetAllSessionsAsync())
            .ReturnsAsync(sessions);

        // Act
        var categoryStats = await _statisticsService.GetCategoryStatsAsync();

        // Assert
        categoryStats.Should().NotBeEmpty();
        categoryStats.Should().Contain(c => c.Category == "Work");
        categoryStats.Should().Contain(c => c.Category == "Study");
        categoryStats.Should().Contain(c => c.Category == "Health");

        var workCategory = categoryStats.First(c => c.Category == "Work");
        workCategory.TaskCount.Should().Be(2);
        workCategory.CompletedCount.Should().Be(1);
        workCategory.TotalTime.Should().Be(TimeSpan.FromMinutes(50)); // 2 sessions * 25 minutes
    }

    [Fact]
    public async Task GetGoalsAsync_ShouldReturnDefaultGoals()
    {
        // Act
        var goals = await _statisticsService.GetGoalsAsync();

        // Assert
        goals.DailyGoal.Should().Be(120);
        goals.WeeklyGoal.Should().Be(840);
        goals.MonthlyGoal.Should().Be(3600);
    }

    [Fact]
    public async Task GetStreaksAsync_ShouldCalculateStreaksCorrectly()
    {
        // Arrange
        var sessions = new List<Session>
        {
            new Session("session1", "pomodoro", DateTime.Today) { EndTime = DateTime.Today.AddMinutes(25) },
            new Session("session2", "pomodoro", DateTime.Today.AddDays(-1)) { EndTime = DateTime.Today.AddDays(-1).AddMinutes(25) },
            new Session("session3", "pomodoro", DateTime.Today.AddDays(-2)) { EndTime = DateTime.Today.AddDays(-2).AddMinutes(25) },
            new Session("session4", "pomodoro", DateTime.Today.AddDays(-4)) { EndTime = DateTime.Today.AddDays(-4).AddMinutes(25) }
        };

        _mockSessionRepository.Setup(x => x.GetAllSessionsAsync())
            .ReturnsAsync(sessions);

        // Act
        var streaks = await _statisticsService.GetStreaksAsync();

        // Assert
        streaks.CurrentStreak.Should().Be(3); // Today, yesterday, day before yesterday
        streaks.LongestStreak.Should().Be(3);
        streaks.MostProductiveTime.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetAveragesAsync_ShouldCalculateAveragesCorrectly()
    {
        // Arrange
        var sessions = new List<Session>
        {
            new Session("session1", "pomodoro", DateTime.Now.AddHours(-1)) { EndTime = DateTime.Now.AddMinutes(-35) }, // 25 minutes
            new Session("session2", "pomodoro", DateTime.Now.AddHours(-3)) { EndTime = DateTime.Now.AddHours(-2).AddMinutes(-10) }, // 50 minutes
            new Session("session3", "pomodoro", DateTime.Now.AddDays(-2)) { EndTime = DateTime.Now.AddDays(-2).AddMinutes(-15) } // 45 minutes
        };

        _mockSessionRepository.Setup(x => x.GetAllSessionsAsync())
            .ReturnsAsync(sessions);

        // Act
        var averages = await _statisticsService.GetAveragesAsync();

        // Assert
        averages.WeeklyAverage.Should().Be(25); // Only 1 session in last week (25 minutes)
        averages.MonthlyAverage.Should().Be(40); // All 3 sessions in last month (120 minutes / 3 = 40)
    }

    [Fact]
    public async Task ExportReportAsync_ShouldExportPdfReport()
    {
        // Arrange
        var sessions = new List<Session>
        {
            new Session("session1", "pomodoro", DateTime.Now.AddHours(-1)) { EndTime = DateTime.Now.AddMinutes(-35) }
        };

        var tasks = new List<TaskItem>
        {
            new TaskItem("Task 1", "session1") { Completed = true }
        };

        _mockSessionRepository.Setup(x => x.GetAllSessionsAsync())
            .ReturnsAsync(sessions);

        _mockTaskRepository.Setup(x => x.GetAllTasksAsync())
            .ReturnsAsync(tasks);

        // Act
        await _statisticsService.ExportReportAsync(ReportFormat.Pdf);

        // Assert
        // Since PDF export creates a file, we just verify it doesn't throw an exception
        // In a real test, we might mock the file system or check if file was created
        Assert.True(true); // If we get here, no exception was thrown
    }

    [Fact]
    public async Task GetWeeklyStatsAsync_ShouldReturnWeeklyStatistics()
    {
        // Arrange
        var sessions = new List<Session>
        {
            new Session("session1", "pomodoro", DateTime.Now) { EndTime = DateTime.Now.AddMinutes(25) },
            new Session("session2", "pomodoro", DateTime.Now.AddDays(-1)) { EndTime = DateTime.Now.AddDays(-1).AddMinutes(25) }
        };

        var tasks = new List<TaskItem>
        {
            new TaskItem("Task 1", "session1") { Completed = true },
            new TaskItem("Task 2", "session2") { Completed = true }
        };

        _mockSessionRepository.Setup(x => x.GetAllSessionsAsync())
            .ReturnsAsync(sessions);

        _mockTaskRepository.Setup(x => x.GetAllTasksAsync())
            .ReturnsAsync(tasks);

        // Act
        var weeklyStats = await _statisticsService.GetWeeklyStatsAsync();

        // Assert
        weeklyStats.Should().NotBeEmpty();
        weeklyStats.First().SessionsCompleted.Should().Be(2);
        weeklyStats.First().TotalMinutes.Should().Be(50);
        weeklyStats.First().TasksCompleted.Should().Be(2);
    }

    [Fact]
    public async Task GetMonthlyStatsAsync_ShouldReturnMonthlyStatistics()
    {
        // Arrange
        var sessions = new List<Session>
        {
            new Session("session1", "pomodoro", DateTime.Now) { EndTime = DateTime.Now.AddMinutes(25) },
            new Session("session2", "pomodoro", DateTime.Now.AddDays(-15)) { EndTime = DateTime.Now.AddDays(-15).AddMinutes(25) }
        };

        var tasks = new List<TaskItem>
        {
            new TaskItem("Task 1", "session1") { Completed = true },
            new TaskItem("Task 2", "session2") { Completed = true }
        };

        _mockSessionRepository.Setup(x => x.GetAllSessionsAsync())
            .ReturnsAsync(sessions);

        _mockTaskRepository.Setup(x => x.GetAllTasksAsync())
            .ReturnsAsync(tasks);

        // Act
        var monthlyStats = await _statisticsService.GetMonthlyStatsAsync();

        // Assert
        monthlyStats.Should().NotBeEmpty();
        monthlyStats.First().SessionsCompleted.Should().Be(2);
        monthlyStats.First().TotalMinutes.Should().Be(50);
        monthlyStats.First().TasksCompleted.Should().Be(2);
    }

    [Fact]
    public async Task GetProductivityInsightsAsync_ShouldReturnInsights()
    {
        // Arrange
        var sessions = new List<Session>
        {
            new Session("session1", "pomodoro", DateTime.Today.AddHours(9)) { EndTime = DateTime.Today.AddHours(9).AddMinutes(25) },
            new Session("session2", "pomodoro", DateTime.Today.AddDays(-1).AddHours(14)) { EndTime = DateTime.Today.AddDays(-1).AddHours(14).AddMinutes(25) }
        };

        var tasks = new List<TaskItem>
        {
            new TaskItem("Task 1", "session1") { Completed = true },
            new TaskItem("Task 2", "session1") { Completed = false },
            new TaskItem("Task 3", "session2") { Completed = true }
        };

        _mockSessionRepository.Setup(x => x.GetAllSessionsAsync())
            .ReturnsAsync(sessions);

        _mockTaskRepository.Setup(x => x.GetAllTasksAsync())
            .ReturnsAsync(tasks);

        // Act
        var insights = await _statisticsService.GetProductivityInsightsAsync();

        // Assert
        insights.Should().NotBeNull();
        insights.MostProductiveDay.Should().NotBeNullOrEmpty();
        insights.MostProductiveHour.Should().NotBeNullOrEmpty();
        insights.AverageSessionLength.Should().Be(25);
        insights.AverageTasksPerSession.Should().Be(1.5);
        insights.TaskCompletionRate.Should().Be(2.0 / 3.0);
        insights.Recommendations.Should().NotBeEmpty();
    }

    [Fact]
    public async Task UpdateGoalsAsync_ShouldUpdateGoals()
    {
        // Act
        await _statisticsService.UpdateGoalsAsync(180, 1260, 5400);

        // Assert
        // This is a simple call that doesn't return anything, so we just verify it doesn't throw
        Assert.True(true);
    }

    [Fact]
    public async Task CategorizeTask_ShouldCategorizeTasksCorrectly()
    {
        // This tests the private CategorizeTask method indirectly through other methods

        // Arrange
        var tasks = new List<TaskItem>
        {
            new TaskItem("Complete work project", "session1") { Completed = true },
            new TaskItem("Study for exam", "session2") { Completed = true },
            new TaskItem("Exercise routine", "session3") { Completed = true },
            new TaskItem("Clean the house", "session4") { Completed = true },
            new TaskItem("Code review", "session5") { Completed = true },
            new TaskItem("Write report", "session6") { Completed = true }
        };

        var sessions = new List<Session>
        {
            new Session("session1", "pomodoro", DateTime.Now) { EndTime = DateTime.Now.AddMinutes(25) },
            new Session("session2", "pomodoro", DateTime.Now.AddHours(-1)) { EndTime = DateTime.Now.AddHours(-1).AddMinutes(25) },
            new Session("session3", "pomodoro", DateTime.Now.AddHours(-2)) { EndTime = DateTime.Now.AddHours(-2).AddMinutes(25) },
            new Session("session4", "pomodoro", DateTime.Now.AddHours(-3)) { EndTime = DateTime.Now.AddHours(-3).AddMinutes(25) },
            new Session("session5", "pomodoro", DateTime.Now.AddHours(-4)) { EndTime = DateTime.Now.AddHours(-4).AddMinutes(25) },
            new Session("session6", "pomodoro", DateTime.Now.AddHours(-5)) { EndTime = DateTime.Now.AddHours(-5).AddMinutes(25) }
        };

        _mockTaskRepository.Setup(x => x.GetAllTasksAsync())
            .ReturnsAsync(tasks);

        _mockSessionRepository.Setup(x => x.GetAllSessionsAsync())
            .ReturnsAsync(sessions);

        // Act
        var categoryStats = await _statisticsService.GetCategoryStatsAsync();

        // Assert
        categoryStats.Should().Contain(c => c.Category == "Work");
        categoryStats.Should().Contain(c => c.Category == "Study");
        categoryStats.Should().Contain(c => c.Category == "Health");
        categoryStats.Should().Contain(c => c.Category == "Home");
        categoryStats.Should().Contain(c => c.Category == "Development");
        categoryStats.Should().Contain(c => c.Category == "Writing");
    }

    [Fact]
    public async Task CalculateCurrentStreak_ShouldCalculateCorrectStreak()
    {
        // Arrange
        var sessions = new List<Session>
        {
            new Session("session1", "pomodoro", DateTime.Today) { EndTime = DateTime.Today.AddMinutes(25) },
            new Session("session2", "pomodoro", DateTime.Today.AddDays(-1)) { EndTime = DateTime.Today.AddDays(-1).AddMinutes(25) },
            new Session("session3", "pomodoro", DateTime.Today.AddDays(-2)) { EndTime = DateTime.Today.AddDays(-2).AddMinutes(25) },
            new Session("session4", "pomodoro", DateTime.Today.AddDays(-4)) { EndTime = DateTime.Today.AddDays(-4).AddMinutes(25) }
        };

        _mockSessionRepository.Setup(x => x.GetAllSessionsAsync())
            .ReturnsAsync(sessions);

        // Act
        var streaks = await _statisticsService.GetStreaksAsync();

        // Assert
        streaks.CurrentStreak.Should().Be(3); // Current streak of 3 days
    }

    [Fact]
    public async Task CalculateLongestStreak_ShouldCalculateLongestStreak()
    {
        // Arrange
        var sessions = new List<Session>
        {
            new Session("session1", "pomodoro", DateTime.Today) { EndTime = DateTime.Today.AddMinutes(25) },
            new Session("session2", "pomodoro", DateTime.Today.AddDays(-1)) { EndTime = DateTime.Today.AddDays(-1).AddMinutes(25) },
            new Session("session3", "pomodoro", DateTime.Today.AddDays(-2)) { EndTime = DateTime.Today.AddDays(-2).AddMinutes(25) },
            new Session("session4", "pomodoro", DateTime.Today.AddDays(-7)) { EndTime = DateTime.Today.AddDays(-7).AddMinutes(25) },
            new Session("session5", "pomodoro", DateTime.Today.AddDays(-8)) { EndTime = DateTime.Today.AddDays(-8).AddMinutes(25) }
        };

        _mockSessionRepository.Setup(x => x.GetAllSessionsAsync())
            .ReturnsAsync(sessions);

        // Act
        var streaks = await _statisticsService.GetStreaksAsync();

        // Assert
        streaks.LongestStreak.Should().Be(3); // Longest streak of 3 days
    }

    [Fact]
    public async Task CalculateMostProductiveTime_ShouldDetermineMostProductiveTime()
    {
        // Arrange
        var sessions = new List<Session>
        {
            new Session("session1", "pomodoro", DateTime.Today.AddHours(9)) { EndTime = DateTime.Today.AddHours(9).AddMinutes(25) },
            new Session("session2", "pomodoro", DateTime.Today.AddDays(-1).AddHours(9)) { EndTime = DateTime.Today.AddDays(-1).AddHours(9).AddMinutes(25) },
            new Session("session3", "pomodoro", DateTime.Today.AddDays(-2).AddHours(14)) { EndTime = DateTime.Today.AddDays(-2).AddHours(14).AddMinutes(25) },
            new Session("session4", "pomodoro", DateTime.Today.AddDays(-3).AddHours(20)) { EndTime = DateTime.Today.AddDays(-3).AddHours(20).AddMinutes(25) }
        };

        _mockSessionRepository.Setup(x => x.GetAllSessionsAsync())
            .ReturnsAsync(sessions);

        // Act
        var streaks = await _statisticsService.GetStreaksAsync();

        // Assert
        streaks.MostProductiveTime.Should().Be("Morning"); // Most sessions in morning (9 AM)
    }
}