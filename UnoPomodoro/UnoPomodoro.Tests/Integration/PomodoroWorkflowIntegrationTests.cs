using Xunit;
using Moq;
using FluentAssertions;
using UnoPomodoro.ViewModels;
using UnoPomodoro.Data.Models;
using UnoPomodoro.Data.Repositories;
using UnoPomodoro.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace UnoPomodoro.Tests.Integration;

public class PomodoroWorkflowIntegrationTests
{
    private readonly Mock<ITimerService> _mockTimerService;
    private readonly Mock<ISessionRepository> _mockSessionRepository;
    private readonly Mock<ITaskRepository> _mockTaskRepository;
    private readonly Mock<ISoundService> _mockSoundService;
    private readonly Mock<INotificationService> _mockNotificationService;
    private readonly Mock<IStatisticsService> _mockStatisticsService;
    private readonly MainViewModel _mainViewModel;
    private readonly DashboardViewModel _dashboardViewModel;

    public PomodoroWorkflowIntegrationTests()
    {
        _mockTimerService = new Mock<ITimerService>();
        _mockSessionRepository = new Mock<ISessionRepository>();
        _mockTaskRepository = new Mock<ITaskRepository>();
        _mockSoundService = new Mock<ISoundService>();
        _mockNotificationService = new Mock<INotificationService>();
        _mockStatisticsService = new Mock<IStatisticsService>();

        _mainViewModel = new MainViewModel(
            _mockTimerService.Object,
            _mockSessionRepository.Object,
            _mockTaskRepository.Object,
            _mockSoundService.Object,
            _mockNotificationService.Object,
            _mockStatisticsService.Object);

        _dashboardViewModel = new DashboardViewModel(
            _mockSessionRepository.Object,
            _mockTaskRepository.Object,
            _mockStatisticsService.Object);
    }

    [Fact]
    public async Task CompletePomodoroWorkflow_ShouldUpdateDashboardStatistics()
    {
        // Arrange
        var sessionId = "test-session-123";
        var session = new Session(sessionId, "pomodoro", DateTime.Now);
        var tasks = new List<TaskItem>
        {
            new TaskItem("Task 1", sessionId) { Id = 1, Completed = true },
            new TaskItem("Task 2", sessionId) { Id = 2, Completed = false }
        };

        // Setup session creation
        _mockSessionRepository.Setup(x => x.CreateSession(It.IsAny<string>(), "pomodoro", It.IsAny<DateTime>()))
            .ReturnsAsync(session);

        // Setup task operations
        _mockTaskRepository.Setup(x => x.Add("Task 1", sessionId))
            .ReturnsAsync(tasks[0]);
        _mockTaskRepository.Setup(x => x.Add("Task 2", sessionId))
            .ReturnsAsync(tasks[1]);
        _mockTaskRepository.Setup(x => x.ToggleCompleted(1, true))
            .ReturnsAsync(tasks[0]);

        // Setup session statistics
        var sessionsWithStats = new List<Session>
        {
            new Session(sessionId, "pomodoro", DateTime.Now) { EndTime = DateTime.Now.AddMinutes(25), TotalTasks = 2, CompletedTasks = 1 }
        };

        _mockSessionRepository.Setup(x => x.GetSessionsWithStats())
            .ReturnsAsync(sessionsWithStats);

        // Setup dashboard statistics
        var dailyStats = new List<DailyStats>
        {
            new DailyStats { Date = DateTime.Today, TotalMinutes = 25, SessionsCompleted = 1, TasksCompleted = 1, ProductivityScore = 75.0 }
        };

        _mockStatisticsService.Setup(x => x.GetDailyStatsAsync())
            .ReturnsAsync(dailyStats);

        // Act - Complete full Pomodoro workflow
        // 1. Start timer
        _mainViewModel.ToggleTimerCommand.Execute(null);

        // 2. Add tasks
        _mainViewModel.NewTask = "Task 1";
        await _mainViewModel.AddTaskCommand.ExecuteAsync(null);

        _mainViewModel.NewTask = "Task 2";
        await _mainViewModel.AddTaskCommand.ExecuteAsync(null);

        // 3. Complete a task
        await _mainViewModel.ToggleTaskCommand.ExecuteAsync(1);

        // 4. Simulate timer completion
        _mockTimerService.Raise(x => x.TimerCompleted += null, EventArgs.Empty);

        // 5. Load dashboard statistics
        await _dashboardViewModel.LoadDailyStatsCommand.ExecuteAsync(null);

        // Assert
        // Verify main view model state
        _mainViewModel.IsRunning.Should().BeFalse();
        _mainViewModel.IsRinging.Should().BeTrue();
        _mainViewModel.Tasks.Should().HaveCount(2);
        _mainViewModel.Tasks.First(t => t.Id == 1).Completed.Should().BeTrue();

        // Verify dashboard statistics
        _dashboardViewModel.TotalFocusTime.Should().Be(TimeSpan.FromMinutes(25));
        _dashboardViewModel.CompletedSessions.Should().Be(1);
        _dashboardViewModel.CompletedTasks.Should().Be(1);
        _dashboardViewModel.ProductivityScore.Should().BeGreaterThan(0);

        // Verify service calls
        _mockSessionRepository.Verify(x => x.CreateSession(It.IsAny<string>(), "pomodoro", It.IsAny<DateTime>()), Times.Once);
        _mockTaskRepository.Verify(x => x.Add("Task 1", It.IsAny<string>()), Times.Once);
        _mockTaskRepository.Verify(x => x.Add("Task 2", It.IsAny<string>()), Times.Once);
        _mockTaskRepository.Verify(x => x.ToggleCompleted(1, true), Times.Once);
        _mockSoundService.Verify(x => x.PlayNotificationSound(), Times.Once);
        _mockNotificationService.Verify(x => x.ShowNotificationAsync("Pomodoro Completed", "Your timer has finished!"), Times.Once);
        _mockStatisticsService.Verify(x => x.GetDailyStatsAsync(), Times.Once);
    }

    [Fact]
    public async Task MultipleSessionsWithAutoAdvance_ShouldCreateCorrectStatistics()
    {
        // Arrange
        var pomodoroSession = new Session("pomodoro-session", "pomodoro", DateTime.Now);
        var breakSession = new Session("break-session", "shortBreak", DateTime.Now.AddMinutes(25));

        _mockSessionRepository.Setup(x => x.CreateSession(It.IsAny<string>(), "pomodoro", It.IsAny<DateTime>()))
            .ReturnsAsync(pomodoroSession);

        _mockSessionRepository.Setup(x => x.CreateSession(It.IsAny<string>(), "shortBreak", It.IsAny<DateTime>()))
            .ReturnsAsync(breakSession);

        _mockSessionRepository.Setup(x => x.EndSession(It.IsAny<string>(), It.IsAny<DateTime>()))
            .ReturnsAsync(true);

        _mockSessionRepository.Setup(x => x.GetSessionsWithStats())
            .ReturnsAsync(new List<Session>
            {
                new Session("pomodoro-session", "pomodoro", DateTime.Now) { EndTime = DateTime.Now.AddMinutes(25), TotalTasks = 1, CompletedTasks = 1 },
                new Session("break-session", "shortBreak", DateTime.Now.AddMinutes(25)) { EndTime = DateTime.Now.AddMinutes(30), TotalTasks = 0, CompletedTasks = 0 }
            });

        var weeklyStats = new List<WeeklyStats>
        {
            new WeeklyStats { WeekNumber = 1, Year = 2024, TotalMinutes = 30, SessionsCompleted = 2, TasksCompleted = 1, ProductivityScore = 80.0 }
        };

        _mockStatisticsService.Setup(x => x.GetWeeklyStatsAsync())
            .ReturnsAsync(weeklyStats);

        // Act
        // Start Pomodoro session
        _mainViewModel.ToggleTimerCommand.Execute(null);

        // Add a task
        _mainViewModel.NewTask = "Complete work";
        await _mainViewModel.AddTaskCommand.ExecuteAsync(null);

        // Simulate timer completion (auto-advance to break)
        _mockTimerService.Raise(x => x.TimerCompleted += null, EventArgs.Empty);

        // Wait a moment for auto-advance
        await Task.Delay(100);

        // Load statistics
        await _dashboardViewModel.LoadDailyStatsCommand.ExecuteAsync(null);

        // Assert
        _mockSessionRepository.Verify(x => x.CreateSession(It.IsAny<string>(), "pomodoro", It.IsAny<DateTime>()), Times.Once);
        _mockSessionRepository.Verify(x => x.CreateSession(It.IsAny<string>(), "shortBreak", It.IsAny<DateTime>()), Times.Once);
        _mockSessionRepository.Verify(x => x.EndSession(It.IsAny<string>(), It.IsAny<DateTime>()), Times.Exactly(2));

        // Verify that the mode changed to short break
        _mainViewModel.Mode.Should().Be("shortBreak");
        _mainViewModel.TimeLeft.Should().Be(5 * 60); // 5 minutes for short break
    }

    [Fact]
    public async Task AchievementSystem_ShouldTrackProgressCorrectly()
    {
        // Arrange
        var sessions = new List<Session>
        {
            new Session("session1", "pomodoro", DateTime.Now) { EndTime = DateTime.Now.AddMinutes(25) },
            new Session("session2", "pomodoro", DateTime.Now.AddDays(-1)) { EndTime = DateTime.Now.AddDays(-1).AddMinutes(25) },
            new Session("session3", "pomodoro", DateTime.Now.AddDays(-2)) { EndTime = DateTime.Now.AddDays(-2).AddMinutes(25) }
        };

        var tasks = new List<TaskItem>
        {
            new TaskItem("Task 1", "session1") { Completed = true },
            new TaskItem("Task 2", "session1") { Completed = true },
            new TaskItem("Task 3", "session2") { Completed = true },
            new TaskItem("Task 4", "session3") { Completed = true }
        };

        var achievements = new List<Achievement>
        {
            new Achievement { Id = "first_session", Title = "First Steps", Progress = 1, MaxProgress = 1, UnlockedDate = DateTime.Now },
            new Achievement { Id = "week_warrior", Title = "Week Warrior", Progress = 3, MaxProgress = 7 },
            new Achievement { Id = "task_master", Title = "Task Master", Progress = 4, MaxProgress = 50 }
        };

        _mockSessionRepository.Setup(x => x.GetAllSessionsAsync())
            .ReturnsAsync(sessions);

        _mockTaskRepository.Setup(x => x.GetAllTasksAsync())
            .ReturnsAsync(tasks);

        _mockStatisticsService.Setup(x => x.GetAchievementsAsync())
            .ReturnsAsync(achievements);

        // Act
        await _dashboardViewModel.LoadAchievementsCommand.ExecuteAsync(null);

        // Assert
        _dashboardViewModel.Achievements.Should().HaveCount(3);
        _dashboardViewModel.Achievements.Should().Contain(a => a.Id == "first_session" && a.Progress == a.MaxProgress);
        _dashboardViewModel.Achievements.Should().Contain(a => a.Id == "week_warrior" && a.Progress < a.MaxProgress);
        _dashboardViewModel.Achievements.Should().Contain(a => a.Id == "task_master" && a.Progress < a.MaxProgress);

        var firstSessionAchievement = _dashboardViewModel.Achievements.First(a => a.Id == "first_session");
        firstSessionAchievement.UnlockedDate.Should().NotBeNull();
    }

    [Fact]
    public async Task CategoryAnalytics_ShouldCategorizeTasksCorrectly()
    {
        // Arrange
        var tasks = new List<TaskItem>
        {
            new TaskItem("Complete work project", "session1") { Completed = true },
            new TaskItem("Study for exam", "session2") { Completed = true },
            new TaskItem("Exercise routine", "session3") { Completed = true },
            new TaskItem("Clean the house", "session4") { Completed = false },
            new TaskItem("Code review", "session5") { Completed = true }
        };

        var sessions = new List<Session>
        {
            new Session("session1", "pomodoro", DateTime.Now) { EndTime = DateTime.Now.AddMinutes(25) },
            new Session("session2", "pomodoro", DateTime.Now.AddHours(-1)) { EndTime = DateTime.Now.AddHours(-1).AddMinutes(25) },
            new Session("session3", "pomodoro", DateTime.Now.AddHours(-2)) { EndTime = DateTime.Now.AddHours(-2).AddMinutes(25) },
            new Session("session4", "pomodoro", DateTime.Now.AddHours(-3)) { EndTime = DateTime.Now.AddHours(-3).AddMinutes(25) },
            new Session("session5", "pomodoro", DateTime.Now.AddHours(-4)) { EndTime = DateTime.Now.AddHours(-4).AddMinutes(25) }
        };

        var categoryStats = new List<CategoryStats>
        {
            new CategoryStats { Category = "Work", TaskCount = 2, CompletedCount = 2, TotalTime = TimeSpan.FromMinutes(50) },
            new CategoryStats { Category = "Study", TaskCount = 1, CompletedCount = 1, TotalTime = TimeSpan.FromMinutes(25) },
            new CategoryStats { Category = "Health", TaskCount = 1, CompletedCount = 1, TotalTime = TimeSpan.FromMinutes(25) },
            new CategoryStats { Category = "Home", TaskCount = 1, CompletedCount = 0, TotalTime = TimeSpan.FromMinutes(25) }
        };

        _mockTaskRepository.Setup(x => x.GetAllTasksAsync())
            .ReturnsAsync(tasks);

        _mockSessionRepository.Setup(x => x.GetAllSessionsAsync())
            .ReturnsAsync(sessions);

        _mockStatisticsService.Setup(x => x.GetCategoryStatsAsync())
            .ReturnsAsync(categoryStats);

        // Act
        await _dashboardViewModel.LoadCategoryStatsCommand.ExecuteAsync(null);

        // Assert
        _dashboardViewModel.CategoryStats.Should().HaveCount(4);
        _dashboardViewModel.CategoryStats.Should().Contain(c => c.Category == "Work" && c.TaskCount == 2);
        _dashboardViewModel.CategoryStats.Should().Contain(c => c.Category == "Study" && c.TaskCount == 1);
        _dashboardViewModel.CategoryStats.Should().Contain(c => c.Category == "Health" && c.TaskCount == 1);
        _dashboardViewModel.CategoryStats.Should().Contain(c => c.Category == "Home" && c.TaskCount == 1);

        var workCategory = _dashboardViewModel.CategoryStats.First(c => c.Category == "Work");
        workCategory.CompletionRate.Should().Be(1.0); // 2/2 completed
        workCategory.TotalTime.Should().Be(TimeSpan.FromMinutes(50));
    }

    [Fact]
    public async Task ExportFunctionality_ShouldGenerateReports()
    {
        // Arrange
        var sessions = new List<Session>
        {
            new Session("session1", "pomodoro", DateTime.Now) { EndTime = DateTime.Now.AddMinutes(25) },
            new Session("session2", "shortBreak", DateTime.Now.AddHours(-1)) { EndTime = DateTime.Now.AddHours(-1).AddMinutes(5) }
        };

        var tasks = new List<TaskItem>
        {
            new TaskItem("Task 1", "session1") { Completed = true },
            new TaskItem("Task 2", "session1") { Completed = false }
        };

        _mockSessionRepository.Setup(x => x.GetAllSessionsAsync())
            .ReturnsAsync(sessions);

        _mockTaskRepository.Setup(x => x.GetAllTasksAsync())
            .ReturnsAsync(tasks);

        _mockStatisticsService.Setup(x => x.ExportReportAsync(ReportFormat.Pdf))
            .Returns(Task.CompletedTask);

        _mockStatisticsService.Setup(x => x.ExportReportAsync(ReportFormat.Csv))
            .Returns(Task.CompletedTask);

        // Act
        await _dashboardViewModel.ExportReportCommand.ExecuteAsync(ReportFormat.Pdf);
        await _dashboardViewModel.ExportReportCommand.ExecuteAsync(ReportFormat.Csv);

        // Assert
        _mockStatisticsService.Verify(x => x.ExportReportAsync(ReportFormat.Pdf), Times.Once);
        _mockStatisticsService.Verify(x => x.ExportReportAsync(ReportFormat.Csv), Times.Once);
    }

    [Fact]
    public async Task GoalTracking_ShouldUpdateGoalsCorrectly()
    {
        // Arrange
        var newDailyGoal = 180;
        var newWeeklyGoal = 1260;
        var newMonthlyGoal = 5400;

        _mockStatisticsService.Setup(x => x.UpdateGoalsAsync(newDailyGoal, newWeeklyGoal, newMonthlyGoal))
            .Returns(Task.CompletedTask);

        // Act
        await _dashboardViewModel.UpdateGoalsCommand.ExecuteAsync((newDailyGoal, newWeeklyGoal, newMonthlyGoal));

        // Assert
        _dashboardViewModel.DailyGoal.Should().Be(newDailyGoal);
        _dashboardViewModel.WeeklyGoal.Should().Be(newWeeklyGoal);
        _dashboardViewModel.MonthlyGoal.Should().Be(newMonthlyGoal);

        _mockStatisticsService.Verify(x => x.UpdateGoalsAsync(newDailyGoal, newWeeklyGoal, newMonthlyGoal), Times.Once);
    }

    [Fact]
    public async Task SoundAndNotificationIntegration_ShouldWorkTogether()
    {
        // Arrange
        _mockSoundService.Setup(x => x.PlayNotificationSound())
            .Verifiable();

        _mockNotificationService.Setup(x => x.ShowNotificationAsync("Pomodoro Completed", "Your timer has finished!"))
            .Returns(Task.CompletedTask)
            .Verifiable();

        // Act
        _mainViewModel.ToggleTimerCommand.Execute(null);
        _mockTimerService.Raise(x => x.TimerCompleted += null, EventArgs.Empty);

        // Assert
        _mainViewModel.IsRinging.Should().BeTrue();
        _mockSoundService.Verify(x => x.PlayNotificationSound(), Times.Once);
        _mockNotificationService.Verify(x => x.ShowNotificationAsync("Pomodoro Completed", "Your timer has finished!"), Times.Once);

        // Test stopping alarm
        _mainViewModel.StopAlarmCommand.Execute(null);
        _mainViewModel.IsRinging.Should().BeFalse();
        _mockSoundService.Verify(x => x.StopNotificationSound(), Times.Once);
    }

    [Fact]
    public async Task TimerProgress_ShouldUpdateCorrectly()
    {
        // Arrange
        var initialTime = 25 * 60; // 25 minutes
        var timeAfter1Minute = initialTime - 60;

        // Act
        // Simulate timer ticking
        _mockTimerService.Raise(x => x.Tick += null, EventArgs.Empty, timeAfter1Minute);

        // Assert
        _mainViewModel.TimeLeft.Should().Be(timeAfter1Minute);
        _mainViewModel.ProgressPercentage.Should().BeGreaterThan(0);
        _mainViewModel.ProgressPercentage.Should().BeLessThan(100);

        // Test progress calculation
        var expectedProgress = ((double)(initialTime - timeAfter1Minute) / initialTime) * 100;
        _mainViewModel.ProgressPercentage.Should().BeApproximately(expectedProgress, 0.01);
    }
}