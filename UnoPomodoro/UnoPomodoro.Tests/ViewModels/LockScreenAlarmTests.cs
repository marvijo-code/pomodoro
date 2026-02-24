using Xunit;
using Moq;
using FluentAssertions;
using UnoPomodoro.ViewModels;
using UnoPomodoro.Data.Models;
using UnoPomodoro.Data.Repositories;
using UnoPomodoro.Services;
using System;
using System.Threading.Tasks;

namespace UnoPomodoro.Tests.ViewModels;

/// <summary>
/// Tests for the lock-screen alarm fix: when the platform's background service
/// already triggers vibration and sound (e.g., phone is locked), the ViewModel
/// should skip starting them again to avoid double-triggering.
/// </summary>
public class LockScreenAlarmTests
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

    public LockScreenAlarmTests()
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

    // ── Platform already started alarm (lock-screen scenario) ────

    [Fact]
    public void TimerCompleted_WhenPlatformAlreadyStartedAlarm_ShouldNotPlaySound()
    {
        // Arrange
        _viewModel.IsRunning = true;
        _viewModel.IsSoundEnabled = true;
        _mockTimerService.Setup(x => x.CompletionAlarmStartedByPlatform).Returns(true);

        // Act
        _mockTimerService.Raise(x => x.TimerCompleted += null, EventArgs.Empty);

        // Assert - sound should NOT be triggered by ViewModel
        _mockSoundService.Verify(x => x.PlayNotificationSound(), Times.Never);
    }

    [Fact]
    public void TimerCompleted_WhenPlatformAlreadyStartedAlarm_ShouldNotVibrate()
    {
        // Arrange
        _viewModel.IsRunning = true;
        _viewModel.IsVibrationEnabled = true;
        _mockVibrationService.Setup(x => x.IsSupported).Returns(true);
        _mockTimerService.Setup(x => x.CompletionAlarmStartedByPlatform).Returns(true);

        // Act
        _mockTimerService.Raise(x => x.TimerCompleted += null, EventArgs.Empty);

        // Assert - vibration should NOT be triggered by ViewModel
        _mockVibrationService.Verify(x => x.VibratePattern(It.IsAny<long[]>(), It.IsAny<bool>()), Times.Never);
    }

    [Fact]
    public void TimerCompleted_WhenPlatformAlreadyStartedAlarm_ShouldStillSetIsRinging()
    {
        // Arrange
        _viewModel.IsRunning = true;
        _viewModel.IsSoundEnabled = true;
        _mockTimerService.Setup(x => x.CompletionAlarmStartedByPlatform).Returns(true);

        // Act
        _mockTimerService.Raise(x => x.TimerCompleted += null, EventArgs.Empty);

        // Assert - IsRinging should still be set so the UI shows the alarm state
        _viewModel.IsRinging.Should().BeTrue();
    }

    [Fact]
    public void TimerCompleted_WhenPlatformAlreadyStartedAlarm_ShouldStillShowCompletionDialog()
    {
        // Arrange
        _viewModel.IsRunning = true;
        _viewModel.IsSoundEnabled = true;
        _mockTimerService.Setup(x => x.CompletionAlarmStartedByPlatform).Returns(true);

        // Act
        _mockTimerService.Raise(x => x.TimerCompleted += null, EventArgs.Empty);

        // Assert - completion dialog should still appear
        _viewModel.ShowCompletionDialog.Should().BeTrue();
        _viewModel.CompletionTitle.Should().Be("Pomodoro Completed!");
    }
    
    [Fact]
    public async Task Constructor_WhenPlatformAlarmAlreadyActive_ShouldShowCompletionDialog()
    {
        // Arrange
        _mockTimerService.Setup(x => x.CompletionAlarmStartedByPlatform).Returns(true);

        var viewModel = new MainViewModel(
            _mockTimerService.Object,
            _mockSessionRepository.Object,
            _mockTaskRepository.Object,
            _mockSoundService.Object,
            _mockNotificationService.Object,
            _mockStatisticsService.Object,
            _mockVibrationService.Object,
            _mockSettingsService.Object);

        // Act
        await Task.Delay(100);

        // Assert
        viewModel.ShowCompletionDialog.Should().BeTrue();
        viewModel.CompletionTitle.Should().Be("Session Completed!");
    }

    // ── Platform did NOT start alarm (normal foreground scenario) ─

    [Fact]
    public void TimerCompleted_WhenPlatformDidNotStartAlarm_ShouldPlaySound()
    {
        // Arrange
        _viewModel.IsRunning = true;
        _viewModel.IsSoundEnabled = true;
        _mockTimerService.Setup(x => x.CompletionAlarmStartedByPlatform).Returns(false);

        // Act
        _mockTimerService.Raise(x => x.TimerCompleted += null, EventArgs.Empty);

        // Assert - ViewModel should trigger sound
        _mockSoundService.Verify(x => x.PlayNotificationSound(), Times.Once);
    }

    [Fact]
    public void TimerCompleted_WhenPlatformDidNotStartAlarm_ShouldVibrate()
    {
        // Arrange
        _viewModel.IsRunning = true;
        _viewModel.IsVibrationEnabled = true;
        _mockVibrationService.Setup(x => x.IsSupported).Returns(true);
        _mockTimerService.Setup(x => x.CompletionAlarmStartedByPlatform).Returns(false);

        // Act
        _mockTimerService.Raise(x => x.TimerCompleted += null, EventArgs.Empty);

        // Assert - ViewModel should trigger vibration
        _mockVibrationService.Verify(x => x.VibratePattern(It.IsAny<long[]>(), true), Times.Once);
    }

    [Fact]
    public void TimerCompleted_DefaultMockReturnsfalse_ShouldPlaySoundNormally()
    {
        // Moq returns false by default for bool properties, so existing tests
        // (without explicit setup) should continue to work - ViewModel triggers alarm.
        // Arrange
        _viewModel.IsRunning = true;
        _viewModel.IsSoundEnabled = true;
        // No explicit setup for CompletionAlarmStartedByPlatform — defaults to false

        // Act
        _mockTimerService.Raise(x => x.TimerCompleted += null, EventArgs.Empty);

        // Assert
        _mockSoundService.Verify(x => x.PlayNotificationSound(), Times.Once);
    }

    // ── Settings propagation to platform service ─────────────────

    [Fact]
    public void OnIsSoundEnabledChanged_ShouldCallUpdateAlarmSettings()
    {
        // Arrange - initial state
        _viewModel.IsVibrationEnabled = true;

        // Act
        _viewModel.IsSoundEnabled = false;

        // Assert - should propagate the new sound setting + current vibration setting
        _mockTimerService.Verify(
            x => x.UpdateAlarmSettings(false, true, It.IsAny<int>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public void OnIsVibrationEnabledChanged_ShouldCallUpdateAlarmSettings()
    {
        // Arrange - initial state
        _viewModel.IsSoundEnabled = true;

        // Act
        _viewModel.IsVibrationEnabled = true;

        // Assert - should propagate current sound setting + new vibration setting
        _mockTimerService.Verify(
            x => x.UpdateAlarmSettings(true, true, It.IsAny<int>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public void OnIsVibrationEnabledChanged_WhenDisabled_ShouldCallUpdateAlarmSettings()
    {
        // Arrange
        _viewModel.IsSoundEnabled = false;

        // Act
        _viewModel.IsVibrationEnabled = false;

        // Assert
        _mockTimerService.Verify(
            x => x.UpdateAlarmSettings(false, false, It.IsAny<int>()),
            Times.AtLeastOnce);
    }

    // ── Combined scenarios ───────────────────────────────────────

    [Fact]
    public void TimerCompleted_WhenPlatformStartedAlarm_SoundDisabled_ShouldNotRing()
    {
        // Arrange
        _viewModel.IsRunning = true;
        _viewModel.IsSoundEnabled = false;
        _mockTimerService.Setup(x => x.CompletionAlarmStartedByPlatform).Returns(true);

        // Act
        _mockTimerService.Raise(x => x.TimerCompleted += null, EventArgs.Empty);

        // Assert
        _viewModel.IsRinging.Should().BeFalse();
        _mockSoundService.Verify(x => x.PlayNotificationSound(), Times.Never);
    }

    [Fact]
    public void TimerCompleted_WhenPlatformStartedAlarm_BothEnabled_ShouldSkipBothButStillRing()
    {
        // Arrange
        _viewModel.IsRunning = true;
        _viewModel.IsSoundEnabled = true;
        _viewModel.IsVibrationEnabled = true;
        _mockVibrationService.Setup(x => x.IsSupported).Returns(true);
        _mockTimerService.Setup(x => x.CompletionAlarmStartedByPlatform).Returns(true);

        // Act
        _mockTimerService.Raise(x => x.TimerCompleted += null, EventArgs.Empty);

        // Assert - both sound and vibration should be skipped (platform handled them)
        _mockSoundService.Verify(x => x.PlayNotificationSound(), Times.Never);
        _mockVibrationService.Verify(x => x.VibratePattern(It.IsAny<long[]>(), It.IsAny<bool>()), Times.Never);
        // But IsRinging is still true so the UI shows the dismiss button
        _viewModel.IsRinging.Should().BeTrue();
        _viewModel.ShowCompletionDialog.Should().BeTrue();
    }

    [Fact]
    public void TimerCompleted_WhenPlatformDidNotStartAlarm_BothEnabled_ShouldTriggerBoth()
    {
        // Arrange
        _viewModel.IsRunning = true;
        _viewModel.IsSoundEnabled = true;
        _viewModel.IsVibrationEnabled = true;
        _mockVibrationService.Setup(x => x.IsSupported).Returns(true);
        _mockTimerService.Setup(x => x.CompletionAlarmStartedByPlatform).Returns(false);

        // Act
        _mockTimerService.Raise(x => x.TimerCompleted += null, EventArgs.Empty);

        // Assert - ViewModel should trigger both
        _mockSoundService.Verify(x => x.PlayNotificationSound(), Times.Once);
        _mockVibrationService.Verify(x => x.VibratePattern(It.IsAny<long[]>(), true), Times.Once);
        _viewModel.IsRinging.Should().BeTrue();
    }

    [Fact]
    public void TimerCompleted_WhenPlatformAlreadyStartedAlarm_ShouldStillIncrementPomodoroCount()
    {
        // Arrange
        _viewModel.IsRunning = true;
        _viewModel.Mode = "pomodoro";
        _viewModel.PomodoroCount = 2;
        _mockTimerService.Setup(x => x.CompletionAlarmStartedByPlatform).Returns(true);

        // Act
        _mockTimerService.Raise(x => x.TimerCompleted += null, EventArgs.Empty);

        // Assert - pomodoro count should still increment regardless of alarm source
        _viewModel.PomodoroCount.Should().Be(3);
    }
}
