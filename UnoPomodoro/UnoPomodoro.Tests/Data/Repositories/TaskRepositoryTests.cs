using Xunit;
using Moq;
using FluentAssertions;
using UnoPomodoro.Data.Models;
using UnoPomodoro.Data.Repositories;
using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace UnoPomodoro.Tests.Data.Repositories;

public class TaskRepositoryTests
{
    private readonly Mock<SQLiteConnection> _mockConnection;
    private readonly TaskRepository _taskRepository;

    public TaskRepositoryTests()
    {
        _mockConnection = new Mock<SQLiteConnection>();
        _taskRepository = new TaskRepository(_mockConnection.Object);
    }

    [Fact]
    public async Task GetBySession_ShouldReturnTasksForSessionOrderedById()
    {
        // Arrange
        var sessionId = "test-session-123";
        var tasks = new List<TaskItem>
        {
            new TaskItem("Task 3", sessionId) { Id = 3 },
            new TaskItem("Task 1", sessionId) { Id = 1 },
            new TaskItem("Task 2", sessionId) { Id = 2 }
        };

        _mockConnection.Setup(x => x.Table<TaskItem>().Where(t => t.SessionId == sessionId).OrderBy(t => t.Id))
            .Returns(tasks.OrderBy(t => t.Id).AsQueryable());

        // Act
        var result = await _taskRepository.GetBySession(sessionId);

        // Assert
        result.Should().HaveCount(3);
        result[0].Id.Should().Be(1);
        result[1].Id.Should().Be(2);
        result[2].Id.Should().Be(3);
        result.All(t => t.SessionId == sessionId).Should().BeTrue();
    }

    [Fact]
    public async Task Add_ShouldInsertTaskAndReturnIt()
    {
        // Arrange
        var text = "Test task";
        var sessionId = "test-session-123";
        var expectedTask = new TaskItem(text, sessionId) { Id = 1, Completed = false, CompletedAt = null };

        _mockConnection.Setup(x => x.Insert(It.IsAny<TaskItem>()))
            .Verifiable();

        // Act
        var result = await _taskRepository.Add(text, sessionId);

        // Assert
        result.Should().NotBeNull();
        result.Text.Should().Be(text);
        result.SessionId.Should().Be(sessionId);
        result.Completed.Should().BeFalse();
        result.CompletedAt.Should().BeNull();
        _mockConnection.Verify(x => x.Insert(It.Is<TaskItem>(t =>
            t.Text == text && t.SessionId == sessionId && t.Completed == false)), Times.Once);
    }

    [Fact]
    public async Task ToggleCompleted_ShouldUpdateTaskCompletionAndReturnTask()
    {
        // Arrange
        var taskId = 1;
        var existingTask = new TaskItem("Test task", "test-session") { Id = taskId, Completed = false, CompletedAt = null };
        var completedTime = DateTime.Now;

        _mockConnection.Setup(x => x.Table<TaskItem>().FirstOrDefault(t => t.Id == taskId))
            .Returns(existingTask);

        _mockConnection.Setup(x => x.Update(It.IsAny<TaskItem>()))
            .Verifiable();

        // Act
        var result = await _taskRepository.ToggleCompleted(taskId, true);

        // Assert
        result.Should().NotBeNull();
        result.Completed.Should().BeTrue();
        result.CompletedAt.Should().BeCloseTo(completedTime, TimeSpan.FromSeconds(1));
        _mockConnection.Verify(x => x.Update(It.Is<TaskItem>(t =>
            t.Id == taskId && t.Completed == true && t.CompletedAt != null)), Times.Once);
    }

    [Fact]
    public async Task ToggleCompleted_WhenTaskNotFound_ShouldReturnNull()
    {
        // Arrange
        var taskId = 999;

        _mockConnection.Setup(x => x.Table<TaskItem>().FirstOrDefault(t => t.Id == taskId))
            .Returns((TaskItem?)null);

        // Act
        var result = await _taskRepository.ToggleCompleted(taskId, true);

        // Assert
        result.Should().BeNull();
        _mockConnection.Verify(x => x.Update(It.IsAny<TaskItem>()), Times.Never);
    }

    [Fact]
    public async Task ToggleCompleted_ToFalse_ShouldClearCompletedAt()
    {
        // Arrange
        var taskId = 1;
        var existingTask = new TaskItem("Test task", "test-session") { Id = taskId, Completed = true, CompletedAt = DateTime.Now };

        _mockConnection.Setup(x => x.Table<TaskItem>().FirstOrDefault(t => t.Id == taskId))
            .Returns(existingTask);

        _mockConnection.Setup(x => x.Update(It.IsAny<TaskItem>()))
            .Verifiable();

        // Act
        var result = await _taskRepository.ToggleCompleted(taskId, false);

        // Assert
        result.Should().NotBeNull();
        result.Completed.Should().BeFalse();
        result.CompletedAt.Should().BeNull();
        _mockConnection.Verify(x => x.Update(It.Is<TaskItem>(t =>
            t.Id == taskId && t.Completed == false && t.CompletedAt == null)), Times.Once);
    }

    [Fact]
    public async Task Delete_ShouldDeleteTask()
    {
        // Arrange
        var taskId = 1;

        _mockConnection.Setup(x => x.Delete<TaskItem>(taskId))
            .Verifiable();

        // Act
        await _taskRepository.Delete(taskId);

        // Assert
        _mockConnection.Verify(x => x.Delete<TaskItem>(taskId), Times.Once);
    }

    [Fact]
    public async Task GetAllTasksAsync_ShouldReturnAllTasksOrderedByIdDescending()
    {
        // Arrange
        var tasks = new List<TaskItem>
        {
            new TaskItem("Task 1", "session1") { Id = 1 },
            new TaskItem("Task 2", "session2") { Id = 2 },
            new TaskItem("Task 3", "session1") { Id = 3 }
        };

        _mockConnection.Setup(x => x.Table<TaskItem>().OrderByDescending(t => t.Id))
            .Returns(tasks.AsQueryable());

        // Act
        var result = await _taskRepository.GetAllTasksAsync();

        // Assert
        result.Should().HaveCount(3);
        result[0].Id.Should().Be(3); // Most recent
        result[1].Id.Should().Be(2);
        result[2].Id.Should().Be(1); // Oldest
    }

    [Fact]
    public async Task GetTasksByDateRangeAsync_ShouldReturnTasksWithinDateRange()
    {
        // Arrange
        var startDate = DateTime.Now.AddDays(-7);
        var endDate = DateTime.Now;
        var sessions = new List<Session>
        {
            new Session("session1", "pomodoro", DateTime.Now.AddDays(-1)),
            new Session("session2", "pomodoro", DateTime.Now.AddDays(-8)), // Outside range
            new Session("session3", "pomodoro", DateTime.Now.AddDays(-3))
        };

        var tasks = new List<TaskItem>
        {
            new TaskItem("Task 1", "session1") { Id = 1 },
            new TaskItem("Task 2", "session2") { Id = 2 },
            new TaskItem("Task 3", "session3") { Id = 3 }
        };

        _mockConnection.Setup(x => x.Table<Session>().Where(s => s.StartTime >= startDate && s.StartTime <= endDate))
            .Returns(sessions.Where(s => s.StartTime >= startDate && s.StartTime <= endDate).AsQueryable());

        _mockConnection.Setup(x => x.Table<TaskItem>().Where(t => It.IsIn<string>(new[] { "session1", "session3" })))
            .Returns(tasks.Where(t => t.SessionId == "session1" || t.SessionId == "session3").AsQueryable());

        // Act
        var result = await _taskRepository.GetTasksByDateRangeAsync(startDate, endDate);

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(t => t.SessionId == "session1");
        result.Should().Contain(t => t.SessionId == "session3");
        result.Should().NotContain(t => t.SessionId == "session2");
    }

    [Fact]
    public async Task GetCompletedTasksCountAsync_ShouldReturnCountOfCompletedTasks()
    {
        // Arrange
        var sessionId = "test-session-123";
        var expectedCount = 3;

        _mockConnection.Setup(x => x.Table<TaskItem>().Count(t => t.SessionId == sessionId && t.Completed))
            .Returns(expectedCount);

        // Act
        var result = await _taskRepository.GetCompletedTasksCountAsync(sessionId);

        // Assert
        result.Should().Be(expectedCount);
    }

    [Fact]
    public async Task GetTotalTasksCountAsync_ShouldReturnCountOfAllTasks()
    {
        // Arrange
        var sessionId = "test-session-123";
        var expectedCount = 5;

        _mockConnection.Setup(x => x.Table<TaskItem>().Count(t => t.SessionId == sessionId))
            .Returns(expectedCount);

        // Act
        var result = await _taskRepository.GetTotalTasksCountAsync(sessionId);

        // Assert
        result.Should().Be(expectedCount);
    }

    [Fact]
    public async Task GetTaskByIdAsync_ShouldReturnTask()
    {
        // Arrange
        var taskId = 1;
        var expectedTask = new TaskItem("Test task", "session1") { Id = taskId };

        _mockConnection.Setup(x => x.Table<TaskItem>().FirstOrDefault(t => t.Id == taskId))
            .Returns(expectedTask);

        // Act
        var result = await _taskRepository.GetTaskByIdAsync(taskId);

        // Assert
        result.Should().NotBeNull();
        result.Should().Be(expectedTask);
    }

    [Fact]
    public async Task GetTaskByIdAsync_WhenTaskNotFound_ShouldReturnNull()
    {
        // Arrange
        var taskId = 999;

        _mockConnection.Setup(x => x.Table<TaskItem>().FirstOrDefault(t => t.Id == taskId))
            .Returns((TaskItem?)null);

        // Act
        var result = await _taskRepository.GetTaskByIdAsync(taskId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task UpdateTaskAsync_ShouldUpdateTaskAndReturnTrue()
    {
        // Arrange
        var task = new TaskItem("Updated task", "session1") { Id = 1, Completed = true };

        _mockConnection.Setup(x => x.Update(task))
            .Verifiable();

        // Act
        var result = await _taskRepository.UpdateTaskAsync(task);

        // Assert
        result.Should().BeTrue();
        _mockConnection.Verify(x => x.Update(task), Times.Once);
    }

    [Fact]
    public async Task UpdateTaskAsync_WhenUpdateFails_ShouldReturnFalse()
    {
        // Arrange
        var task = new TaskItem("Updated task", "session1") { Id = 1, Completed = true };

        _mockConnection.Setup(x => x.Update(task))
            .Throws(new Exception("Database error"));

        // Act
        var result = await _taskRepository.UpdateTaskAsync(task);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task GetBySession_WhenNoTasks_ShouldReturnEmptyList()
    {
        // Arrange
        var sessionId = "empty-session";

        _mockConnection.Setup(x => x.Table<TaskItem>().Where(t => t.SessionId == sessionId).OrderBy(t => t.Id))
            .Returns(new List<TaskItem>().AsQueryable());

        // Act
        var result = await _taskRepository.GetBySession(sessionId);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetTasksByDateRangeAsync_WhenNoSessionsInDateRange_ShouldReturnEmptyList()
    {
        // Arrange
        var startDate = DateTime.Now.AddDays(-7);
        var endDate = DateTime.Now;

        _mockConnection.Setup(x => x.Table<Session>().Where(s => s.StartTime >= startDate && s.StartTime <= endDate))
            .Returns(new List<Session>().AsQueryable());

        _mockConnection.Setup(x => x.Table<TaskItem>().Where(t => It.IsIn<string>(Array.Empty<string>())))
            .Returns(new List<TaskItem>().AsQueryable());

        // Act
        var result = await _taskRepository.GetTasksByDateRangeAsync(startDate, endDate);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetCompletedTasksCountAsync_WhenNoTasks_ShouldReturnZero()
    {
        // Arrange
        var sessionId = "empty-session";
        var expectedCount = 0;

        _mockConnection.Setup(x => x.Table<TaskItem>().Count(t => t.SessionId == sessionId && t.Completed))
            .Returns(expectedCount);

        // Act
        var result = await _taskRepository.GetCompletedTasksCountAsync(sessionId);

        // Assert
        result.Should().Be(expectedCount);
    }

    [Fact]
    public async Task GetTotalTasksCountAsync_WhenNoTasks_ShouldReturnZero()
    {
        // Arrange
        var sessionId = "empty-session";
        var expectedCount = 0;

        _mockConnection.Setup(x => x.Table<TaskItem>().Count(t => t.SessionId == sessionId))
            .Returns(expectedCount);

        // Act
        var result = await _taskRepository.GetTotalTasksCountAsync(sessionId);

        // Assert
        result.Should().Be(expectedCount);
    }
}