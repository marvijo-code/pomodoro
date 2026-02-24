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
    private readonly Mock<IVibrationService> _mockVibrationService;
    private readonly Mock<ISettingsService> _mockSettingsService;
    private readonly MainViewModel _viewModel;

    public MainViewModelTests()
    {
        _mockTimerService = new Mock<ITimerService>();
        _mockSessionRepository = new Mock<ISessionRepository>();
        _mockTaskRepository = new Mock<ITaskRepository>();
        _mockSoundService = new Mock<ISoundService>();
        _mockNotificationService = new Mock<INotificationService>();
        _mockStatisticsService = new Mock<IStatisticsService>();
        _mockVibrationService = new Mock<IVibrationService>();
        _mockSettingsService = new Mock<ISettingsService>();

        // Setup default settings values
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
    public void ToggleTimer_WhenResuming_ShouldCallResumeInsteadOfStart()
    {
        // Arrange - simulate a paused session (SessionId exists, not running)
        _viewModel.SessionId = "existing-session";
        _viewModel.IsRunning = false;

        // Act
        _viewModel.ToggleTimerCommand.Execute(null);

        // Assert - should call Resume() not Start()
        _viewModel.IsRunning.Should().BeTrue();
        _mockTimerService.Verify(x => x.Resume(), Times.Once);
        _mockTimerService.Verify(x => x.Start(It.IsAny<int>()), Times.Never);
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
    public void ChangeMode_WhenRunning_ShouldNotChangeMode()
    {
        // Arrange
        _viewModel.IsRunning = true;

        // Act
        _viewModel.ChangeModeCommand.Execute("shortBreak");

        // Assert - mode should stay as pomodoro
        _viewModel.Mode.Should().Be("pomodoro");
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
        _viewModel.PomodoroCount = 1; // 1 % 4 != 0 → shortBreak

        _mockSessionRepository.Setup(x => x.EndSession("test-session", It.IsAny<DateTime>()))
            .ReturnsAsync(true);

        // Act
        await _viewModel.SkipSessionCommand.ExecuteAsync(null);

        // Assert
        _mockSessionRepository.Verify(x => x.EndSession("test-session", It.IsAny<DateTime>()), Times.Once);
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
    public void StopAlarm_ShouldStopAlarmSoundAndVibration()
    {
        // Arrange
        _viewModel.IsRinging = true;

        // Act
        _viewModel.StopAlarmCommand.Execute(null);

        // Assert
        _viewModel.IsRinging.Should().BeFalse();
        _mockSoundService.Verify(x => x.StopNotificationSound(), Times.Once);
        _mockVibrationService.Verify(x => x.Cancel(), Times.Once);
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
        _viewModel.ShowCompletionDialog.Should().BeTrue();
        _viewModel.CompletionTitle.Should().Be("Pomodoro Completed!");
        _mockSoundService.Verify(x => x.PlayNotificationSound(), Times.Once);
    }

    [Fact]
    public void TimerCompleted_WithVibrationEnabled_ShouldVibrate()
    {
        // Arrange
        _viewModel.IsRunning = true;
        _viewModel.IsVibrationEnabled = true;
        _mockVibrationService.Setup(x => x.IsSupported).Returns(true);

        // Act
        _mockTimerService.Raise(x => x.TimerCompleted += null, EventArgs.Empty);

        // Assert - repeat=true so alarm vibrates until user dismisses completion dialog
        _mockVibrationService.Verify(x => x.VibratePattern(It.IsAny<long[]>(), true), Times.Once);
    }
    
    [Fact]
    public async Task TimerCompleted_WithVibrationEnabled_ShouldAutoStopVibrationAfterDuration()
    {
        // Arrange
        _viewModel.IsRunning = true;
        _viewModel.IsVibrationEnabled = true;
        _viewModel.VibrationDuration = 1;
        _mockVibrationService.Setup(x => x.IsSupported).Returns(true);

        // Act
        _mockTimerService.Raise(x => x.TimerCompleted += null, EventArgs.Empty);
        await Task.Delay(1200);

        // Assert
        _mockVibrationService.Verify(x => x.Cancel(), Times.AtLeastOnce);
    }

    [Fact]
    public void TimerCompleted_WithVibrationDisabled_ShouldNotVibrate()
    {
        // Arrange
        _viewModel.IsRunning = true;
        _viewModel.IsVibrationEnabled = false;

        // Act
        _mockTimerService.Raise(x => x.TimerCompleted += null, EventArgs.Empty);

        // Assert
        _mockVibrationService.Verify(x => x.VibratePattern(It.IsAny<long[]>(), It.IsAny<bool>()), Times.Never);
    }

    [Fact]
    public void TimerCompleted_WithNotificationsDisabled_ShouldNotShowNotification()
    {
        // Arrange
        _viewModel.IsRunning = true;
        _viewModel.IsNotificationEnabled = false;

        // Act
        _mockTimerService.Raise(x => x.TimerCompleted += null, EventArgs.Empty);

        // Assert
        _mockNotificationService.Verify(x => x.ShowNotificationAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task TimerTick_WithLastMinuteAlertEnabled_ShouldSendFinalMinuteNotification()
    {
        // Arrange
        _viewModel.Mode = "pomodoro";
        _viewModel.IsRunning = true;
        _viewModel.IsLastMinuteAlertEnabled = true;
        _viewModel.IsNotificationEnabled = true;

        // Act
        _mockTimerService.Raise(x => x.Tick += null, EventArgs.Empty, 60);
        await Task.Delay(50);

        // Assert
        _mockNotificationService.Verify(
            x => x.ShowNotificationAsync("Final Minute", "One minute left. Wrap up your current task."),
            Times.Once);
    }

    [Fact]
    public async Task TimerCompleted_WithSessionGoal_ShouldIncludeGoalStatusInCompletionMessage()
    {
        // Arrange
        _viewModel.IsRunning = true;
        _viewModel.Mode = "pomodoro";
        _viewModel.SessionTaskGoal = 2;
        _viewModel.IsSoundEnabled = false;
        _viewModel.IsNotificationEnabled = true;
        _viewModel.Tasks.Add(new TaskItem("Task 1", "session") { Id = 1, Completed = true });
        _viewModel.Tasks.Add(new TaskItem("Task 2", "session") { Id = 2, Completed = false });

        // Act
        _mockTimerService.Raise(x => x.TimerCompleted += null, EventArgs.Empty);
        await Task.Delay(50);

        // Assert - completion message should contain goal progress
        _viewModel.ShowCompletionDialog.Should().BeTrue();
        _viewModel.CompletionMessage.Should().Contain("1 more task");
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

    // ── Overlay mutual exclusion tests ───────────────────────────

    [Fact]
    public void ToggleTasks_ShouldCloseOtherOverlays()
    {
        // Arrange - dashboard is open
        _viewModel.ShowDashboard = true;
        _viewModel.ShowSettings = true;

        // Act
        _viewModel.ToggleTasksCommand.Execute(null);

        // Assert
        _viewModel.ShowTasks.Should().BeTrue();
        _viewModel.ShowDashboard.Should().BeFalse();
        _viewModel.ShowSettings.Should().BeFalse();
    }

    [Fact]
    public void ToggleSettings_ShouldCloseOtherOverlays()
    {
        // Arrange - tasks panel is open
        _viewModel.ShowTasks = true;
        _viewModel.ShowDashboard = true;

        // Act
        _viewModel.ToggleSettingsCommand.Execute(null);

        // Assert
        _viewModel.ShowSettings.Should().BeTrue();
        _viewModel.ShowTasks.Should().BeFalse();
        _viewModel.ShowDashboard.Should().BeFalse();
    }

    [Fact]
    public void ToggleTimer_WithAutoOpenTasksOnSessionStart_ShouldShowTasksOverlay()
    {
        // Arrange
        _viewModel.AutoOpenTasksOnSessionStart = true;
        _viewModel.Mode = "pomodoro";

        _mockSessionRepository.Setup(x => x.CreateSession(It.IsAny<string>(), "pomodoro", It.IsAny<DateTime>()))
            .ReturnsAsync(new Session());
        _mockSessionRepository.Setup(x => x.GetSessionsWithStats())
            .ReturnsAsync(new System.Collections.Generic.List<Session>());

        // Act
        _viewModel.ToggleTimerCommand.Execute(null);

        // Assert
        _viewModel.ShowTasks.Should().BeTrue();
    }

    [Fact]
    public async Task SkipSession_WithCarryIncompleteTasksEnabled_ShouldRestoreTasksOnNextPomodoro()
    {
        // Arrange
        _viewModel.CarryIncompleteTasksToNextSession = true;
        _viewModel.Mode = "pomodoro";
        _viewModel.SessionId = "session-1";
        _viewModel.Tasks.Add(new TaskItem("Carry me", "session-1") { Id = 1, Completed = false });

        _mockSessionRepository.Setup(x => x.EndSession(It.IsAny<string>(), It.IsAny<DateTime>()))
            .ReturnsAsync(true);
        _mockSessionRepository.Setup(x => x.CreateSession(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>()))
            .ReturnsAsync(new Session());
        _mockTaskRepository.Setup(x => x.Add(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync((string text, string sessionId) => new TaskItem(text, sessionId) { Id = 10 });

        // Act
        await _viewModel.SkipSessionCommand.ExecuteAsync(null); // pomodoro -> shortBreak
        await _viewModel.SkipSessionCommand.ExecuteAsync(null); // shortBreak -> pomodoro

        // Assert
        _viewModel.Mode.Should().Be("pomodoro");
        _viewModel.Tasks.Should().ContainSingle();
        _viewModel.Tasks[0].Text.Should().Be("Carry me");
    }

    // ── Configurable duration tests ──────────────────────────────

    [Fact]
    public void PomodoroDuration_WhenChanged_ShouldUpdateTimerIfNotRunning()
    {
        // Arrange
        _viewModel.Mode = "pomodoro";
        _viewModel.IsRunning = false;

        // Act
        _viewModel.PomodoroDuration = 30;

        // Assert
        _viewModel.TimeLeft.Should().Be(30 * 60);
        _viewModel.TotalDuration.Should().Be(30 * 60);
    }

    [Fact]
    public void ShortBreakDuration_WhenChanged_ShouldUpdateTimerIfInShortBreakMode()
    {
        // Arrange
        _viewModel.ChangeModeCommand.Execute("shortBreak");
        _viewModel.IsRunning = false;

        // Act
        _viewModel.ShortBreakDuration = 10;

        // Assert
        _viewModel.TimeLeft.Should().Be(10 * 60);
    }

    // ── Settings persistence tests ───────────────────────────────

    [Fact]
    public void ChangingSoundEnabled_ShouldPersistSetting()
    {
        // Act
        _viewModel.IsSoundEnabled = false;

        // Assert
        _mockSettingsService.VerifySet(x => x.IsSoundEnabled = false, Times.Once);
        _mockSettingsService.Verify(x => x.SaveAsync(), Times.AtLeastOnce);
    }

    [Fact]
    public void ChangingVibrationEnabled_ShouldPersistSetting()
    {
        // Act
        _viewModel.IsVibrationEnabled = true;

        // Assert
        _mockSettingsService.VerifySet(x => x.IsVibrationEnabled = true, Times.Once);
        _mockSettingsService.Verify(x => x.SaveAsync(), Times.AtLeastOnce);
    }
    
    [Fact]
    public void ChangingVibrationDuration_ShouldPersistSetting()
    {
        // Act
        _viewModel.VibrationDuration = 7;

        // Assert
        _mockSettingsService.VerifySet(x => x.VibrationDuration = 7, Times.Once);
        _mockSettingsService.Verify(x => x.SaveAsync(), Times.AtLeastOnce);
    }

    // ── Long break logic tests ───────────────────────────────────

    [Fact]
    public void PomodoroCount_ShouldIncrementOnPomodoroCompletion()
    {
        // Arrange
        _viewModel.IsRunning = true;
        _viewModel.Mode = "pomodoro";
        _viewModel.PomodoroCount = 0;

        // Act
        _mockTimerService.Raise(x => x.TimerCompleted += null, EventArgs.Empty);

        // Assert
        _viewModel.PomodoroCount.Should().Be(1);
    }

    [Fact]
    public void AfterFourPomodoros_ShouldShowLongBreakInCompletionDialog()
    {
        // Arrange
        _viewModel.IsRunning = true;
        _viewModel.Mode = "pomodoro";
        _viewModel.PomodoroCount = 3; // Will become 4 after completion
        _viewModel.PomodorosBeforeLongBreak = 4;

        // Act
        _mockTimerService.Raise(x => x.TimerCompleted += null, EventArgs.Empty);

        // Assert - mode stays as pomodoro until user dismisses the dialog
        _viewModel.ShowCompletionDialog.Should().BeTrue();
        _viewModel.NextActionLabel.Should().Be("Start Long Break");
        _viewModel.Mode.Should().Be("pomodoro"); // Not changed yet
    }

    [Fact]
    public void AfterThreePomodoros_ShouldShowShortBreakInCompletionDialog()
    {
        // Arrange
        _viewModel.IsRunning = true;
        _viewModel.Mode = "pomodoro";
        _viewModel.PomodoroCount = 0; // Will become 1 after completion (1 % 4 != 0)

        // Act
        _mockTimerService.Raise(x => x.TimerCompleted += null, EventArgs.Empty);

        // Assert - mode stays as pomodoro until user dismisses the dialog
        _viewModel.ShowCompletionDialog.Should().BeTrue();
        _viewModel.NextActionLabel.Should().Be("Start Short Break");
        _viewModel.Mode.Should().Be("pomodoro"); // Not changed yet
    }

    // ── Session notes tests ──────────────────────────────────────

    [Fact]
    public void SessionNotes_ShouldBeResetOnNewSession()
    {
        // Arrange
        _viewModel.SessionNotes = "Some notes";
        _viewModel.SessionId = "test-session";

        // Act
        _viewModel.StartNewSessionCommand.Execute(null);

        // Assert
        _viewModel.SessionNotes.Should().BeEmpty();
    }

    // ── AddOneMinute tests ───────────────────────────────────────

    [Fact]
    public void AddOneMinute_ShouldAdd60Seconds()
    {
        // Arrange
        _viewModel.TimeLeft = 60; // 1 minute left

        // Act
        _viewModel.AddOneMinuteCommand.Execute(null);

        // Assert
        _viewModel.TimeLeft.Should().Be(120);
    }

    [Fact]
    public void CanAddMinute_WhenTimeLessThan3Minutes_ShouldBeTrue()
    {
        // Arrange
        _viewModel.TimeLeft = 170;

        // Assert
        _viewModel.CanAddMinute.Should().BeTrue();
    }

    [Fact]
    public void CanAddMinute_WhenTimeAtOrAbove3Minutes_ShouldBeFalse()
    {
        // Arrange
        _viewModel.TimeLeft = 180;

        // Assert
        _viewModel.CanAddMinute.Should().BeFalse();
    }

    // ── EditTask tests ───────────────────────────────────────────

    [Fact]
    public async Task EditTask_WhenValidInput_ShouldUpdateTaskText()
    {
        // Arrange
        var task = new TaskItem("Old text", "test-session") { Id = 1 };
        _viewModel.Tasks.Add(task);
        _mockTaskRepository.Setup(x => x.UpdateTaskAsync(It.IsAny<TaskItem>())).Returns(Task.FromResult(true));

        // Act
        await _viewModel.EditTaskCommand.ExecuteAsync((1, "New text"));

        // Assert
        task.Text.Should().Be("New text");
        _mockTaskRepository.Verify(x => x.UpdateTaskAsync(It.IsAny<TaskItem>()), Times.Once);
    }

    [Fact]
    public async Task EditTask_WhenEmptyText_ShouldNotUpdateTask()
    {
        // Arrange
        var task = new TaskItem("Old text", "test-session") { Id = 1 };
        _viewModel.Tasks.Add(task);

        // Act
        await _viewModel.EditTaskCommand.ExecuteAsync((1, ""));

        // Assert
        task.Text.Should().Be("Old text");
        _mockTaskRepository.Verify(x => x.UpdateTaskAsync(It.IsAny<TaskItem>()), Times.Never);
    }

    // ── CanChangeMode tests ──────────────────────────────────────

    [Fact]
    public void CanChangeMode_WhenNotRunning_ShouldBeTrue()
    {
        _viewModel.IsRunning = false;
        _viewModel.CanChangeMode.Should().BeTrue();
    }

    [Fact]
    public void CanChangeMode_WhenRunning_ShouldBeFalse()
    {
        _viewModel.IsRunning = true;
        _viewModel.CanChangeMode.Should().BeFalse();
    }
}
