# Uno Platform Implementation Examples

## Timer Service Implementation

### ITimerService Interface
```csharp
// Services/Interfaces/ITimerService.cs
using System;
using System.Reactive.Subjects;

namespace PomodoroUno.Services.Interfaces
{
    public interface ITimerService
    {
        IObservable<TimeSpan> Timer { get; }
        TimerState State { get; }
        event Action OnTimerCompleted;
        
        void Start(TimeSpan duration);
        void Pause();
        void Resume();
        void Stop();
    }
    
    public enum TimerState
    {
        Stopped,
        Running,
        Paused,
        Completed
    }
}
```

### TimerService Implementation
```csharp
// Services/TimerService.cs
using System;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using PomodoroUno.Services.Interfaces;

namespace PomodoroUno.Services
{
    public class TimerService : ITimerService, IDisposable
    {
        private readonly Subject<TimeSpan> _timerSubject = new();
        private IDisposable _timerSubscription;
        private TimeSpan _duration;
        private TimeSpan _elapsed;
        private TimerState _state = TimerState.Stopped;
        
        public IObservable<TimeSpan> Timer => _timerSubject.AsObservable();
        public TimerState State => _state;
        public event Action OnTimerCompleted;
        
        public void Start(TimeSpan duration)
        {
            Stop(); // Ensure clean start
            
            _duration = duration;
            _elapsed = TimeSpan.Zero;
            _state = TimerState.Running;
            
            _timerSubscription = Observable
                .Interval(TimeSpan.FromSeconds(1))
                .Subscribe(_ =>
                {
                    _elapsed = _elapsed.Add(TimeSpan.FromSeconds(1));
                    var remaining = _duration - _elapsed;
                    
                    if (remaining <= TimeSpan.Zero)
                    {
                        _timerSubject.OnNext(TimeSpan.Zero);
                        Complete();
                    }
                    else
                    {
                        _timerSubject.OnNext(remaining);
                    }
                });
            
            _timerSubject.OnNext(_duration);
        }
        
        public void Pause()
        {
            if (_state == TimerState.Running)
            {
                _timerSubscription?.Dispose();
                _state = TimerState.Paused;
            }
        }
        
        public void Resume()
        {
            if (_state == TimerState.Paused)
            {
                _state = TimerState.Running;
                var remaining = _duration - _elapsed;
                
                _timerSubscription = Observable
                    .Interval(TimeSpan.FromSeconds(1))
                    .Subscribe(_ =>
                    {
                        _elapsed = _elapsed.Add(TimeSpan.FromSeconds(1));
                        var timeLeft = _duration - _elapsed;
                        
                        if (timeLeft <= TimeSpan.Zero)
                        {
                            _timerSubject.OnNext(TimeSpan.Zero);
                            Complete();
                        }
                        else
                        {
                            _timerSubject.OnNext(timeLeft);
                        }
                    });
            }
        }
        
        public void Stop()
        {
            _timerSubscription?.Dispose();
            _state = TimerState.Stopped;
            _elapsed = TimeSpan.Zero;
            _timerSubject.OnNext(_duration);
        }
        
        private void Complete()
        {
            _state = TimerState.Completed;
            _timerSubscription?.Dispose();
            OnTimerCompleted?.Invoke();
        }
        
        public void Dispose()
        {
            _timerSubscription?.Dispose();
            _timerSubject?.Dispose();
        }
    }
}
```

## ViewModels

### TimerViewModel
```csharp
// ViewModels/TimerViewModel.cs
using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PomodoroUno.Models;
using PomodoroUno.Services.Interfaces;

namespace PomodoroUno.ViewModels
{
    public partial class TimerViewModel : ObservableObject, IDisposable
    {
        private readonly ITimerService _timerService;
        private readonly IAudioService _audioService;
        private readonly IDataService _dataService;
        private readonly IDisposable _timerSubscription;
        private string _currentSessionId;
        
        [ObservableProperty]
        private TimeSpan _timeRemaining;
        
        [ObservableProperty]
        private TimeSpan _totalTime;
        
        [ObservableProperty]
        private TimerMode _currentMode = TimerMode.Pomodoro;
        
        [ObservableProperty]
        private bool _isRunning;
        
        [ObservableProperty]
        private bool _isPaused;
        
        public TimerViewModel(
            ITimerService timerService,
            IAudioService audioService,
            IDataService dataService)
        {
            _timerService = timerService;
            _audioService = audioService;
            _dataService = dataService;
            
            _timerSubscription = _timerService.Timer.Subscribe(time => TimeRemaining = time);
            _timerService.OnTimerCompleted += HandleTimerCompleted;
            
            InitializeTimer();
        }
        
        private void InitializeTimer()
        {
            TotalTime = GetDurationForMode(CurrentMode);
            TimeRemaining = TotalTime;
        }
        
        [RelayCommand]
        private async Task StartTimer()
        {
            if (IsPaused)
            {
                _timerService.Resume();
                IsPaused = false;
            }
            else
            {
                var duration = GetDurationForMode(CurrentMode);
                _timerService.Start(duration);
                
                // Create new session
                _currentSessionId = Guid.NewGuid().ToString();
                await _dataService.CreateSessionAsync(new PomodoroSession
                {
                    Id = _currentSessionId,
                    StartTime = DateTime.Now,
                    Mode = CurrentMode
                });
            }
            
            IsRunning = true;
        }
        
        [RelayCommand]
        private void PauseTimer()
        {
            _timerService.Pause();
            IsRunning = false;
            IsPaused = true;
        }
        
        [RelayCommand]
        private async Task ResetTimer()
        {
            _timerService.Stop();
            IsRunning = false;
            IsPaused = false;
            InitializeTimer();
            
            // End current session if exists
            if (!string.IsNullOrEmpty(_currentSessionId))
            {
                await _dataService.EndSessionAsync(_currentSessionId);
                _currentSessionId = null;
            }
        }
        
        [RelayCommand]
        private void ChangeMode(string mode)
        {
            if (Enum.TryParse<TimerMode>(mode, out var timerMode))
            {
                CurrentMode = timerMode;
                ResetTimerCommand.Execute(null);
            }
        }
        
        private void HandleTimerCompleted()
        {
            IsRunning = false;
            IsPaused = false;
            _audioService.PlayNotification();
            
            // Show notification
            ShowCompletionNotification();
        }
        
        private void ShowCompletionNotification()
        {
            // Platform-specific notification implementation
        }
        
        private TimeSpan GetDurationForMode(TimerMode mode) => mode switch
        {
            TimerMode.Pomodoro => TimeSpan.FromMinutes(25),
            TimerMode.ShortBreak => TimeSpan.FromMinutes(5),
            TimerMode.LongBreak => TimeSpan.FromMinutes(15),
            _ => TimeSpan.FromMinutes(25)
        };
        
        public void Dispose()
        {
            _timerSubscription?.Dispose();
            if (_timerService != null)
            {
                _timerService.OnTimerCompleted -= HandleTimerCompleted;
            }
        }
    }
}
```

### TasksViewModel
```csharp
// ViewModels/TasksViewModel.cs
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PomodoroUno.Models;
using PomodoroUno.Services.Interfaces;

namespace PomodoroUno.ViewModels
{
    public partial class TasksViewModel : ObservableObject
    {
        private readonly IDataService _dataService;
        private string _currentSessionId;
        
        [ObservableProperty]
        private ObservableCollection<PomodoroTask> _tasks = new();
        
        [ObservableProperty]
        private string _newTaskText;
        
        [ObservableProperty]
        private bool _isLoading;
        
        public TasksViewModel(IDataService dataService)
        {
            _dataService = dataService;
        }
        
        public async Task InitializeAsync(string sessionId)
        {
            _currentSessionId = sessionId;
            await LoadTasksAsync();
        }
        
        private async Task LoadTasksAsync()
        {
            if (string.IsNullOrEmpty(_currentSessionId))
                return;
            
            IsLoading = true;
            
            try
            {
                var tasks = await _dataService.GetTasksAsync(_currentSessionId);
                Tasks.Clear();
                foreach (var task in tasks)
                {
                    Tasks.Add(task);
                }
            }
            finally
            {
                IsLoading = false;
            }
        }
        
        [RelayCommand]
        private async Task AddTask()
        {
            if (string.IsNullOrWhiteSpace(NewTaskText) || string.IsNullOrEmpty(_currentSessionId))
                return;
            
            var task = new PomodoroTask
            {
                Text = NewTaskText,
                SessionId = _currentSessionId,
                IsCompleted = false
            };
            
            var addedTask = await _dataService.AddTaskAsync(task);
            Tasks.Add(addedTask);
            NewTaskText = string.Empty;
        }
        
        [RelayCommand]
        private async Task ToggleTask(PomodoroTask task)
        {
            if (task == null)
                return;
            
            task.IsCompleted = !task.IsCompleted;
            task.CompletedAt = task.IsCompleted ? DateTime.Now : null;
            
            await _dataService.UpdateTaskAsync(task);
        }
        
        [RelayCommand]
        private async Task DeleteTask(PomodoroTask task)
        {
            if (task == null)
                return;
            
            await _dataService.DeleteTaskAsync(task.Id);
            Tasks.Remove(task);
        }
    }
}
```

## UI Implementation

### Timer Page XAML
```xml
<!-- Views/Pages/TimerPage.xaml -->
<Page x:Class="PomodoroUno.Views.Pages.TimerPage"
      xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
      xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
      xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
      xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
      xmlns:toolkit="using:Uno.Toolkit.UI"
      xmlns:controls="using:PomodoroUno.Views.Controls"
      mc:Ignorable="d">
    
    <Grid Background="{ThemeResource ApplicationPageBackgroundThemeBrush}">
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="*"/>
            <RowDefinition Height="Auto"/>
        </Grid.RowDefinitions>
        
        <!-- Header with Mode Selector -->
        <StackPanel Grid.Row="0" 
                    Margin="20,20,20,10">
            <TextBlock Text="Pomodoro Timer" 
                      Style="{StaticResource HeaderTextBlockStyle}"
                      HorizontalAlignment="Center"/>
            
            <toolkit:Chips x:Name="ModeChips"
                          Margin="0,20,0,0"
                          HorizontalAlignment="Center"
                          SelectedItem="{Binding CurrentMode, Mode=TwoWay}"
                          Style="{StaticResource MaterialChipsStyle}">
                <toolkit:Chip Content="Pomodoro" 
                            Tag="Pomodoro"
                            Command="{Binding ChangeModeCommand}"
                            CommandParameter="Pomodoro"/>
                <toolkit:Chip Content="Short Break" 
                            Tag="ShortBreak"
                            Command="{Binding ChangeModeCommand}"
                            CommandParameter="ShortBreak"/>
                <toolkit:Chip Content="Long Break" 
                            Tag="LongBreak"
                            Command="{Binding ChangeModeCommand}"
                            CommandParameter="LongBreak"/>
            </toolkit:Chips>
        </StackPanel>
        
        <!-- Timer Display -->
        <Grid Grid.Row="1" 
              VerticalAlignment="Center"
              HorizontalAlignment="Center">
            
            <!-- Circular Progress -->
            <controls:CircularTimer TimeRemaining="{Binding TimeRemaining}"
                                   TotalTime="{Binding TotalTime}"
                                   Height="300"
                                   Width="300"/>
            
            <!-- Digital Time Display -->
            <TextBlock Text="{Binding TimeRemaining, Converter={StaticResource TimeSpanToStringConverter}}"
                      FontSize="72"
                      FontWeight="Bold"
                      HorizontalAlignment="Center"
                      VerticalAlignment="Center"/>
        </Grid>
        
        <!-- Control Buttons -->
        <StackPanel Grid.Row="2" 
                   Orientation="Horizontal" 
                   HorizontalAlignment="Center"
                   Margin="20,10,20,30"
                   Spacing="20">
            
            <!-- Start Button -->
            <Button Command="{Binding StartTimerCommand}"
                   Visibility="{Binding IsRunning, Converter={StaticResource InverseBoolToVisibilityConverter}}"
                   Style="{StaticResource MaterialFilledButtonStyle}"
                   MinWidth="120"
                   Height="48">
                <StackPanel Orientation="Horizontal" Spacing="8">
                    <SymbolIcon Symbol="Play"/>
                    <TextBlock Text="Start"/>
                </StackPanel>
            </Button>
            
            <!-- Pause Button -->
            <Button Command="{Binding PauseTimerCommand}"
                   Visibility="{Binding IsRunning, Converter={StaticResource BoolToVisibilityConverter}}"
                   Style="{StaticResource MaterialFilledButtonStyle}"
                   MinWidth="120"
                   Height="48">
                <StackPanel Orientation="Horizontal" Spacing="8">
                    <SymbolIcon Symbol="Pause"/>
                    <TextBlock Text="Pause"/>
                </StackPanel>
            </Button>
            
            <!-- Reset Button -->
            <Button Command="{Binding ResetTimerCommand}"
                   Style="{StaticResource MaterialOutlinedButtonStyle}"
                   MinWidth="120"
                   Height="48">
                <StackPanel Orientation="Horizontal" Spacing="8">
                    <SymbolIcon Symbol="Refresh"/>
                    <TextBlock Text="Reset"/>
                </StackPanel>
            </Button>
        </StackPanel>
    </Grid>
</Page>
```

### Circular Timer Control
```csharp
// Views/Controls/CircularTimer.xaml.cs
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using System;

namespace PomodoroUno.Views.Controls
{
    public sealed partial class CircularTimer : UserControl
    {
        public static readonly DependencyProperty TimeRemainingProperty =
            DependencyProperty.Register(nameof(TimeRemaining), typeof(TimeSpan), 
                typeof(CircularTimer), new PropertyMetadata(TimeSpan.Zero, OnTimeChanged));
        
        public static readonly DependencyProperty TotalTimeProperty =
            DependencyProperty.Register(nameof(TotalTime), typeof(TimeSpan), 
                typeof(CircularTimer), new PropertyMetadata(TimeSpan.FromMinutes(25)));
        
        public TimeSpan TimeRemaining
        {
            get => (TimeSpan)GetValue(TimeRemainingProperty);
            set => SetValue(TimeRemainingProperty, value);
        }
        
        public TimeSpan TotalTime
        {
            get => (TimeSpan)GetValue(TotalTimeProperty);
            set => SetValue(TotalTimeProperty, value);
        }
        
        private Path _progressPath;
        private TextBlock _timeText;
        
        public CircularTimer()
        {
            InitializeComponent();
        }
        
        private static void OnTimeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is CircularTimer timer)
            {
                timer.UpdateProgress();
            }
        }
        
        private void UpdateProgress()
        {
            if (_progressPath == null)
                return;
            
            var progress = TotalTime.TotalSeconds > 0 
                ? TimeRemaining.TotalSeconds / TotalTime.TotalSeconds 
                : 0;
            
            // Update arc path based on progress
            UpdateArcPath(progress);
            
            // Update color based on mode and progress
            UpdateProgressColor(progress);
        }
        
        private void UpdateArcPath(double progress)
        {
            // Calculate arc path geometry
            var angle = progress * 360;
            // Implementation of arc path calculation
        }
        
        private void UpdateProgressColor(double progress)
        {
            // Change color based on progress
            if (progress < 0.25)
            {
                _progressPath.Stroke = new SolidColorBrush(Microsoft.UI.Colors.Red);
            }
            else if (progress < 0.5)
            {
                _progressPath.Stroke = new SolidColorBrush(Microsoft.UI.Colors.Orange);
            }
            else
            {
                _progressPath.Stroke = new SolidColorBrush(Microsoft.UI.Colors.Green);
            }
        }
    }
}
```

## Data Service Implementation

```csharp
// Services/SqliteDataService.cs
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using PomodoroUno.Models;
using PomodoroUno.Services.Interfaces;

namespace PomodoroUno.Services
{
    public class SqliteDataService : IDataService
    {
        private readonly string _connectionString;
        
        public SqliteDataService()
        {
            var dbPath = System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "pomodoro.db"
            );
            _connectionString = $"Data Source={dbPath}";
            InitializeDatabaseAsync().Wait();
        }
        
        private async Task InitializeDatabaseAsync()
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();
            
            var createTables = @"
                CREATE TABLE IF NOT EXISTS tasks (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    text TEXT NOT NULL,
                    completed INTEGER DEFAULT 0,
                    sessionId TEXT NOT NULL,
                    completedAt TEXT
                );
                
                CREATE TABLE IF NOT EXISTS sessions (
                    id TEXT PRIMARY KEY,
                    startTime TEXT DEFAULT CURRENT_TIMESTAMP,
                    endTime TEXT,
                    mode TEXT
                );
                
                CREATE INDEX IF NOT EXISTS idx_tasks_sessionId ON tasks(sessionId);
                CREATE INDEX IF NOT EXISTS idx_sessions_startTime ON sessions(startTime);";
            
            using var command = new SqliteCommand(createTables, connection);
            await command.ExecuteNonQueryAsync();
        }
        
        public async Task<List<PomodoroTask>> GetTasksAsync(string sessionId)
        {
            var tasks = new List<PomodoroTask>();
            
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();
            
            var query = "SELECT * FROM tasks WHERE sessionId = @sessionId ORDER BY id ASC";
            using var command = new SqliteCommand(query, connection);
            command.Parameters.AddWithValue("@sessionId", sessionId);
            
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                tasks.Add(new PomodoroTask
                {
                    Id = reader.GetInt32(0),
                    Text = reader.GetString(1),
                    IsCompleted = reader.GetInt32(2) == 1,
                    SessionId = reader.GetString(3),
                    CompletedAt = reader.IsDBNull(4) ? null : DateTime.Parse(reader.GetString(4))
                });
            }
            
            return tasks;
        }
        
        public async Task<PomodoroTask> AddTaskAsync(PomodoroTask task)
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();
            
            var query = @"INSERT INTO tasks (text, sessionId) VALUES (@text, @sessionId);
                         SELECT last_insert_rowid();";
            
            using var command = new SqliteCommand(query, connection);
            command.Parameters.AddWithValue("@text", task.Text);
            command.Parameters.AddWithValue("@sessionId", task.SessionId);
            
            var id = Convert.ToInt32(await command.ExecuteScalarAsync());
            task.Id = id;
            
            return task;
        }
        
        public async Task UpdateTaskAsync(PomodoroTask task)
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();
            
            var query = @"UPDATE tasks 
                         SET completed = @completed, completedAt = @completedAt 
                         WHERE id = @id";
            
            using var command = new SqliteCommand(query, connection);
            command.Parameters.AddWithValue("@completed", task.IsCompleted ? 1 : 0);
            command.Parameters.AddWithValue("@completedAt", 
                task.CompletedAt?.ToString("yyyy-MM-dd HH:mm:ss") ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@id", task.Id);
            
            await command.ExecuteNonQueryAsync();
        }
        
        public async Task DeleteTaskAsync(int taskId)
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();
            
            var query = "DELETE FROM tasks WHERE id = @id";
            using var command = new SqliteCommand(query, connection);
            command.Parameters.AddWithValue("@id", taskId);
            
            await command.ExecuteNonQueryAsync();
        }
        
        public async Task<PomodoroSession> CreateSessionAsync(PomodoroSession session)
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();
            
            var query = @"INSERT INTO sessions (id, startTime, mode) 
                         VALUES (@id, @startTime, @mode)";
            
            using var command = new SqliteCommand(query, connection);
            command.Parameters.AddWithValue("@id", session.Id);
            command.Parameters.AddWithValue("@startTime", 
                session.StartTime.ToString("yyyy-MM-dd HH:mm:ss"));
            command.Parameters.AddWithValue("@mode", session.Mode.ToString());
            
            await command.ExecuteNonQueryAsync();
            
            return session;
        }
        
        public async Task EndSessionAsync(string sessionId)
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();
            
            var query = @"UPDATE sessions 
                         SET endTime = @endTime 
                         WHERE id = @id";
            
            using var command = new SqliteCommand(query, connection);
            command.Parameters.AddWithValue("@endTime", 
                DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            command.Parameters.AddWithValue("@id", sessionId);
            
            await command.ExecuteNonQueryAsync();
        }
        
        public async Task<List<PomodoroSession>> GetSessionsAsync()
        {
            var sessions = new List<PomodoroSession>();
            
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();
            
            var query = @"SELECT s.*, 
                         COUNT(t.id) as totalTasks,
                         SUM(CASE WHEN t.completed = 1 THEN 1 ELSE 0 END) as completedTasks
                         FROM sessions s
                         LEFT JOIN tasks t ON s.id = t.sessionId
                         GROUP BY s.id
                         ORDER BY s.startTime DESC";
            
            using var command = new SqliteCommand(query, connection);
            using var reader = await command.ExecuteReaderAsync();
            
            while (await reader.ReadAsync())
            {
                sessions.Add(new PomodoroSession
                {
                    Id = reader.GetString(0),
                    StartTime = DateTime.Parse(reader.GetString(1)),
                    EndTime = reader.IsDBNull(2) ? null : DateTime.Parse(reader.GetString(2)),
                    Mode = Enum.Parse<TimerMode>(reader.GetString(3))
                });
            }
            
            return sessions;
        }
    }
}
```

## Converters

```csharp
// Converters/TimeSpanToStringConverter.cs
using Microsoft.UI.Xaml.Data;
using System;

namespace PomodoroUno.Converters
{
    public class TimeSpanToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is TimeSpan timeSpan)
            {
                return $"{(int)timeSpan.TotalMinutes:00}:{timeSpan.Seconds:00}";
            }
            return "00:00";
        }
        
        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}

// Converters/BoolToVisibilityConverter.cs
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using System;

namespace PomodoroUno.Converters
{
    public class BoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is bool boolValue)
            {
                return boolValue ? Visibility.Visible : Visibility.Collapsed;
            }
            return Visibility.Collapsed;
        }
        
        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            if (value is Visibility visibility)
            {
                return visibility == Visibility.Visible;
            }
            return false;
        }
    }
}

// Converters/InverseBoolToVisibilityConverter.cs
namespace PomodoroUno.Converters
{
    public class InverseBoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is bool boolValue)
            {
                return boolValue ? Visibility.Collapsed : Visibility.Visible;
            }
            return Visibility.Visible;
        }
        
        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            if (value is Visibility visibility)
            {
                return visibility == Visibility.Collapsed;
            }
            return true;
        }
    }
}
```

## App.xaml Configuration

```xml
<!-- App.xaml -->
<Application x:Class="PomodoroUno.App"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:converters="using:PomodoroUno.Converters">
    
    <Application.Resources>
        <ResourceDictionary>
            <ResourceDictionary.MergedDictionaries>
                <XamlControlsResources xmlns="using:Microsoft.UI.Xaml.Controls" />
                <ResourceDictionary Source="ms-appx:///Uno.Toolkit.WinUI/Themes/Generic.xaml" />
            </ResourceDictionary.MergedDictionaries>
            
            <!-- Converters -->
            <converters:TimeSpanToStringConverter x:Key="TimeSpanToStringConverter"/>
            <converters:BoolToVisibilityConverter x:Key="BoolToVisibilityConverter"/>
            <converters:InverseBoolToVisibilityConverter x:Key="InverseBoolToVisibilityConverter"/>
            
            <!-- Colors -->
            <Color x:Key="PrimaryColor">#FF6B6B</Color>
            <Color x:Key="SecondaryColor">#4ECDC4</Color>
            <Color x:Key="SurfaceColor">#F7F7F7</Color>
            <Color x:Key="OnSurfaceColor">#2D2D2D</Color>
            
            <!-- Brushes -->
            <SolidColorBrush x:Key="PrimaryBrush" Color="{StaticResource PrimaryColor}"/>
            <SolidColorBrush x:Key="SecondaryBrush" Color="{StaticResource SecondaryColor}"/>
            <SolidColorBrush x:Key="SurfaceBrush" Color="{StaticResource SurfaceColor}"/>
            <SolidColorBrush x:Key="OnSurfaceBrush" Color="{StaticResource OnSurfaceColor}"/>
        </ResourceDictionary>
    </Application.Resources>
</Application>
```
