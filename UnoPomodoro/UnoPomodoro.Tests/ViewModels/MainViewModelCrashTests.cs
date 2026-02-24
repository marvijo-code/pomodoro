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

public class MainViewModelCrashTests
{
    private readonly Mock<ITimerService> _mockTimerService;
    private readonly Mock<ISessionRepository> _mockSessionRepository;
    private readonly Mock<ITaskRepository> _mockTaskRepository;
    private readonly Mock<ISoundService> _mockSoundService;
    private readonly Mock<INotificationService> _mockNotificationService;
    private readonly Mock<IStatisticsService> _mockStatisticsService;
    private readonly Mock<IVibrationService> _mockVibrationService;
    private readonly Mock<ISettingsService> _mockSettingsService;
    private readonly MainViewModel _viewModel;

    public MainViewModelCrashTests()
    {
        _mockTimerService = new Mock<ITimerService>();
        _mockSessionRepository = new Mock<ISessionRepository>();
        _mockTaskRepository = new Mock<ITaskRepository>();
        _mockSoundService = new Mock<ISoundService>();
        _mockNotificationService = new Mock<INotificationService>();
        _mockStatisticsService = new Mock<IStatisticsService>();
        _mockVibrationService = new Mock<IVibrationService>();
        _mockSettingsService = new Mock<ISettingsService>();

        _mockSettingsService.SetupAllProperties();
        _mockSettingsService.Object.IsSoundEnabled = true;
        _mockSettingsService.Object.SoundVolume = 100;
        _mockSettingsService.Object.SoundDuration = 5;
        _mockSettingsService.Object.VibrationDuration = 5;
        _mockSettingsService.Object.PomodoroDuration = 25;
        _mockSettingsService.Object.ShortBreakDuration = 5;
        _mockSettingsService.Object.LongBreakDuration = 15;
        _mockSettingsService.Object.PomodorosBeforeLongBreak = 4;
        _mockSettingsService.Object.IsNotificationEnabled = true;
        _mockSettingsService.Object.DailyGoal = 120;
        _mockSettingsService.Object.WeeklyGoal = 840;
        _mockSettingsService.Object.MonthlyGoal = 3600;
        _mockSettingsService.Setup(x => x.LoadAsync()).Returns(Task.CompletedTask);
        _mockSettingsService.Setup(x => x.SaveAsync()).Returns(Task.CompletedTask);

        _viewModel = new MainViewModel(
            _mockTimerService.Object,
            _mockSessionRepository.Object,
            _mockTaskRepository.Object,
            _mockSoundService.Object,
            _mockNotificationService.Object,
            _mockStatisticsService.Object,
            _mockVibrationService.Object,
            _mockSettingsService.Object);
    }

    [Fact]
    public void ToggleTimer_WhenSessionIdIsNull_ShouldCreateSessionAndStartTimer()
    {
        // Arrange
        _viewModel.SessionId = null;
        _viewModel.IsRunning = false;
        
        _mockSessionRepository.Setup(x => x.CreateSession(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>()))
            .ReturnsAsync(new Session());
            
        _mockSessionRepository.Setup(x => x.GetSessionsWithStats())
            .ReturnsAsync(new System.Collections.Generic.List<Session>());

        // Act
        _viewModel.ToggleTimerCommand.Execute(null);

        // Assert
        _viewModel.IsRunning.Should().BeTrue();
        _viewModel.SessionId.Should().NotBeNull();
        _mockTimerService.Verify(x => x.Start(It.IsAny<int>()), Times.Once);
    }
    
    [Fact]
    public async Task StartNewSession_ShouldResetSessionIdToNull()
    {
        // Arrange
        _viewModel.SessionId = "existing-session";
        
        // Act
        await _viewModel.StartNewSessionCommand.ExecuteAsync(null);
        
        // Assert
        _viewModel.SessionId.Should().BeNull();
        _viewModel.Tasks.Should().BeEmpty();
    }
    
    [Fact]
    public async Task AddTask_WhenSessionIdIsNull_ShouldCreateSession()
    {
        // Arrange
        _viewModel.SessionId = null;
        _viewModel.NewTask = "New Task";
        
        _mockSessionRepository.Setup(x => x.CreateSession(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>()))
            .ReturnsAsync(new Session());
            
        _mockSessionRepository.Setup(x => x.GetSessionsWithStats())
            .ReturnsAsync(new System.Collections.Generic.List<Session>());

        _mockTaskRepository.Setup(x => x.Add(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new TaskItem("New Task", "new-session"));

        // Act
        await _viewModel.AddTaskCommand.ExecuteAsync(null);
        
        // Assert
        _viewModel.SessionId.Should().NotBeNull();
        _viewModel.Tasks.Should().ContainSingle();
    }

    [Fact]
    public void TimerCompleted_WhenSoundDisabled_ShouldNotRing()
    {
        // Arrange
        _viewModel.IsRunning = true;
        _viewModel.IsSoundEnabled = false;

        // Act
        _mockTimerService.Raise(x => x.TimerCompleted += null, EventArgs.Empty);

        // Assert
        _viewModel.IsRinging.Should().BeFalse();
        _mockSoundService.Verify(x => x.PlayNotificationSound(), Times.Never);
    }

    [Fact]
    public async Task DeleteTask_WithInvalidId_ShouldNotThrow()
    {
        // Arrange - no tasks in collection
        _mockTaskRepository.Setup(x => x.Delete(999)).Returns(Task.CompletedTask);

        // Act & Assert - should not throw
        await _viewModel.DeleteTaskCommand.ExecuteAsync(999);
        _viewModel.Tasks.Should().BeEmpty();
    }

    [Fact]
    public void MultipleToggleTimerCalls_ShouldNotCrash()
    {
        // Arrange
        _mockSessionRepository.Setup(x => x.CreateSession(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>()))
            .ReturnsAsync(new Session());
        _mockSessionRepository.Setup(x => x.GetSessionsWithStats())
            .ReturnsAsync(new System.Collections.Generic.List<Session>());

        // Act - rapid toggle
        _viewModel.ToggleTimerCommand.Execute(null); // start
        _viewModel.ToggleTimerCommand.Execute(null); // pause
        _viewModel.ToggleTimerCommand.Execute(null); // resume

        // Assert
        _viewModel.IsRunning.Should().BeTrue();
    }
}
