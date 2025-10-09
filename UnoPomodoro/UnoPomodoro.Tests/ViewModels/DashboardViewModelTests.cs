using Xunit;
using Moq;
using FluentAssertions;
using UnoPomodoro.ViewModels;
using UnoPomodoro.Data.Models;
using UnoPomodoro.Data.Repositories;
using UnoPomodoro.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace UnoPomodoro.Tests.ViewModels;

public class DashboardViewModelTests
{
    private readonly Mock<ISessionRepository> _mockSessionRepository;
    private readonly Mock<ITaskRepository> _mockTaskRepository;
    private readonly Mock<IStatisticsService> _mockStatisticsService;
    private readonly DashboardViewModel _viewModel;

    public DashboardViewModelTests()
    {
        _mockSessionRepository = new Mock<ISessionRepository>();
        _mockTaskRepository = new Mock<ITaskRepository>();
        _mockStatisticsService = new Mock<IStatisticsService>();

        _viewModel = new DashboardViewModel(
            _mockSessionRepository.Object,
            _mockTaskRepository.Object,
            _mockStatisticsService.Object);
    }

    [Fact]
    public void Constructor_ShouldInitializeWithDefaultValues()
    {
        // Assert
        _viewModel.SelectedDate.Should().Be(DateTime.Today);
        _viewModel.TotalFocusTime.Should().Be(TimeSpan.Zero);
        _viewModel.CompletedSessions.Should().Be(0);
        _viewModel.CompletedTasks.Should().Be(0);
        _viewModel.ProductivityScore.Should().Be(0);
        _viewModel.DailyGoal.Should().Be(120);
        _viewModel.WeeklyGoal.Should().Be(840);
        _viewModel.MonthlyGoal.Should().Be(3600);
        _viewModel.CurrentStreak.Should().Be(0);
        _viewModel.LongestStreak.Should().Be(0);
        _viewModel.MostProductiveTime.Should().Be("Morning");
        _viewModel.WeeklyAverage.Should().Be(0);
        _viewModel.MonthlyAverage.Should().Be(0);
        _viewModel.DailyStats.Should().BeEmpty();
        _viewModel.RecentSessions.Should().BeEmpty();
        _viewModel.Achievements.Should().BeEmpty();
        _viewModel.CategoryStats.Should().BeEmpty();
    }

    [Fact]
    public async Task LoadDailyStats_ShouldUpdateStatistics()
    {
        // Arrange
        var expectedStats = new List<DailyStats>
        {
            new DailyStats { Date = DateTime.Today, TotalMinutes = 120, SessionsCompleted = 4, TasksCompleted = 8, ProductivityScore = 85.5 },
            new DailyStats { Date = DateTime.Today.AddDays(-1), TotalMinutes = 90, SessionsCompleted = 3, TasksCompleted = 6, ProductivityScore = 75.0 }
        };

        _mockStatisticsService.Setup(x => x.GetDailyStatsAsync())
            .ReturnsAsync(expectedStats);

        // Act
        await _viewModel.LoadDailyStatsCommand.ExecuteAsync(null);

        // Assert
        _viewModel.DailyStats.Should().HaveCount(2);
        _viewModel.TotalFocusTime.Should().Be(TimeSpan.FromMinutes(210));
        _viewModel.CompletedSessions.Should().Be(7);
        _viewModel.CompletedTasks.Should().Be(14);
        _viewModel.ProductivityScore.Should().BeGreaterThan(0);
        _mockStatisticsService.Verify(x => x.GetDailyStatsAsync(), Times.Once);
    }

    [Fact]
    public async Task LoadRecentSessions_ShouldUpdateRecentSessions()
    {
        // Arrange
        var expectedSessions = new List<Session>
        {
            new Session("session1", "pomodoro", DateTime.Now.AddHours(-2)) { EndTime = DateTime.Now.AddHours(-1), TotalTasks = 3, CompletedTasks = 2 },
            new Session("session2", "shortBreak", DateTime.Now.AddHours(-4)) { EndTime = DateTime.Now.AddHours(-3), TotalTasks = 1, CompletedTasks = 1 }
        };

        _mockSessionRepository.Setup(x => x.GetRecentSessionsAsync(10))
            .ReturnsAsync(expectedSessions);

        // Act
        await _viewModel.LoadRecentSessionsCommand.ExecuteAsync(null);

        // Assert
        _viewModel.RecentSessions.Should().HaveCount(2);
        _viewModel.RecentSessions.Should().BeEquivalentTo(expectedSessions);
        _mockSessionRepository.Verify(x => x.GetRecentSessionsAsync(10), Times.Once);
    }

    [Fact]
    public async Task LoadAchievements_ShouldUpdateAchievements()
    {
        // Arrange
        var expectedAchievements = new List<Achievement>
        {
            new Achievement
            {
                Id = "first_session",
                Title = "First Steps",
                Description = "Complete your first Pomodoro session",
                Icon = "🎯",
                MaxProgress = 1,
                Progress = 1,
                UnlockedDate = DateTime.Now
            },
            new Achievement
            {
                Id = "task_master",
                Title = "Task Master",
                Description = "Complete 50 tasks",
                Icon = "📋",
                MaxProgress = 50,
                Progress = 25
            }
        };

        _mockStatisticsService.Setup(x => x.GetAchievementsAsync())
            .ReturnsAsync(expectedAchievements);

        // Act
        await _viewModel.LoadAchievementsCommand.ExecuteAsync(null);

        // Assert
        _viewModel.Achievements.Should().HaveCount(2);
        _viewModel.Achievements.Should().BeEquivalentTo(expectedAchievements);
        _mockStatisticsService.Verify(x => x.GetAchievementsAsync(), Times.Once);
    }

    [Fact]
    public async Task LoadCategoryStats_ShouldUpdateCategoryStatistics()
    {
        // Arrange
        var expectedStats = new List<CategoryStats>
        {
            new CategoryStats { Category = "Work", TaskCount = 10, CompletedCount = 8, TotalTime = TimeSpan.FromMinutes(120) },
            new CategoryStats { Category = "Study", TaskCount = 5, CompletedCount = 3, TotalTime = TimeSpan.FromMinutes(60) }
        };

        _mockStatisticsService.Setup(x => x.GetCategoryStatsAsync())
            .ReturnsAsync(expectedStats);

        // Act
        await _viewModel.LoadCategoryStatsCommand.ExecuteAsync(null);

        // Assert
        _viewModel.CategoryStats.Should().HaveCount(2);
        _viewModel.CategoryStats.Should().BeEquivalentTo(expectedStats);
        _mockStatisticsService.Verify(x => x.GetCategoryStatsAsync(), Times.Once);
    }

    [Fact]
    public async Task LoadGoals_ShouldUpdateGoalValues()
    {
        // Arrange
        var expectedGoals = new GoalsInfo
        {
            DailyGoal = 180,
            WeeklyGoal = 1260,
            MonthlyGoal = 5400
        };

        _mockStatisticsService.Setup(x => x.GetGoalsAsync())
            .ReturnsAsync(expectedGoals);

        // Act
        await _viewModel.LoadGoalsCommand.ExecuteAsync(null);

        // Assert
        _viewModel.DailyGoal.Should().Be(180);
        _viewModel.WeeklyGoal.Should().Be(1260);
        _viewModel.MonthlyGoal.Should().Be(5400);
        _mockStatisticsService.Verify(x => x.GetGoalsAsync(), Times.Once);
    }

    [Fact]
    public async Task LoadStreaks_ShouldUpdateStreakInformation()
    {
        // Arrange
        var expectedStreaks = new StreakInfo
        {
            CurrentStreak = 5,
            LongestStreak = 12,
            MostProductiveTime = "Afternoon"
        };

        _mockStatisticsService.Setup(x => x.GetStreaksAsync())
            .ReturnsAsync(expectedStreaks);

        // Act
        await _viewModel.LoadStreaksCommand.ExecuteAsync(null);

        // Assert
        _viewModel.CurrentStreak.Should().Be(5);
        _viewModel.LongestStreak.Should().Be(12);
        _viewModel.MostProductiveTime.Should().Be("Afternoon");
        _mockStatisticsService.Verify(x => x.GetStreaksAsync(), Times.Once);
    }

    [Fact]
    public async Task LoadAverages_ShouldUpdateAverageValues()
    {
        // Arrange
        var expectedAverages = new AverageInfo
        {
            WeeklyAverage = 25.5,
            MonthlyAverage = 23.2
        };

        _mockStatisticsService.Setup(x => x.GetAveragesAsync())
            .ReturnsAsync(expectedAverages);

        // Act
        await _viewModel.LoadAveragesCommand.ExecuteAsync(null);

        // Assert
        _viewModel.WeeklyAverage.Should().Be(25.5);
        _viewModel.MonthlyAverage.Should().Be(23.2);
        _mockStatisticsService.Verify(x => x.GetAveragesAsync(), Times.Once);
    }

    [Fact]
    public async Task ExportReport_ShouldExportPdfReport()
    {
        // Act
        await _viewModel.ExportReportCommand.ExecuteAsync(ReportFormat.Pdf);

        // Assert
        _mockStatisticsService.Verify(x => x.ExportReportAsync(ReportFormat.Pdf), Times.Once);
    }

    [Fact]
    public async Task UpdateGoals_ShouldUpdateGoalValues()
    {
        // Arrange
        var newDailyGoal = 240;
        var newWeeklyGoal = 1680;
        var newMonthlyGoal = 7200;

        _mockStatisticsService.Setup(x => x.UpdateGoalsAsync(newDailyGoal, newWeeklyGoal, newMonthlyGoal))
            .Returns(Task.CompletedTask);

        // Act
        await _viewModel.UpdateGoalsCommand.ExecuteAsync((newDailyGoal, newWeeklyGoal, newMonthlyGoal));

        // Assert
        _viewModel.DailyGoal.Should().Be(newDailyGoal);
        _viewModel.WeeklyGoal.Should().Be(newWeeklyGoal);
        _viewModel.MonthlyGoal.Should().Be(newMonthlyGoal);
        _mockStatisticsService.Verify(x => x.UpdateGoalsAsync(newDailyGoal, newWeeklyGoal, newMonthlyGoal), Times.Once);
    }

    [Fact]
    public void ChangeDate_ShouldUpdateSelectedDateAndReloadData()
    {
        // Arrange
        var initialDate = _viewModel.SelectedDate;

        // Act
        _viewModel.ChangeDateCommand.Execute(1);

        // Assert
        _viewModel.SelectedDate.Should().Be(initialDate.AddDays(1));
    }

    [Fact]
    public void CalculateProductivityScore_ShouldCalculateCorrectScore()
    {
        // Arrange - This tests the private method indirectly through LoadDailyStats
        var stats = new List<DailyStats>
        {
            new DailyStats { Date = DateTime.Today, TotalMinutes = 240, SessionsCompleted = 6, TasksCompleted = 12, ProductivityScore = 90.0 }
        };

        _mockStatisticsService.Setup(x => x.GetDailyStatsAsync())
            .ReturnsAsync(stats);

        // Act
        _viewModel.LoadDailyStatsCommand.Execute(null);

        // Assert
        _viewModel.ProductivityScore.Should().BeGreaterThan(80); // Should be high for good stats
    }
}