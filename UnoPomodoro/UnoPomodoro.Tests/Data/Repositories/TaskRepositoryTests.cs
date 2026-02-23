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

public class TaskRepositoryTests : IDisposable
{
    private readonly SQLiteConnection _connection;
    private readonly TaskRepository _taskRepository;

    public TaskRepositoryTests()
    {
        _connection = new SQLiteConnection(":memory:");
        _connection.CreateTable<TaskItem>();
        _connection.CreateTable<Session>(); // Needed for joins if any
        _taskRepository = new TaskRepository(_connection);
    }

    public void Dispose()
    {
        _connection.Close();
        _connection.Dispose();
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
        _connection.InsertAll(tasks);

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

        // Act
        var result = await _taskRepository.Add(text, sessionId);

        // Assert
        result.Should().NotBeNull();
        result!.Text.Should().Be(text);
        result.SessionId.Should().Be(sessionId);
        result.Completed.Should().BeFalse();
        result.CompletedAt.Should().BeNull();
        
        var dbTask = _connection.Table<TaskItem>().FirstOrDefault(t => t.Id == result.Id);
        dbTask.Should().NotBeNull();
        dbTask.Text.Should().Be(text);
    }

    [Fact]
    public async Task ToggleCompleted_ShouldUpdateTaskCompletionAndReturnTask()
    {
        // Arrange
        var existingTask = new TaskItem("Test task", "test-session") { Completed = false, CompletedAt = null };
        _connection.Insert(existingTask);
        var taskId = existingTask.Id;
        var completedTime = DateTime.Now;

        // Act
        var result = await _taskRepository.ToggleCompleted(taskId, true);

        // Assert
        result.Should().NotBeNull();
        result!.Completed.Should().BeTrue();
        result.CompletedAt.Should().BeCloseTo(completedTime, TimeSpan.FromSeconds(5));
        
        var dbTask = _connection.Table<TaskItem>().FirstOrDefault(t => t.Id == taskId);
        dbTask.Completed.Should().BeTrue();
        dbTask.CompletedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task ToggleCompleted_WhenTaskNotFound_ShouldReturnNull()
    {
        // Arrange
        var taskId = 999;

        // Act
        var result = await _taskRepository.ToggleCompleted(taskId, true);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task ToggleCompleted_ToFalse_ShouldClearCompletedAt()
    {
        // Arrange
        var existingTask = new TaskItem("Test task", "test-session") { Completed = true, CompletedAt = DateTime.Now };
        _connection.Insert(existingTask);
        var taskId = existingTask.Id;

        // Act
        var result = await _taskRepository.ToggleCompleted(taskId, false);

        // Assert
        result.Should().NotBeNull();
        result!.Completed.Should().BeFalse();
        result.CompletedAt.Should().BeNull();
        
        var dbTask = _connection.Table<TaskItem>().FirstOrDefault(t => t.Id == taskId);
        dbTask.Completed.Should().BeFalse();
        dbTask.CompletedAt.Should().BeNull();
    }

    [Fact]
    public async Task Delete_ShouldDeleteTask()
    {
        // Arrange
        var existingTask = new TaskItem("Test task", "test-session");
        _connection.Insert(existingTask);
        var taskId = existingTask.Id;

        // Act
        await _taskRepository.Delete(taskId);

        // Assert
        var dbTask = _connection.Table<TaskItem>().FirstOrDefault(t => t.Id == taskId);
        dbTask.Should().BeNull();
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
        _connection.InsertAll(tasks);

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
        _connection.InsertAll(sessions);

        var tasks = new List<TaskItem>
        {
            new TaskItem("Task 1", "session1") { Id = 1 },
            new TaskItem("Task 2", "session2") { Id = 2 },
            new TaskItem("Task 3", "session3") { Id = 3 }
        };
        _connection.InsertAll(tasks);

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
        var tasks = new List<TaskItem>
        {
            new TaskItem("Task 1", sessionId) { Completed = true },
            new TaskItem("Task 2", sessionId) { Completed = true },
            new TaskItem("Task 3", sessionId) { Completed = true },
            new TaskItem("Task 4", sessionId) { Completed = false },
            new TaskItem("Task 5", "other-session") { Completed = true }
        };
        _connection.InsertAll(tasks);

        // Act
        var result = await _taskRepository.GetCompletedTasksCountAsync(sessionId);

        // Assert
        result.Should().Be(3);
    }

    [Fact]
    public async Task GetTotalTasksCountAsync_ShouldReturnCountOfAllTasks()
    {
        // Arrange
        var sessionId = "test-session-123";
        var tasks = new List<TaskItem>
        {
            new TaskItem("Task 1", sessionId),
            new TaskItem("Task 2", sessionId),
            new TaskItem("Task 3", sessionId),
            new TaskItem("Task 4", sessionId),
            new TaskItem("Task 5", sessionId),
            new TaskItem("Task 6", "other-session")
        };
        _connection.InsertAll(tasks);

        // Act
        var result = await _taskRepository.GetTotalTasksCountAsync(sessionId);

        // Assert
        result.Should().Be(5);
    }

    [Fact]
    public async Task GetTaskByIdAsync_ShouldReturnTask()
    {
        // Arrange
        var expectedTask = new TaskItem("Test task", "session1");
        _connection.Insert(expectedTask);
        var taskId = expectedTask.Id;

        // Act
        var result = await _taskRepository.GetTaskByIdAsync(taskId);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(taskId);
    }

    [Fact]
    public async Task GetTaskByIdAsync_WhenTaskNotFound_ShouldReturnNull()
    {
        // Arrange
        var taskId = 999;

        // Act
        var result = await _taskRepository.GetTaskByIdAsync(taskId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task UpdateTaskAsync_ShouldUpdateTaskAndReturnTrue()
    {
        // Arrange
        var task = new TaskItem("Original task", "session1") { Completed = false };
        _connection.Insert(task);
        
        task.Text = "Updated task";
        task.Completed = true;

        // Act
        var result = await _taskRepository.UpdateTaskAsync(task);

        // Assert
        result.Should().BeTrue();
        
        var dbTask = _connection.Table<TaskItem>().FirstOrDefault(t => t.Id == task.Id);
        dbTask.Text.Should().Be("Updated task");
        dbTask.Completed.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateTaskAsync_WhenUpdateFails_ShouldReturnFalse()
    {
        // Arrange
        var task = new TaskItem("Updated task", "session1") { Id = 999, Completed = true };

        // Act
        // In SQLite-net-pcl, Update returns 0 if not found, doesn't throw usually unless constraint violation
        // But the repository might check return value.
        // If the repository just calls Update, it returns int (rows affected).
        // Let's assume the repository returns true if rows > 0.
        
        var result = await _taskRepository.UpdateTaskAsync(task);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task GetBySession_WhenNoTasks_ShouldReturnEmptyList()
    {
        // Arrange
        var sessionId = "empty-session";

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

        // Act
        var result = await _taskRepository.GetCompletedTasksCountAsync(sessionId);

        // Assert
        result.Should().Be(0);
    }

    [Fact]
    public async Task GetTotalTasksCountAsync_WhenNoTasks_ShouldReturnZero()
    {
        // Arrange
        var sessionId = "empty-session";

        // Act
        var result = await _taskRepository.GetTotalTasksCountAsync(sessionId);

        // Assert
        result.Should().Be(0);
    }
}