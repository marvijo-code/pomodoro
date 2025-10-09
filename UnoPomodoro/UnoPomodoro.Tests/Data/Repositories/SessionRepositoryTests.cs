using Xunit;
using Moq;
using FluentAssertions;
using UnoPomodoro.Data.Models;
using UnoPomodoro.Data.Repositories;
using SQLite;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace UnoPomodoro.Tests.Data.Repositories;

public class SessionRepositoryTests
{
    private readonly Mock<SQLiteConnection> _mockConnection;
    private readonly SessionRepository _sessionRepository;

    public SessionRepositoryTests()
    {
        _mockConnection = new Mock<SQLiteConnection>();
        _sessionRepository = new SessionRepository(_mockConnection.Object);
    }

    [Fact]
    public async Task CreateSession_ShouldInsertSessionAndReturnIt()
    {
        // Arrange
        var sessionId = "test-session-123";
        var mode = "pomodoro";
        var startTime = DateTime.Now;
        var expectedSession = new Session(sessionId, mode, startTime);

        _mockConnection.Setup(x => x.Insert(It.IsAny<Session>()))
            .Verifiable();

        // Act
        var result = await _sessionRepository.CreateSession(sessionId, mode, startTime);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(sessionId);
        result.Mode.Should().Be(mode);
        result.StartTime.Should().Be(startTime);
        result.EndTime.Should().BeNull();
        _mockConnection.Verify(x => x.Insert(It.Is<Session>(s =>
            s.Id == sessionId && s.Mode == mode && s.StartTime == startTime)), Times.Once);
    }

    [Fact]
    public async Task CloseSession_ShouldUpdateSessionEndTime()
    {
        // Arrange
        var sessionId = "test-session-123";
        var endTime = DateTime.Now;
        var existingSession = new Session(sessionId, "pomodoro", DateTime.Now.AddHours(-1));

        _mockConnection.Setup(x => x.Table<Session>().FirstOrDefault(s => s.Id == sessionId))
            .Returns(existingSession);

        _mockConnection.Setup(x => x.Update(It.IsAny<Session>()))
            .Verifiable();

        // Act
        var result = await _sessionRepository.CloseSession(sessionId, endTime);

        // Assert
        result.Should().NotBeNull();
        result.EndTime.Should().Be(endTime);
        _mockConnection.Verify(x => x.Update(It.Is<Session>(s => s.EndTime == endTime)), Times.Once);
    }

    [Fact]
    public async Task CloseSession_WhenSessionNotFound_ShouldReturnNull()
    {
        // Arrange
        var sessionId = "non-existent-session";
        var endTime = DateTime.Now;

        _mockConnection.Setup(x => x.Table<Session>().FirstOrDefault(s => s.Id == sessionId))
            .Returns((Session?)null);

        // Act
        var result = await _sessionRepository.CloseSession(sessionId, endTime);

        // Assert
        result.Should().BeNull();
        _mockConnection.Verify(x => x.Update(It.IsAny<Session>()), Times.Never);
    }

    [Fact]
    public async Task GetSessionById_ShouldReturnSession()
    {
        // Arrange
        var sessionId = "test-session-123";
        var expectedSession = new Session(sessionId, "pomodoro", DateTime.Now);

        _mockConnection.Setup(x => x.Table<Session>().FirstOrDefault(s => s.Id == sessionId))
            .Returns(expectedSession);

        // Act
        var result = await _sessionRepository.GetSessionById(sessionId);

        // Assert
        result.Should().NotBeNull();
        result.Should().Be(expectedSession);
    }

    [Fact]
    public async Task GetSessionById_WhenSessionNotFound_ShouldReturnNull()
    {
        // Arrange
        var sessionId = "non-existent-session";

        _mockConnection.Setup(x => x.Table<Session>().FirstOrDefault(s => s.Id == sessionId))
            .Returns((Session?)null);

        // Act
        var result = await _sessionRepository.GetSessionById(sessionId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetSessionsWithStats_ShouldReturnSessionsWithTaskStatistics()
    {
        // Arrange
        var sessions = new List<Session>
        {
            new Session("session1", "pomodoro", DateTime.Now.AddHours(-2)),
            new Session("session2", "shortBreak", DateTime.Now.AddHours(-1))
        };

        var tasks = new List<TaskItem>
        {
            new TaskItem("Task 1", "session1") { Completed = true },
            new TaskItem("Task 2", "session1") { Completed = false },
            new TaskItem("Task 3", "session2") { Completed = true }
        };

        _mockConnection.Setup(x => x.Table<Session>().OrderByDescending(s => s.StartTime))
            .Returns(sessions.AsQueryable());

        _mockConnection.Setup(x => x.Table<TaskItem>().Where(t => t.SessionId == "session1"))
            .Returns(tasks.Where(t => t.SessionId == "session1").AsQueryable());

        _mockConnection.Setup(x => x.Table<TaskItem>().Where(t => t.SessionId == "session2"))
            .Returns(tasks.Where(t => t.SessionId == "session2").AsQueryable());

        // Act
        var result = await _sessionRepository.GetSessionsWithStats();

        // Assert
        result.Should().HaveCount(2);
        result.First().Id.Should().Be("session2"); // Ordered by start time descending
        result.First().TotalTasks.Should().Be(1);
        result.First().CompletedTasks.Should().Be(1);
        result.Last().Id.Should().Be("session1");
        result.Last().TotalTasks.Should().Be(2);
        result.Last().CompletedTasks.Should().Be(1);
    }

    [Fact]
    public async Task GetAllSessionsAsync_ShouldReturnAllSessionsOrderedByStartTime()
    {
        // Arrange
        var sessions = new List<Session>
        {
            new Session("session1", "pomodoro", DateTime.Now.AddHours(-2)),
            new Session("session2", "shortBreak", DateTime.Now.AddHours(-1)),
            new Session("session3", "pomodoro", DateTime.Now.AddHours(-3))
        };

        _mockConnection.Setup(x => x.Table<Session>().OrderByDescending(s => s.StartTime))
            .Returns(sessions.AsQueryable());

        // Act
        var result = await _sessionRepository.GetAllSessionsAsync();

        // Assert
        result.Should().HaveCount(3);
        result[0].Id.Should().Be("session2"); // Most recent
        result[1].Id.Should().Be("session1");
        result[2].Id.Should().Be("session3"); // Oldest
    }

    [Fact]
    public async Task GetRecentSessionsAsync_ShouldReturnLimitedSessionsWithStats()
    {
        // Arrange
        var sessions = new List<Session>
        {
            new Session("session1", "pomodoro", DateTime.Now.AddHours(-1)),
            new Session("session2", "shortBreak", DateTime.Now.AddHours(-2)),
            new Session("session3", "pomodoro", DateTime.Now.AddHours(-3)),
            new Session("session4", "longBreak", DateTime.Now.AddHours(-4))
        };

        var tasks = new List<TaskItem>
        {
            new TaskItem("Task 1", "session1") { Completed = true },
            new TaskItem("Task 2", "session2") { Completed = false }
        };

        _mockConnection.Setup(x => x.Table<Session>().OrderByDescending(s => s.StartTime).Take(2))
            .Returns(sessions.Take(2).AsQueryable());

        _mockConnection.Setup(x => x.Table<TaskItem>().Where(t => t.SessionId == "session1"))
            .Returns(tasks.Where(t => t.SessionId == "session1").AsQueryable());

        _mockConnection.Setup(x => x.Table<TaskItem>().Where(t => t.SessionId == "session2"))
            .Returns(tasks.Where(t => t.SessionId == "session2").AsQueryable());

        // Act
        var result = await _sessionRepository.GetRecentSessionsAsync(2);

        // Assert
        result.Should().HaveCount(2);
        result[0].Id.Should().Be("session1"); // Most recent
        result[1].Id.Should().Be("session2");
        result[0].TotalTasks.Should().Be(1);
        result[0].CompletedTasks.Should().Be(1);
    }

    [Fact]
    public async Task EndSession_ShouldUpdateSessionEndTimeAndReturnTrue()
    {
        // Arrange
        var sessionId = "test-session-123";
        var endTime = DateTime.Now;
        var existingSession = new Session(sessionId, "pomodoro", DateTime.Now.AddHours(-1));

        _mockConnection.Setup(x => x.Table<Session>().FirstOrDefault(s => s.Id == sessionId))
            .Returns(existingSession);

        _mockConnection.Setup(x => x.Update(It.IsAny<Session>()))
            .Verifiable();

        // Act
        var result = await _sessionRepository.EndSession(sessionId, endTime);

        // Assert
        result.Should().BeTrue();
        _mockConnection.Verify(x => x.Update(It.Is<Session>(s => s.EndTime == endTime)), Times.Once);
    }

    [Fact]
    public async Task EndSession_WhenSessionNotFound_ShouldReturnFalse()
    {
        // Arrange
        var sessionId = "non-existent-session";
        var endTime = DateTime.Now;

        _mockConnection.Setup(x => x.Table<Session>().FirstOrDefault(s => s.Id == sessionId))
            .Returns((Session?)null);

        // Act
        var result = await _sessionRepository.EndSession(sessionId, endTime);

        // Assert
        result.Should().BeFalse();
        _mockConnection.Verify(x => x.Update(It.IsAny<Session>()), Times.Never);
    }

    [Fact]
    public async Task DeleteSessionAsync_ShouldDeleteSessionAndReturnCount()
    {
        // Arrange
        var sessionId = "test-session-123";
        var expectedCount = 1;

        _mockConnection.Setup(x => x.Table<Session>().Delete(s => s.Id == sessionId))
            .Returns(expectedCount);

        // Act
        var result = await _sessionRepository.DeleteSessionAsync(sessionId);

        // Assert
        result.Should().Be(expectedCount);
        _mockConnection.Verify(x => x.Table<Session>().Delete(s => s.Id == sessionId), Times.Once);
    }

    [Fact]
    public async Task DeleteSessionAsync_WhenSessionNotFound_ShouldReturnZero()
    {
        // Arrange
        var sessionId = "non-existent-session";
        var expectedCount = 0;

        _mockConnection.Setup(x => x.Table<Session>().Delete(s => s.Id == sessionId))
            .Returns(expectedCount);

        // Act
        var result = await _sessionRepository.DeleteSessionAsync(sessionId);

        // Assert
        result.Should().Be(expectedCount);
        _mockConnection.Verify(x => x.Table<Session>().Delete(s => s.Id == sessionId), Times.Once);
    }
}