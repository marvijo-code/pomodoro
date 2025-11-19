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

public class SessionRepositoryTests : IDisposable
{
    private readonly SQLiteConnection _connection;
    private readonly SessionRepository _sessionRepository;

    public SessionRepositoryTests()
    {
        _connection = new SQLiteConnection(":memory:");
        _connection.CreateTable<Session>();
        _connection.CreateTable<TaskItem>(); // Needed for stats
        _sessionRepository = new SessionRepository(_connection);
    }

    public void Dispose()
    {
        _connection.Close();
        _connection.Dispose();
    }

    [Fact]
    public async Task CreateSession_ShouldInsertSessionAndReturnIt()
    {
        // Arrange
        var sessionId = "test-session-123";
        var mode = "pomodoro";
        var startTime = DateTime.Now;

        // Act
        var result = await _sessionRepository.CreateSession(sessionId, mode, startTime);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(sessionId);
        result.Mode.Should().Be(mode);
        result.StartTime.Should().Be(startTime);
        result.EndTime.Should().BeNull();
        
        var dbSession = _connection.Table<Session>().FirstOrDefault(s => s.Id == sessionId);
        dbSession.Should().NotBeNull();
        dbSession.Mode.Should().Be(mode);
    }

    [Fact]
    public async Task CloseSession_ShouldUpdateSessionEndTime()
    {
        // Arrange
        var sessionId = "test-session-123";
        var existingSession = new Session(sessionId, "pomodoro", DateTime.Now.AddHours(-1));
        _connection.Insert(existingSession);
        var endTime = DateTime.Now;

        // Act
        var result = await _sessionRepository.CloseSession(sessionId, endTime);

        // Assert
        result.Should().NotBeNull();
        result.EndTime.Should().Be(endTime);
        
        var dbSession = _connection.Table<Session>().FirstOrDefault(s => s.Id == sessionId);
        dbSession.EndTime.Should().Be(endTime);
    }

    [Fact]
    public async Task CloseSession_WhenSessionNotFound_ShouldReturnNull()
    {
        // Arrange
        var sessionId = "non-existent-session";
        var endTime = DateTime.Now;

        // Act
        var result = await _sessionRepository.CloseSession(sessionId, endTime);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetSessionById_ShouldReturnSession()
    {
        // Arrange
        var sessionId = "test-session-123";
        var expectedSession = new Session(sessionId, "pomodoro", DateTime.Now);
        _connection.Insert(expectedSession);

        // Act
        var result = await _sessionRepository.GetSessionById(sessionId);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(sessionId);
    }

    [Fact]
    public async Task GetSessionById_WhenSessionNotFound_ShouldReturnNull()
    {
        // Arrange
        var sessionId = "non-existent-session";

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
        _connection.InsertAll(sessions);

        var tasks = new List<TaskItem>
        {
            new TaskItem("Task 1", "session1") { Completed = true },
            new TaskItem("Task 2", "session1") { Completed = false },
            new TaskItem("Task 3", "session2") { Completed = true }
        };
        _connection.InsertAll(tasks);

        // Act
        var result = await _sessionRepository.GetSessionsWithStats();

        // Assert
        result.Should().HaveCount(2);
        // Note: Order might depend on implementation, but usually by StartTime descending
        var session2 = result.First(s => s.Id == "session2");
        session2.TotalTasks.Should().Be(1);
        session2.CompletedTasks.Should().Be(1);
        
        var session1 = result.First(s => s.Id == "session1");
        session1.TotalTasks.Should().Be(2);
        session1.CompletedTasks.Should().Be(1);
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
        _connection.InsertAll(sessions);

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
        _connection.InsertAll(sessions);

        var tasks = new List<TaskItem>
        {
            new TaskItem("Task 1", "session1") { Completed = true },
            new TaskItem("Task 2", "session2") { Completed = false }
        };
        _connection.InsertAll(tasks);

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
        var existingSession = new Session(sessionId, "pomodoro", DateTime.Now.AddHours(-1));
        _connection.Insert(existingSession);
        var endTime = DateTime.Now;

        // Act
        var result = await _sessionRepository.EndSession(sessionId, endTime);

        // Assert
        result.Should().BeTrue();
        
        var dbSession = _connection.Table<Session>().FirstOrDefault(s => s.Id == sessionId);
        dbSession.EndTime.Should().Be(endTime);
    }

    [Fact]
    public async Task EndSession_WhenSessionNotFound_ShouldReturnFalse()
    {
        // Arrange
        var sessionId = "non-existent-session";
        var endTime = DateTime.Now;

        // Act
        var result = await _sessionRepository.EndSession(sessionId, endTime);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteSessionAsync_ShouldDeleteSessionAndReturnCount()
    {
        // Arrange
        var sessionId = "test-session-123";
        var existingSession = new Session(sessionId, "pomodoro", DateTime.Now);
        _connection.Insert(existingSession);

        // Act
        var result = await _sessionRepository.DeleteSessionAsync(sessionId);

        // Assert
        result.Should().Be(1);
        
        var dbSession = _connection.Table<Session>().FirstOrDefault(s => s.Id == sessionId);
        dbSession.Should().BeNull();
    }

    [Fact]
    public async Task DeleteSessionAsync_WhenSessionNotFound_ShouldReturnZero()
    {
        // Arrange
        var sessionId = "non-existent-session";

        // Act
        var result = await _sessionRepository.DeleteSessionAsync(sessionId);

        // Assert
        result.Should().Be(0);
    }
}