using Xunit;
using Moq;
using FluentAssertions;
using UnoPomodoro.ViewModels;
using UnoPomodoro.Data.Models;
using UnoPomodoro.Data.Repositories;
using UnoPomodoro.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace UnoPomodoro.Tests.ViewModels;

/// <summary>
/// Tests for the 10 enterprise-level features added to MainViewModel:
/// 1. Task Priority Levels
/// 2. Focus Streak Protection
/// 3. Session Tagging/Labeling
/// 4. Distraction Counter
/// 5. Pomodoro Templates
/// 6. Daily Focus Quota
/// 7. Task Time Estimation vs Actual
/// 8. Break Activity Suggestions
/// 9. Session Rating/Retrospective
/// 10. Bulk Task Operations
/// </summary>
public class EnterpriseFeatureTests
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

    public EnterpriseFeatureTests()
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

    // ═════════════════════════════════════════════════════════════
    // Feature 1: Task Priority Levels
    // ═════════════════════════════════════════════════════════════

    [Fact]
    public void TaskPriority_Enum_ShouldHaveCorrectValues()
    {
        ((int)TaskPriority.High).Should().Be(0);
        ((int)TaskPriority.Medium).Should().Be(1);
        ((int)TaskPriority.Low).Should().Be(2);
        ((int)TaskPriority.None).Should().Be(3);
    }

    [Fact]
    public void SetTaskPriority_ShouldUpdateTaskPriority()
    {
        // Arrange
        var task = new TaskItem("Test task", "session-1") { Id = 1, Priority = TaskPriority.None };
        _viewModel.Tasks.Add(task);
        _mockTaskRepository.Setup(x => x.UpdateTaskAsync(It.IsAny<TaskItem>())).Returns(Task.FromResult(true));

        // Act
        _viewModel.SetTaskPriorityCommand.Execute((1, TaskPriority.High));

        // Assert
        task.Priority.Should().Be(TaskPriority.High);
        _mockTaskRepository.Verify(x => x.UpdateTaskAsync(It.Is<TaskItem>(t => t.Priority == TaskPriority.High)), Times.Once);
    }

    [Fact]
    public void SetTaskPriority_WhenTaskNotFound_ShouldDoNothing()
    {
        // Act — no tasks in collection
        _viewModel.SetTaskPriorityCommand.Execute((999, TaskPriority.High));

        // Assert
        _mockTaskRepository.Verify(x => x.UpdateTaskAsync(It.IsAny<TaskItem>()), Times.Never);
    }

    [Fact]
    public void SortTasksByPriority_ShouldOrderByPriorityThenSortOrder()
    {
        // Arrange
        var task1 = new TaskItem("Low task", "s1") { Id = 1, Priority = TaskPriority.Low, SortOrder = 0 };
        var task2 = new TaskItem("High task", "s1") { Id = 2, Priority = TaskPriority.High, SortOrder = 1 };
        var task3 = new TaskItem("Medium task", "s1") { Id = 3, Priority = TaskPriority.Medium, SortOrder = 2 };
        _viewModel.Tasks.Add(task1);
        _viewModel.Tasks.Add(task2);
        _viewModel.Tasks.Add(task3);

        // Act
        _viewModel.SortTasksByPriorityCommand.Execute(null);

        // Assert
        _viewModel.Tasks[0].Priority.Should().Be(TaskPriority.High);
        _viewModel.Tasks[1].Priority.Should().Be(TaskPriority.Medium);
        _viewModel.Tasks[2].Priority.Should().Be(TaskPriority.Low);
    }

    [Fact]
    public void SortTasksByPriority_SamePriority_ShouldPreserveSortOrder()
    {
        // Arrange
        var task1 = new TaskItem("First", "s1") { Id = 1, Priority = TaskPriority.High, SortOrder = 2 };
        var task2 = new TaskItem("Second", "s1") { Id = 2, Priority = TaskPriority.High, SortOrder = 0 };
        _viewModel.Tasks.Add(task1);
        _viewModel.Tasks.Add(task2);

        // Act
        _viewModel.SortTasksByPriorityCommand.Execute(null);

        // Assert — sort order 0 first
        _viewModel.Tasks[0].Id.Should().Be(2);
        _viewModel.Tasks[1].Id.Should().Be(1);
    }

    [Fact]
    public void DefaultTaskPriority_WhenSetToNone_ShouldBe3()
    {
        // SetupAllProperties defaults int to 0, so set explicitly
        _viewModel.DefaultTaskPriority = 3; // TaskPriority.None
        _viewModel.DefaultTaskPriority.Should().Be(3);
    }

    [Fact]
    public void DefaultTaskPriority_WhenChanged_ShouldPersist()
    {
        // Act
        _viewModel.DefaultTaskPriority = 0; // High

        // Assert
        _mockSettingsService.VerifySet(x => x.DefaultTaskPriority = 0, Times.Once);
        _mockSettingsService.Verify(x => x.SaveAsync(), Times.AtLeastOnce);
    }

    [Fact]
    public void DefaultTaskPriority_ShouldClampToValidRange()
    {
        // Act — set out-of-range value
        _viewModel.DefaultTaskPriority = 10;

        // Assert — should be clamped to 3 (max)
        _viewModel.DefaultTaskPriority.Should().Be(3);
    }

    // ═════════════════════════════════════════════════════════════
    // Feature 2: Focus Streak Protection
    // ═════════════════════════════════════════════════════════════

    [Fact]
    public void CheckStreakProtection_WhenStreakActive_ShouldShowWarning()
    {
        // Arrange
        _viewModel.IsStreakProtectionEnabled = true;
        _viewModel.CurrentStreak = 5;

        // Act
        _viewModel.CheckStreakProtection();

        // Assert
        _viewModel.ShowStreakWarning.Should().BeTrue();
        _viewModel.StreakWarningMessage.Should().Contain("5-day streak");
    }

    [Fact]
    public void CheckStreakProtection_WhenNoStreak_ShouldNotShowWarning()
    {
        // Arrange
        _viewModel.IsStreakProtectionEnabled = true;
        _viewModel.CurrentStreak = 0;

        // Act
        _viewModel.CheckStreakProtection();

        // Assert
        _viewModel.ShowStreakWarning.Should().BeFalse();
    }

    [Fact]
    public void CheckStreakProtection_WhenDisabled_ShouldNotShowWarning()
    {
        // Arrange
        _viewModel.IsStreakProtectionEnabled = false;
        _viewModel.CurrentStreak = 10;

        // Act
        _viewModel.CheckStreakProtection();

        // Assert
        _viewModel.ShowStreakWarning.Should().BeFalse();
    }

    [Fact]
    public void DismissStreakWarning_ShouldHideWarning()
    {
        // Arrange
        _viewModel.ShowStreakWarning = true;
        _viewModel.StreakWarningMessage = "Some warning";

        // Act
        _viewModel.DismissStreakWarningCommand.Execute(null);

        // Assert
        _viewModel.ShowStreakWarning.Should().BeFalse();
        _viewModel.StreakWarningMessage.Should().BeEmpty();
    }

    [Fact]
    public void ToggleTimer_WhenStreakWarningShown_ShouldBlockStart()
    {
        // Arrange
        _viewModel.IsStreakProtectionEnabled = true;
        _viewModel.CurrentStreak = 3;
        _viewModel.ShowStreakWarning = true;
        _viewModel.Mode = "pomodoro";

        // Act
        _viewModel.ToggleTimerCommand.Execute(null);

        // Assert — should NOT start
        _viewModel.IsRunning.Should().BeFalse();
        _mockTimerService.Verify(x => x.Start(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public void IsStreakProtectionEnabled_WhenChanged_ShouldPersist()
    {
        // Act
        _viewModel.IsStreakProtectionEnabled = true;

        // Assert
        _mockSettingsService.VerifySet(x => x.IsStreakProtectionEnabled = true, Times.Once);
        _mockSettingsService.Verify(x => x.SaveAsync(), Times.AtLeastOnce);
    }

    // ═════════════════════════════════════════════════════════════
    // Feature 3: Session Tagging / Labeling
    // ═════════════════════════════════════════════════════════════

    [Fact]
    public void SessionTag_ShouldDefaultToEmpty()
    {
        _viewModel.SessionTag.Should().BeEmpty();
    }

    [Fact]
    public void SessionTag_ShouldBeSettable()
    {
        // Act
        _viewModel.SessionTag = "Deep Work";

        // Assert
        _viewModel.SessionTag.Should().Be("Deep Work");
    }

    [Fact]
    public void SessionTag_ShouldResetOnNewSession()
    {
        // Arrange
        _viewModel.SessionTag = "Old Tag";
        _viewModel.SessionId = "session-1";

        // Act
        _viewModel.StartNewSessionCommand.Execute(null);

        // Assert
        _viewModel.SessionTag.Should().BeEmpty();
    }

    [Fact]
    public void Session_Model_ShouldHaveTagProperty()
    {
        // Arrange
        var session = new Session("id", "pomodoro", DateTime.Now);

        // Act
        session.Tag = "Project Alpha";

        // Assert
        session.Tag.Should().Be("Project Alpha");
    }

    // ═════════════════════════════════════════════════════════════
    // Feature 4: Distraction Counter
    // ═════════════════════════════════════════════════════════════

    [Fact]
    public void DistractionCount_ShouldDefaultToZero()
    {
        _viewModel.DistractionCount.Should().Be(0);
    }

    [Fact]
    public void LogDistraction_ShouldIncrementCount()
    {
        // Act
        _viewModel.LogDistractionCommand.Execute(null);
        _viewModel.LogDistractionCommand.Execute(null);
        _viewModel.LogDistractionCommand.Execute(null);

        // Assert
        _viewModel.DistractionCount.Should().Be(3);
    }

    [Fact]
    public void DistractionCount_ShouldResetOnNewSession()
    {
        // Arrange
        _viewModel.DistractionCount = 5;
        _viewModel.SessionId = "session-1";

        // Act
        _viewModel.StartNewSessionCommand.Execute(null);

        // Assert
        _viewModel.DistractionCount.Should().Be(0);
    }

    [Fact]
    public void Session_Model_ShouldHaveDistractionCountProperty()
    {
        var session = new Session("id", "pomodoro", DateTime.Now);
        session.DistractionCount = 7;
        session.DistractionCount.Should().Be(7);
    }

    // ═════════════════════════════════════════════════════════════
    // Feature 5: Pomodoro Templates
    // ═════════════════════════════════════════════════════════════

    [Fact]
    public void Templates_ShouldBeInitializedWithDefaults()
    {
        // The constructor calls InitializeFromSettingsAsync -> ApplySettingsToViewModel
        // -> InitializeDefaultTemplates, so templates should already be populated
        _viewModel.Templates.Should().HaveCount(3);
        _viewModel.Templates.Select(t => t.Name).Should().Contain("Standard");
        _viewModel.Templates.Select(t => t.Name).Should().Contain("Deep Work");
        _viewModel.Templates.Select(t => t.Name).Should().Contain("Quick Sprint");
    }

    [Fact]
    public void Templates_Standard_ShouldHaveCorrectDurations()
    {
        var standard = _viewModel.Templates.FirstOrDefault(t => t.Name == "Standard");
        standard.Should().NotBeNull();
        standard!.PomodoroDuration.Should().Be(25);
        standard.ShortBreakDuration.Should().Be(5);
        standard.LongBreakDuration.Should().Be(15);
        standard.PomodorosBeforeLongBreak.Should().Be(4);
    }

    [Fact]
    public void Templates_DeepWork_ShouldHaveCorrectDurations()
    {
        var deepWork = _viewModel.Templates.FirstOrDefault(t => t.Name == "Deep Work");
        deepWork.Should().NotBeNull();
        deepWork!.PomodoroDuration.Should().Be(50);
        deepWork.ShortBreakDuration.Should().Be(10);
        deepWork.LongBreakDuration.Should().Be(30);
        deepWork.PomodorosBeforeLongBreak.Should().Be(3);
    }

    [Fact]
    public void Templates_QuickSprint_ShouldHaveCorrectDurations()
    {
        var sprint = _viewModel.Templates.FirstOrDefault(t => t.Name == "Quick Sprint");
        sprint.Should().NotBeNull();
        sprint!.PomodoroDuration.Should().Be(15);
        sprint.ShortBreakDuration.Should().Be(3);
        sprint.LongBreakDuration.Should().Be(10);
        sprint.PomodorosBeforeLongBreak.Should().Be(4);
    }

    [Fact]
    public void ApplyTemplate_ShouldUpdateDurations()
    {
        // Act
        _viewModel.ApplyTemplateCommand.Execute("Deep Work");

        // Assert
        _viewModel.ActiveTemplateName.Should().Be("Deep Work");
        _viewModel.PomodoroDuration.Should().Be(50);
        _viewModel.ShortBreakDuration.Should().Be(10);
        _viewModel.LongBreakDuration.Should().Be(30);
        _viewModel.PomodorosBeforeLongBreak.Should().Be(3);
    }

    [Fact]
    public void ApplyTemplate_WhenRunning_ShouldNotApply()
    {
        // Arrange
        _viewModel.IsRunning = true;
        var originalDuration = _viewModel.PomodoroDuration;

        // Act
        _viewModel.ApplyTemplateCommand.Execute("Deep Work");

        // Assert — duration unchanged
        _viewModel.PomodoroDuration.Should().Be(originalDuration);
    }

    [Fact]
    public void ApplyTemplate_WhenTemplateNotFound_ShouldDoNothing()
    {
        // Arrange
        var originalDuration = _viewModel.PomodoroDuration;

        // Act
        _viewModel.ApplyTemplateCommand.Execute("NonExistent");

        // Assert
        _viewModel.PomodoroDuration.Should().Be(originalDuration);
    }

    [Fact]
    public void ApplyTemplate_ShouldUpdateTimeLeft()
    {
        // Act
        _viewModel.ApplyTemplateCommand.Execute("Quick Sprint");

        // Assert
        _viewModel.TimeLeft.Should().Be(15 * 60);
    }

    // ═════════════════════════════════════════════════════════════
    // Feature 6: Daily Focus Quota
    // ═════════════════════════════════════════════════════════════

    [Fact]
    public void DailyFocusQuotaMinutes_ShouldDefaultToZero()
    {
        _viewModel.DailyFocusQuotaMinutes.Should().Be(0);
    }

    [Fact]
    public void DailyFocusQuotaMinutes_WhenChanged_ShouldPersist()
    {
        // Act
        _viewModel.DailyFocusQuotaMinutes = 120;

        // Assert
        _mockSettingsService.VerifySet(x => x.DailyFocusQuotaMinutes = 120, Times.Once);
        _mockSettingsService.Verify(x => x.SaveAsync(), Times.AtLeastOnce);
    }

    [Fact]
    public void DailyFocusQuotaMinutes_ShouldClampToValidRange()
    {
        // Act — over max
        _viewModel.DailyFocusQuotaMinutes = 1000;

        // Assert
        _viewModel.DailyFocusQuotaMinutes.Should().Be(720);
    }

    [Fact]
    public void DailyFocusQuotaMinutes_ShouldClampNegative()
    {
        // Act
        _viewModel.DailyFocusQuotaMinutes = -10;

        // Assert
        _viewModel.DailyFocusQuotaMinutes.Should().Be(0);
    }

    [Fact]
    public void IsDailyQuotaExceeded_WhenQuotaZero_ShouldBeFalse()
    {
        // Arrange
        _viewModel.DailyFocusQuotaMinutes = 0;

        // Assert — zero means unlimited
        _viewModel.IsDailyQuotaExceeded.Should().BeFalse();
    }

    [Fact]
    public void ToggleTimer_WhenQuotaExceeded_ShouldBlockStart()
    {
        // Arrange
        _viewModel.DailyFocusQuotaMinutes = 60;
        _viewModel.IsDailyQuotaExceeded = true;
        _viewModel.Mode = "pomodoro";

        // Act
        _viewModel.ToggleTimerCommand.Execute(null);

        // Assert — should NOT start
        _viewModel.IsRunning.Should().BeFalse();
        _mockTimerService.Verify(x => x.Start(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public void ToggleTimer_WhenQuotaNotExceeded_ShouldStart()
    {
        // Arrange
        _viewModel.DailyFocusQuotaMinutes = 120;
        _viewModel.IsDailyQuotaExceeded = false;
        _viewModel.Mode = "pomodoro";

        _mockSessionRepository.Setup(x => x.CreateSession(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>()))
            .ReturnsAsync(new Session());
        _mockSessionRepository.Setup(x => x.GetSessionsWithStats())
            .ReturnsAsync(new List<Session>());

        // Act
        _viewModel.ToggleTimerCommand.Execute(null);

        // Assert
        _viewModel.IsRunning.Should().BeTrue();
    }

    [Fact]
    public void QuotaStatusText_WhenQuotaZero_ShouldBeEmpty()
    {
        // Arrange
        _viewModel.DailyFocusQuotaMinutes = 0;

        // Assert
        _viewModel.QuotaStatusText.Should().BeEmpty();
    }

    // ═════════════════════════════════════════════════════════════
    // Feature 7: Task Time Estimation vs Actual
    // ═════════════════════════════════════════════════════════════

    [Fact]
    public void SetTaskEstimate_ShouldUpdateEstimatedPomodoros()
    {
        // Arrange
        var task = new TaskItem("Test", "s1") { Id = 1 };
        _viewModel.Tasks.Add(task);
        _mockTaskRepository.Setup(x => x.UpdateTaskAsync(It.IsAny<TaskItem>())).Returns(Task.FromResult(true));

        // Act
        _viewModel.SetTaskEstimateCommand.Execute((1, 3));

        // Assert
        task.EstimatedPomodoros.Should().Be(3);
        _mockTaskRepository.Verify(x => x.UpdateTaskAsync(It.Is<TaskItem>(t => t.EstimatedPomodoros == 3)), Times.Once);
    }

    [Fact]
    public void SetTaskEstimate_NegativeValue_ShouldClampToZero()
    {
        // Arrange
        var task = new TaskItem("Test", "s1") { Id = 1 };
        _viewModel.Tasks.Add(task);
        _mockTaskRepository.Setup(x => x.UpdateTaskAsync(It.IsAny<TaskItem>())).Returns(Task.FromResult(true));

        // Act
        _viewModel.SetTaskEstimateCommand.Execute((1, -5));

        // Assert
        task.EstimatedPomodoros.Should().Be(0);
    }

    [Fact]
    public void SetTaskEstimate_WhenTaskNotFound_ShouldDoNothing()
    {
        // Act
        _viewModel.SetTaskEstimateCommand.Execute((999, 3));

        // Assert
        _mockTaskRepository.Verify(x => x.UpdateTaskAsync(It.IsAny<TaskItem>()), Times.Never);
    }

    [Fact]
    public void EstimationAccuracyText_WhenNoEstimates_ShouldBeEmpty()
    {
        // Arrange
        _viewModel.Tasks.Add(new TaskItem("Task", "s1") { Id = 1, EstimatedPomodoros = 0 });

        // The property should default to empty since no estimates are set
        // Triggering an update by setting estimate to 0 on an already-0 task
        _viewModel.EstimationAccuracyText.Should().BeEmpty();
    }

    [Fact]
    public void EstimationAccuracyText_WhenEstimatesExist_ShouldShowAccuracy()
    {
        // Arrange
        var task = new TaskItem("Task", "s1") { Id = 1, EstimatedPomodoros = 4, ActualPomodoros = 3 };
        _viewModel.Tasks.Add(task);
        _mockTaskRepository.Setup(x => x.UpdateTaskAsync(It.IsAny<TaskItem>())).Returns(Task.FromResult(true));

        // Act — trigger recalculation by setting estimate
        _viewModel.SetTaskEstimateCommand.Execute((1, 4));

        // Assert
        _viewModel.EstimationAccuracyText.Should().Contain("Est: 4");
        _viewModel.EstimationAccuracyText.Should().Contain("Actual: 3");
        _viewModel.EstimationAccuracyText.Should().Contain("75%");
    }

    [Fact]
    public void TimerCompleted_ShouldIncrementActualPomodorosOnIncompleteTasks()
    {
        // Arrange
        var task1 = new TaskItem("Incomplete", "s1") { Id = 1, Completed = false, ActualPomodoros = 0 };
        var task2 = new TaskItem("Complete", "s1") { Id = 2, Completed = true, ActualPomodoros = 0 };
        _viewModel.Tasks.Add(task1);
        _viewModel.Tasks.Add(task2);
        _viewModel.IsRunning = true;
        _viewModel.Mode = "pomodoro";

        // Act
        _mockTimerService.Raise(x => x.TimerCompleted += null, EventArgs.Empty);

        // Assert — only incomplete task should have actual incremented
        task1.ActualPomodoros.Should().Be(1);
        task2.ActualPomodoros.Should().Be(0);
    }

    [Fact]
    public void TaskItem_ShouldHaveEstimationFields()
    {
        var task = new TaskItem("Test", "s1");
        task.EstimatedPomodoros.Should().Be(0);
        task.ActualPomodoros.Should().Be(0);
        task.SortOrder.Should().Be(0);
    }

    // ═════════════════════════════════════════════════════════════
    // Feature 8: Break Activity Suggestions
    // ═════════════════════════════════════════════════════════════

    [Fact]
    public void IsBreakSuggestionsEnabled_ShouldDefaultToFalse()
    {
        _viewModel.IsBreakSuggestionsEnabled.Should().BeFalse();
    }

    [Fact]
    public void IsBreakSuggestionsEnabled_WhenChanged_ShouldPersist()
    {
        // Act
        _viewModel.IsBreakSuggestionsEnabled = true;

        // Assert
        _mockSettingsService.VerifySet(x => x.IsBreakSuggestionsEnabled = true, Times.Once);
        _mockSettingsService.Verify(x => x.SaveAsync(), Times.AtLeastOnce);
    }

    [Fact]
    public void TimerCompleted_WithBreakSuggestionsEnabled_ShouldGenerateSuggestion()
    {
        // Arrange
        _viewModel.IsRunning = true;
        _viewModel.Mode = "pomodoro";
        _viewModel.IsBreakSuggestionsEnabled = true;

        // Act
        _mockTimerService.Raise(x => x.TimerCompleted += null, EventArgs.Empty);

        // Assert
        _viewModel.BreakSuggestion.Should().NotBeEmpty();
    }

    [Fact]
    public void TimerCompleted_WithBreakSuggestionsDisabled_ShouldNotGenerateSuggestion()
    {
        // Arrange
        _viewModel.IsRunning = true;
        _viewModel.Mode = "pomodoro";
        _viewModel.IsBreakSuggestionsEnabled = false;
        _viewModel.BreakSuggestion = "";

        // Act
        _mockTimerService.Raise(x => x.TimerCompleted += null, EventArgs.Empty);

        // Assert
        _viewModel.BreakSuggestion.Should().BeEmpty();
    }

    [Fact]
    public void TimerCompleted_BreakSuggestions_ShouldCycleThroughDifferentSuggestions()
    {
        // Arrange
        _viewModel.IsBreakSuggestionsEnabled = true;

        // Act — complete two pomodoros to get two suggestions
        _viewModel.IsRunning = true;
        _viewModel.Mode = "pomodoro";
        _mockTimerService.Raise(x => x.TimerCompleted += null, EventArgs.Empty);
        var first = _viewModel.BreakSuggestion;

        // Reset for next pomodoro
        _viewModel.IsRunning = true;
        _viewModel.Mode = "pomodoro";
        // Need to reset the completion-handled flag
        typeof(MainViewModel)
            .GetField("_completionHandled", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .SetValue(_viewModel, false);

        _mockTimerService.Raise(x => x.TimerCompleted += null, EventArgs.Empty);
        var second = _viewModel.BreakSuggestion;

        // Assert — should be different suggestions
        second.Should().NotBe(first);
    }

    [Fact]
    public void TimerCompleted_OnBreak_ShouldNotGenerateBreakSuggestion()
    {
        // Arrange — mode is break, not pomodoro
        _viewModel.IsRunning = true;
        _viewModel.Mode = "shortBreak";
        _viewModel.IsBreakSuggestionsEnabled = true;
        _viewModel.BreakSuggestion = "";

        // Act
        _mockTimerService.Raise(x => x.TimerCompleted += null, EventArgs.Empty);

        // Assert — no suggestion generated for breaks
        _viewModel.BreakSuggestion.Should().BeEmpty();
    }

    // ═════════════════════════════════════════════════════════════
    // Feature 9: Session Rating / Retrospective
    // ═════════════════════════════════════════════════════════════

    [Fact]
    public void IsRetroPromptEnabled_ShouldDefaultToFalse()
    {
        _viewModel.IsRetroPromptEnabled.Should().BeFalse();
    }

    [Fact]
    public void IsRetroPromptEnabled_WhenChanged_ShouldPersist()
    {
        // Act
        _viewModel.IsRetroPromptEnabled = true;

        // Assert
        _mockSettingsService.VerifySet(x => x.IsRetroPromptEnabled = true, Times.Once);
        _mockSettingsService.Verify(x => x.SaveAsync(), Times.AtLeastOnce);
    }

    [Fact]
    public void TimerCompleted_WithRetroEnabled_ShouldShowRetroPrompt()
    {
        // Arrange
        _viewModel.IsRunning = true;
        _viewModel.Mode = "pomodoro";
        _viewModel.IsRetroPromptEnabled = true;

        // Act
        _mockTimerService.Raise(x => x.TimerCompleted += null, EventArgs.Empty);

        // Assert
        _viewModel.ShowRetroPrompt.Should().BeTrue();
    }

    [Fact]
    public void TimerCompleted_WithRetroDisabled_ShouldNotShowRetroPrompt()
    {
        // Arrange
        _viewModel.IsRunning = true;
        _viewModel.Mode = "pomodoro";
        _viewModel.IsRetroPromptEnabled = false;

        // Act
        _mockTimerService.Raise(x => x.TimerCompleted += null, EventArgs.Empty);

        // Assert
        _viewModel.ShowRetroPrompt.Should().BeFalse();
    }

    [Fact]
    public void SessionRating_ShouldDefaultToZero()
    {
        _viewModel.SessionRating.Should().Be(0);
    }

    [Fact]
    public void SessionRating_ShouldBeSettable()
    {
        _viewModel.SessionRating = 4;
        _viewModel.SessionRating.Should().Be(4);
    }

    [Fact]
    public void RetroNote_ShouldDefaultToEmpty()
    {
        _viewModel.RetroNote.Should().BeEmpty();
    }

    [Fact]
    public void RetroNote_ShouldBeSettable()
    {
        _viewModel.RetroNote = "Good focus today";
        _viewModel.RetroNote.Should().Be("Good focus today");
    }

    [Fact]
    public async Task SaveRetro_ShouldHideRetroPrompt()
    {
        // Arrange
        _viewModel.ShowRetroPrompt = true;
        _viewModel.SessionId = "session-1";
        _viewModel.SessionRating = 5;
        _viewModel.RetroNote = "Great session";

        _mockSessionRepository.Setup(x => x.GetSessionsWithStats())
            .ReturnsAsync(new List<Session> { new Session("session-1", "pomodoro", DateTime.Now) });

        // Act
        await _viewModel.SaveRetroCommand.ExecuteAsync(null);

        // Assert
        _viewModel.ShowRetroPrompt.Should().BeFalse();
    }

    [Fact]
    public void SessionRating_ShouldResetOnNewSession()
    {
        // Arrange
        _viewModel.SessionRating = 4;
        _viewModel.RetroNote = "Some note";
        _viewModel.SessionId = "session-1";

        // Act
        _viewModel.StartNewSessionCommand.Execute(null);

        // Assert
        _viewModel.SessionRating.Should().Be(0);
        _viewModel.RetroNote.Should().BeEmpty();
    }

    [Fact]
    public void ShowRetroPrompt_ShouldResetOnNewSession()
    {
        // Arrange
        _viewModel.ShowRetroPrompt = true;
        _viewModel.SessionId = "session-1";

        // Act
        _viewModel.StartNewSessionCommand.Execute(null);

        // Assert
        _viewModel.ShowRetroPrompt.Should().BeFalse();
    }

    [Fact]
    public void Session_Model_ShouldHaveRatingAndRetroFields()
    {
        var session = new Session("id", "pomodoro", DateTime.Now);
        session.Rating = 5;
        session.RetroNote = "Great focus";
        session.Rating.Should().Be(5);
        session.RetroNote.Should().Be("Great focus");
    }

    // ═════════════════════════════════════════════════════════════
    // Feature 10: Bulk Task Operations
    // ═════════════════════════════════════════════════════════════

    [Fact]
    public async Task CompleteAllTasks_ShouldMarkAllTasksAsCompleted()
    {
        // Arrange
        var task1 = new TaskItem("Task 1", "s1") { Id = 1, Completed = false };
        var task2 = new TaskItem("Task 2", "s1") { Id = 2, Completed = false };
        var task3 = new TaskItem("Task 3", "s1") { Id = 3, Completed = true }; // already complete
        _viewModel.Tasks.Add(task1);
        _viewModel.Tasks.Add(task2);
        _viewModel.Tasks.Add(task3);

        _mockTaskRepository.Setup(x => x.ToggleCompleted(It.IsAny<int>(), true))
            .ReturnsAsync((int id, bool c) => new TaskItem());

        // Act
        await _viewModel.CompleteAllTasksCommand.ExecuteAsync(null);

        // Assert
        _viewModel.Tasks.Should().OnlyContain(t => t.Completed);
        task1.CompletedAt.Should().NotBeNull();
        task2.CompletedAt.Should().NotBeNull();
        // Only the 2 incomplete tasks should have been toggled
        _mockTaskRepository.Verify(x => x.ToggleCompleted(It.IsAny<int>(), true), Times.Exactly(2));
    }

    [Fact]
    public async Task CompleteAllTasks_WhenAllAlreadyComplete_ShouldDoNothing()
    {
        // Arrange
        var task = new TaskItem("Done", "s1") { Id = 1, Completed = true };
        _viewModel.Tasks.Add(task);

        // Act
        await _viewModel.CompleteAllTasksCommand.ExecuteAsync(null);

        // Assert
        _mockTaskRepository.Verify(x => x.ToggleCompleted(It.IsAny<int>(), It.IsAny<bool>()), Times.Never);
    }

    [Fact]
    public async Task DeleteCompletedTasks_ShouldRemoveOnlyCompletedTasks()
    {
        // Arrange
        var task1 = new TaskItem("Incomplete", "s1") { Id = 1, Completed = false };
        var task2 = new TaskItem("Complete 1", "s1") { Id = 2, Completed = true };
        var task3 = new TaskItem("Complete 2", "s1") { Id = 3, Completed = true };
        _viewModel.Tasks.Add(task1);
        _viewModel.Tasks.Add(task2);
        _viewModel.Tasks.Add(task3);

        _mockTaskRepository.Setup(x => x.Delete(It.IsAny<int>())).Returns(Task.CompletedTask);

        // Act
        await _viewModel.DeleteCompletedTasksCommand.ExecuteAsync(null);

        // Assert
        _viewModel.Tasks.Should().HaveCount(1);
        _viewModel.Tasks[0].Text.Should().Be("Incomplete");
        _mockTaskRepository.Verify(x => x.Delete(2), Times.Once);
        _mockTaskRepository.Verify(x => x.Delete(3), Times.Once);
    }

    [Fact]
    public async Task DeleteCompletedTasks_WhenNoCompletedTasks_ShouldDoNothing()
    {
        // Arrange
        var task = new TaskItem("Incomplete", "s1") { Id = 1, Completed = false };
        _viewModel.Tasks.Add(task);

        // Act
        await _viewModel.DeleteCompletedTasksCommand.ExecuteAsync(null);

        // Assert
        _viewModel.Tasks.Should().HaveCount(1);
        _mockTaskRepository.Verify(x => x.Delete(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public void ReorderTask_ShouldMoveTaskToNewPosition()
    {
        // Arrange
        var task1 = new TaskItem("First", "s1") { Id = 1 };
        var task2 = new TaskItem("Second", "s1") { Id = 2 };
        var task3 = new TaskItem("Third", "s1") { Id = 3 };
        _viewModel.Tasks.Add(task1);
        _viewModel.Tasks.Add(task2);
        _viewModel.Tasks.Add(task3);

        // Act — move task 3 to position 0
        _viewModel.ReorderTaskCommand.Execute((3, 0));

        // Assert
        _viewModel.Tasks[0].Id.Should().Be(3);
        _viewModel.Tasks[1].Id.Should().Be(1);
        _viewModel.Tasks[2].Id.Should().Be(2);
    }

    [Fact]
    public void ReorderTask_ShouldUpdateSortOrders()
    {
        // Arrange
        var task1 = new TaskItem("A", "s1") { Id = 1 };
        var task2 = new TaskItem("B", "s1") { Id = 2 };
        var task3 = new TaskItem("C", "s1") { Id = 3 };
        _viewModel.Tasks.Add(task1);
        _viewModel.Tasks.Add(task2);
        _viewModel.Tasks.Add(task3);

        // Act — move task 1 to position 2
        _viewModel.ReorderTaskCommand.Execute((1, 2));

        // Assert — sort orders should be sequential
        _viewModel.Tasks[0].SortOrder.Should().Be(0);
        _viewModel.Tasks[1].SortOrder.Should().Be(1);
        _viewModel.Tasks[2].SortOrder.Should().Be(2);
    }

    [Fact]
    public void ReorderTask_WhenTaskNotFound_ShouldDoNothing()
    {
        // Arrange
        var task = new TaskItem("Only", "s1") { Id = 1 };
        _viewModel.Tasks.Add(task);

        // Act
        _viewModel.ReorderTaskCommand.Execute((999, 0));

        // Assert — unchanged
        _viewModel.Tasks.Should().HaveCount(1);
        _viewModel.Tasks[0].Id.Should().Be(1);
    }

    [Fact]
    public void ReorderTask_SamePosition_ShouldDoNothing()
    {
        // Arrange
        var task1 = new TaskItem("A", "s1") { Id = 1 };
        var task2 = new TaskItem("B", "s1") { Id = 2 };
        _viewModel.Tasks.Add(task1);
        _viewModel.Tasks.Add(task2);

        // Act — move task 1 to its current position
        _viewModel.ReorderTaskCommand.Execute((1, 0));

        // Assert — unchanged
        _viewModel.Tasks[0].Id.Should().Be(1);
        _viewModel.Tasks[1].Id.Should().Be(2);
    }

    [Fact]
    public void ReorderTask_IndexOutOfRange_ShouldClamp()
    {
        // Arrange
        var task1 = new TaskItem("A", "s1") { Id = 1 };
        var task2 = new TaskItem("B", "s1") { Id = 2 };
        _viewModel.Tasks.Add(task1);
        _viewModel.Tasks.Add(task2);

        // Act — move task 1 to position 100 (should clamp to 1)
        _viewModel.ReorderTaskCommand.Execute((1, 100));

        // Assert
        _viewModel.Tasks[0].Id.Should().Be(2);
        _viewModel.Tasks[1].Id.Should().Be(1);
    }

    // ═════════════════════════════════════════════════════════════
    // Cross-feature integration tests
    // ═════════════════════════════════════════════════════════════

    [Fact]
    public void ToggleTimer_NewSession_ShouldResetEnterpriseState()
    {
        // Arrange — set feature state from a previous session
        _viewModel.DistractionCount = 5;
        _viewModel.SessionTag = "Old Tag";
        _viewModel.SessionRating = 3;
        _viewModel.RetroNote = "Old note";
        _viewModel.ShowRetroPrompt = true;
        _viewModel.Mode = "pomodoro";

        _mockSessionRepository.Setup(x => x.CreateSession(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>()))
            .ReturnsAsync(new Session());
        _mockSessionRepository.Setup(x => x.GetSessionsWithStats())
            .ReturnsAsync(new List<Session>());

        // Act
        _viewModel.ToggleTimerCommand.Execute(null);

        // Assert — all enterprise state should be reset for new session
        _viewModel.DistractionCount.Should().Be(0);
        _viewModel.SessionTag.Should().BeEmpty();
        _viewModel.SessionRating.Should().Be(0);
        _viewModel.RetroNote.Should().BeEmpty();
        _viewModel.ShowRetroPrompt.Should().BeFalse();
    }

    [Fact]
    public void StartNewSession_ShouldResetAllEnterpriseState()
    {
        // Arrange
        _viewModel.SessionId = "session-1";
        _viewModel.DistractionCount = 3;
        _viewModel.SessionTag = "Tag";
        _viewModel.SessionRating = 5;
        _viewModel.RetroNote = "Note";
        _viewModel.ShowRetroPrompt = true;
        _viewModel.ShowStreakWarning = true;

        // Act
        _viewModel.StartNewSessionCommand.Execute(null);

        // Assert
        _viewModel.DistractionCount.Should().Be(0);
        _viewModel.SessionTag.Should().BeEmpty();
        _viewModel.SessionRating.Should().Be(0);
        _viewModel.RetroNote.Should().BeEmpty();
        _viewModel.ShowRetroPrompt.Should().BeFalse();
        _viewModel.ShowStreakWarning.Should().BeFalse();
    }

    [Fact]
    public void PomodoroTemplate_Class_ShouldHaveAllRequiredProperties()
    {
        var template = new PomodoroTemplate
        {
            Name = "Custom",
            PomodoroDuration = 30,
            ShortBreakDuration = 7,
            LongBreakDuration = 20,
            PomodorosBeforeLongBreak = 5
        };

        template.Name.Should().Be("Custom");
        template.PomodoroDuration.Should().Be(30);
        template.ShortBreakDuration.Should().Be(7);
        template.LongBreakDuration.Should().Be(20);
        template.PomodorosBeforeLongBreak.Should().Be(5);
    }

    [Fact]
    public void TaskItem_Priority_ShouldDefaultToNone()
    {
        var task = new TaskItem("Test", "s1");
        task.Priority.Should().Be(TaskPriority.None);
    }

    [Fact]
    public void ActiveTemplateName_ShouldDefaultToEmpty()
    {
        _viewModel.ActiveTemplateName.Should().BeEmpty();
    }
}
