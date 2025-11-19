using Xunit;
using Moq;
using FluentAssertions;
using UnoPomodoro.ViewModels;
using UnoPomodoro.Data.Models;
using UnoPomodoro.Data.Repositories;
using UnoPomodoro.Services;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace UnoPomodoro.Tests.ViewModels;

public class MainViewModelTests
{
    private readonly Mock<ITimerService> _mockTimerService;
    private readonly Mock<ISessionRepository> _mockSessionRepository;
    private readonly Mock<ITaskRepository> _mockTaskRepository;
    private readonly Mock<ISoundService> _mockSoundService;
    private readonly Mock<INotificationService> _mockNotificationService;
    private readonly Mock<IStatisticsService> _mockStatisticsService;
    private readonly MainViewModel _viewModel;

    public MainViewModelTests()
    {
        _mockTimerService = new Mock<ITimerService>();
        _mockSessionRepository = new Mock<ISessionRepository>();
        _mockTaskRepository = new Mock<ITaskRepository>();
        _mockSoundService = new Mock<ISoundService>();
        _mockNotificationService = new Mock<INotificationService>();
        _mockStatisticsService = new Mock<IStatisticsService>();

        _viewModel = new MainViewModel(
            _mockTimerService.Object,
            _mockSessionRepository.Object,
            _mockTaskRepository.Object,
            _mockSoundService.Object,
            _mockNotificationService.Object,
            _mockStatisticsService.Object);
    }

    [Fact]
    public void Constructor_ShouldInitializeWithDefaultValues()
    {
        // Assert
        _viewModel.TimeLeft.Should().Be(25 * 60); // 25 minutes default
        _viewModel.IsRunning.Should().BeFalse();
        _viewModel.Mode.Should().Be("pomodoro");
        _viewModel.IsSoundEnabled.Should().BeTrue();
        _viewModel.IsRinging.Should().BeFalse();
        _viewModel.ShowTasks.Should().BeFalse();
        _viewModel.ProgressPercentage.Should().Be(0);
        _viewModel.SessionInfo.Should().Be("Ready to start");
        _viewModel.Tasks.Should().BeEmpty();
        _viewModel.Sessions.Should().BeEmpty();
    }

    [Fact]
    public void Constructor_ShouldSetupTimerEventHandlers()
    {
        // Assert
        _mockTimerService.VerifyAdd(x => x.Tick += It.IsAny<EventHandler<int>>(), Times.Once);
        _mockTimerService.VerifyAdd(x => x.TimerCompleted += It.IsAny<EventHandler>(), Times.Once);
    }

    [Fact]
    public async Task ToggleTimer_WhenNotRunning_ShouldStartTimerAndCreateSession()
    {
        // Arrange
        _mockSessionRepository.Setup(x => x.CreateSession(It.IsAny<string>(), "pomodoro", It.IsAny<DateTime>()))
            .ReturnsAsync((string sessionId, string mode, DateTime startTime) => new Session(sessionId, mode, startTime));

        // Act
        _viewModel.ToggleTimerCommand.Execute(null);

        // Assert
        _viewModel.IsRunning.Should().BeTrue();
        _viewModel.SessionId.Should().NotBeNull();
        _mockTimerService.Verify(x => x.Start(25 * 60), Times.Once);
        _mockSessionRepository.Verify(x => x.CreateSession(It.IsAny<string>(), "pomodoro", It.IsAny<DateTime>()), Times.Once);
        _mockTaskRepository.Verify(x => x.GetBySession(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public void ToggleTimer_WhenRunning_ShouldPauseTimer()
    {
        // Arrange
        _viewModel.IsRunning = true;
        _viewModel.SessionId = "test-session";

        // Act
        _viewModel.ToggleTimerCommand.Execute(null);

        // Assert
        _viewModel.IsRunning.Should().BeFalse();
        _mockTimerService.Verify(x => x.Pause(), Times.Once);
    }

    [Fact]
    public void ResetTimer_ShouldResetTimerToDefaultTime()
    {
        // Arrange
        _viewModel.TimeLeft = 100;
        _viewModel.IsRunning = true;

        // Act
        _viewModel.ResetTimerCommand.Execute(null);

        // Assert
        _viewModel.TimeLeft.Should().Be(25 * 60);
        _viewModel.IsRunning.Should().BeFalse();
        _viewModel.ProgressPercentage.Should().Be(0);
        _mockTimerService.Verify(x => x.Reset(25 * 60), Times.Once);
    }

    [Fact]
    public void ChangeMode_ShouldUpdateTimerSettings()
    {
        // Act
        _viewModel.ChangeModeCommand.Execute("shortBreak");

        // Assert
        _viewModel.Mode.Should().Be("shortBreak");
        _viewModel.TimeLeft.Should().Be(5 * 60);
        _viewModel.IsRunning.Should().BeFalse();
        _viewModel.ProgressPercentage.Should().Be(0);
        _mockTimerService.Verify(x => x.Reset(5 * 60), Times.Once);
    }

    [Fact]
    public async Task AddTask_WhenValid_ShouldAddTaskToCollection()
    {
        // Arrange
        _viewModel.SessionId = "test-session";
        _viewModel.NewTask = "Test task";
        var expectedTask = new TaskItem("Test task", "test-session") { Id = 1 };

        _mockTaskRepository.Setup(x => x.Add("Test task", "test-session"))
            .ReturnsAsync(expectedTask);

        // Act
        await _viewModel.AddTaskCommand.ExecuteAsync(null);

        // Assert
        _viewModel.Tasks.Should().ContainSingle();
        _viewModel.Tasks.First().Should().BeEquivalentTo(expectedTask);
        _viewModel.NewTask.Should().BeEmpty();
        _mockTaskRepository.Verify(x => x.Add("Test task", "test-session"), Times.Once);
    }

    [Fact]
    public async Task AddTask_WhenEmptyTask_ShouldNotAddTask()
    {
        // Arrange
        _viewModel.SessionId = "test-session";
        _viewModel.NewTask = "";

        // Act
        await _viewModel.AddTaskCommand.ExecuteAsync(null);

        // Assert
        _viewModel.Tasks.Should().BeEmpty();
        _mockTaskRepository.Verify(x => x.Add(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task AddTask_WhenNoSession_ShouldCreateSessionAndAddTask()
    {
        // Arrange
        _viewModel.SessionId = null;
        _viewModel.NewTask = "Test task";
        var expectedTask = new TaskItem("Test task", "new-session") { Id = 1 };

        _mockSessionRepository.Setup(x => x.CreateSession(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>()))
            .ReturnsAsync(new Session());
        _mockSessionRepository.Setup(x => x.GetSessionsWithStats())
            .ReturnsAsync(new System.Collections.Generic.List<Session>());
        _mockTaskRepository.Setup(x => x.Add("Test task", It.IsAny<string>()))
            .ReturnsAsync(expectedTask);

        // Act
        await _viewModel.AddTaskCommand.ExecuteAsync(null);

        // Assert
        _viewModel.Tasks.Should().ContainSingle();
        _viewModel.SessionId.Should().NotBeNull();
        _mockTaskRepository.Verify(x => x.Add("Test task", It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task DeleteTask_ShouldRemoveTaskFromCollection()
    {
        // Arrange
        var task = new TaskItem("Test task", "test-session") { Id = 1 };
        _viewModel.Tasks.Add(task);

        _mockTaskRepository.Setup(x => x.Delete(1)).Returns(Task.CompletedTask);

        // Act
        await _viewModel.DeleteTaskCommand.ExecuteAsync(1);

        // Assert
        _viewModel.Tasks.Should().BeEmpty();
        _mockTaskRepository.Verify(x => x.Delete(1), Times.Once);
    }

    [Fact]
    public async Task ToggleTask_ShouldUpdateTaskCompletionStatus()
    {
        // Arrange
        var task = new TaskItem("Test task", "test-session") { Id = 1, Completed = false };
        _viewModel.Tasks.Add(task);

        _mockTaskRepository.Setup(x => x.ToggleCompleted(1, true))
            .ReturnsAsync(task);

        // Act
        await _viewModel.ToggleTaskCommand.ExecuteAsync(1);

        // Assert
        task.Completed.Should().BeTrue();
        task.CompletedAt.Should().NotBeNull();
        _mockTaskRepository.Verify(x => x.ToggleCompleted(1, true), Times.Once);
    }

    [Fact]
    public void ToggleSound_ShouldToggleSoundEnabled()
    {
        // Arrange
        _viewModel.IsSoundEnabled = true;

        // Act
        _viewModel.ToggleSoundCommand.Execute(null);

        // Assert
        _viewModel.IsSoundEnabled.Should().BeFalse();

        // Act again
        _viewModel.ToggleSoundCommand.Execute(null);

        // Assert
        _viewModel.IsSoundEnabled.Should().BeTrue();
    }

    [Fact]
    public void ToggleTasks_ShouldToggleShowTasks()
    {
        // Arrange
        _viewModel.ShowTasks = false;

        // Act
        _viewModel.ToggleTasksCommand.Execute(null);

        // Assert
        _viewModel.ShowTasks.Should().BeTrue();

        // Act again
        _viewModel.ToggleTasksCommand.Execute(null);

        // Assert
        _viewModel.ShowTasks.Should().BeFalse();
    }

    [Fact]
    public async Task SkipSession_ShouldAutoAdvanceToNextMode()
    {
        // Arrange
        _viewModel.Mode = "pomodoro";
        _viewModel.SessionId = "test-session";

        _mockSessionRepository.Setup(x => x.EndSession("test-session", It.IsAny<DateTime>()))
            .ReturnsAsync(true);

        _mockSessionRepository.Setup(x => x.CreateSession(It.IsAny<string>(), "shortBreak", It.IsAny<DateTime>()))
            .ReturnsAsync((string sessionId, string mode, DateTime startTime) => new Session(sessionId, mode, startTime));

        // Act
        await _viewModel.SkipSessionCommand.ExecuteAsync(null);

        // Assert
        _mockSessionRepository.Verify(x => x.EndSession("test-session", It.IsAny<DateTime>()), Times.Once);
        // Note: AutoAdvanceSession does NOT create a new session immediately, it just changes mode.
        // Session is created when timer starts or task added.
        // So we should verify ChangeMode logic (TimeLeft reset) and EndSession.
        _viewModel.Mode.Should().Be("shortBreak");
        _viewModel.TimeLeft.Should().Be(5 * 60);
    }

    [Fact]
    public async Task StartNewSession_ShouldEndCurrentSessionAndReset()
    {
        // Arrange
        _viewModel.SessionId = "current-session";
        _viewModel.Mode = "pomodoro";

        _mockSessionRepository.Setup(x => x.EndSession("current-session", It.IsAny<DateTime>()))
            .ReturnsAsync(true);

        // Act
        await _viewModel.StartNewSessionCommand.ExecuteAsync(null);

        // Assert
        _mockSessionRepository.Verify(x => x.EndSession("current-session", It.IsAny<DateTime>()), Times.Once);
        _mockSessionRepository.Verify(x => x.CreateSession(It.IsAny<string>(), "pomodoro", It.IsAny<DateTime>()), Times.Never);
        _viewModel.Tasks.Should().BeEmpty();
        _viewModel.TimeLeft.Should().Be(25 * 60);
        _viewModel.SessionId.Should().BeNull();
    }

    [Fact]
    public void TestSound_ShouldPlayNotificationSound()
    {
        // Act
        _viewModel.TestSoundCommand.Execute(null);

        // Assert
        _mockSoundService.Verify(x => x.PlayNotificationSound(), Times.Once);
    }

    [Fact]
    public void StopAlarm_ShouldStopAlarmSound()
    {
        // Arrange
        _viewModel.IsRinging = true;

        // Act
        _viewModel.StopAlarmCommand.Execute(null);

        // Assert
        _viewModel.IsRinging.Should().BeFalse();
        _mockSoundService.Verify(x => x.StopNotificationSound(), Times.Once);
    }

    [Fact]
    public void TimerTick_ShouldUpdateTimeLeftAndProgress()
    {
        // Arrange
        var initialTime = 25 * 60;

        // Act
        _mockTimerService.Raise(x => x.Tick += null, EventArgs.Empty, initialTime - 60);

        // Assert
        _viewModel.TimeLeft.Should().Be(initialTime - 60);
        _viewModel.ProgressPercentage.Should().BeGreaterThan(0);
    }

    [Fact]
    public void TimerCompleted_ShouldStopTimerAndPlaySound()
    {
        // Arrange
        _viewModel.IsRunning = true;
        _viewModel.IsSoundEnabled = true;

        // Act
        _mockTimerService.Raise(x => x.TimerCompleted += null, EventArgs.Empty);

        // Assert
        _viewModel.IsRunning.Should().BeFalse();
        _viewModel.IsRinging.Should().BeTrue();
        _mockSoundService.Verify(x => x.PlayNotificationSound(), Times.Once);
        _mockNotificationService.Verify(x => x.ShowNotificationAsync("Pomodoro Completed", "Your timer has finished!"), Times.Once);
    }

    [Fact]
    public void FormatTime_ShouldFormatTimeCorrectly()
    {
        // Assert
        _viewModel.FormatTime(0).Should().Be("00:00");
        _viewModel.FormatTime(60).Should().Be("01:00");
        _viewModel.FormatTime(3661).Should().Be("61:01");
        _viewModel.FormatTime(125).Should().Be("02:05");
    }

    [Fact]
    public void UpdateSessionInfo_WhenNoSession_ShouldShowReadyMessage()
    {
        // Arrange
        _viewModel.SessionId = null;

        // Act
        var privateMethod = typeof(MainViewModel).GetMethod("UpdateSessionInfo", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        privateMethod?.Invoke(_viewModel, null);

        // Assert
        _viewModel.SessionInfo.Should().Be("Ready to start");
    }

    [Fact]
    public void UpdateSessionInfo_WhenRunning_ShouldShowTaskProgress()
    {
        // Arrange
        _viewModel.SessionId = "test-session";
        _viewModel.IsRunning = true;
        _viewModel.Tasks.Add(new TaskItem("Task 1", "test-session") { Completed = true });
        _viewModel.Tasks.Add(new TaskItem("Task 2", "test-session") { Completed = false });

        // Act
        var privateMethod = typeof(MainViewModel).GetMethod("UpdateSessionInfo", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        privateMethod?.Invoke(_viewModel, null);

        // Assert
        _viewModel.SessionInfo.Should().Be("1/2 tasks completed");
    }
}