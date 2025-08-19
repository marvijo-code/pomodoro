# Pomodoro App Migration to Uno Platform - Technical Implementation Plan

## Executive Summary
This document outlines the complete technical plan for migrating the existing React-based Pomodoro web application to Uno Platform, with initial focus on Android deployment. The migration will leverage Uno Platform's latest version (5.5+) and utilize CLI tools for efficient project setup and development.

## Table of Contents
1. [Current Application Analysis](#current-application-analysis)
2. [Uno Platform Architecture](#uno-platform-architecture)
3. [Prerequisites & Environment Setup](#prerequisites--environment-setup)
4. [Project Initialization](#project-initialization)
5. [Core Migration Strategy](#core-migration-strategy)
6. [Implementation Phases](#implementation-phases)
7. [Data Layer Migration](#data-layer-migration)
8. [UI/UX Migration](#uiux-migration)
9. [Platform-Specific Considerations](#platform-specific-considerations)
10. [Testing Strategy](#testing-strategy)
11. [Deployment Pipeline](#deployment-pipeline)
12. [Timeline & Milestones](#timeline--milestones)

---

## Current Application Analysis

### Existing Features
1. **Timer Functionality**
   - 25-minute Pomodoro timer
   - 5-minute short break
   - 15-minute long break
   - Start/Pause/Reset controls
   - Visual countdown display

2. **Task Management**
   - Add/Delete tasks
   - Mark tasks as complete
   - Task persistence per session
   - Session-based task grouping

3. **Session History**
   - Track completed sessions
   - Display task completion statistics
   - Historical session review
   - Expandable task details

4. **Audio Notifications**
   - Completion sound alerts
   - Mute/Unmute functionality
   - Test sound feature
   - Loop alarm until dismissed

5. **Backend Services**
   - SQLite database
   - Express.js REST API
   - Session management
   - Task CRUD operations

### Technology Stack
- **Frontend**: React 18.3.1, Vite, Axios
- **Backend**: Express 4.21.1, SQLite3 5.1.7
- **Mobile (Expo)**: React Native with Expo Audio

---

## Uno Platform Architecture

### Target Version
- **Uno Platform**: 5.5.x (Latest stable)
- **Target Framework**: net9.0
- **Android Target**: API 21+ (Android 5.0+)

### Project Structure
```
PomodoroUno/
├── PomodoroUno/                    # Shared project
│   ├── Models/
│   ├── ViewModels/
│   ├── Views/
│   ├── Services/
│   ├── Controls/
│   └── Converters/
├── PomodoroUno.Mobile/             # Android head project
├── PomodoroUno.Windows/            # Windows head (future)
├── PomodoroUno.WebAssembly/        # WASM head (future)
└── PomodoroUno.Tests/              # Unit tests
```

---

## Prerequisites & Environment Setup

### Development Environment
```powershell
# 1. Install .NET 9 SDK
winget install Microsoft.DotNet.SDK.9

# 2. Install Uno Platform tools
dotnet tool install -g uno.check
dotnet tool install -g uno.templates

# 3. Verify installation
uno-check --fix
dotnet new list | findstr uno

# 4. Install Android workload
dotnet workload install android

# 5. Setup Android SDK (if not present)
# Download Android Studio or use command line tools
# Set ANDROID_HOME environment variable
```

### IDE Setup
```powershell
# Visual Studio 2022 Extensions
# Install via VS Extension Manager:
# - Uno Platform
# - Mobile development with .NET workload

# VS Code Extensions (alternative)
code --install-extension unoplatform.vscode
code --install-extension ms-dotnettools.csharp
```

---

## Project Initialization

### CLI Project Creation
```powershell
# Navigate to project directory
cd C:\dev\pomodoro-2

# Create new Uno app with specific configuration
dotnet new unoapp -o PomodoroUno `
  --preset=recommended `
  --theme=material `
  --toolkit=true `
  --server=true `
  --vscode=true `
  --android=true `
  --windows=false `
  --ios=false `
  --wasm=false `
  --macos=false `
  --linux=false

# Navigate to project
cd PomodoroUno

# Add required NuGet packages
dotnet add PomodoroUno/PomodoroUno.csproj package CommunityToolkit.Mvvm --version 8.3.2
dotnet add PomodoroUno/PomodoroUno.csproj package Microsoft.Extensions.Hosting --version 9.0.0
dotnet add PomodoroUno/PomodoroUno.csproj package Microsoft.Data.Sqlite --version 9.0.0
dotnet add PomodoroUno/PomodoroUno.csproj package System.Reactive --version 6.0.1
dotnet add PomodoroUno/PomodoroUno.csproj package Uno.Toolkit.WinUI --version 4.2.5
dotnet add PomodoroUno/PomodoroUno.csproj package Uno.Extensions.Reactive --version 4.5.0
dotnet add PomodoroUno/PomodoroUno.csproj package Uno.Extensions.Navigation --version 4.5.0
dotnet add PomodoroUno/PomodoroUno.csproj package Uno.Extensions.Hosting --version 4.5.0
dotnet add PomodoroUno/PomodoroUno.csproj package Uno.Extensions.Configuration --version 4.5.0

# Restore packages
dotnet restore

# Build initial project
dotnet build
```

---

## Core Migration Strategy

### Phase 1: Project Foundation (Week 1)

#### 1.1 Model Layer Setup
```csharp
// Models/PomodoroSession.cs
public class PomodoroSession
{
    public string Id { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public TimerMode Mode { get; set; }
    public List<PomodoroTask> Tasks { get; set; }
}

// Models/PomodoroTask.cs
public class PomodoroTask
{
    public int Id { get; set; }
    public string Text { get; set; }
    public bool IsCompleted { get; set; }
    public string SessionId { get; set; }
    public DateTime? CompletedAt { get; set; }
}

// Models/TimerMode.cs
public enum TimerMode
{
    Pomodoro,
    ShortBreak,
    LongBreak
}
```

#### 1.2 Service Layer Architecture
```powershell
# Create service structure via CLI
mkdir PomodoroUno\Services
mkdir PomodoroUno\Services\Interfaces

# Generate service files
dotnet new class -n ITimerService -o PomodoroUno\Services\Interfaces
dotnet new class -n TimerService -o PomodoroUno\Services
dotnet new class -n IDataService -o PomodoroUno\Services\Interfaces
dotnet new class -n SqliteDataService -o PomodoroUno\Services
dotnet new class -n IAudioService -o PomodoroUno\Services\Interfaces
dotnet new class -n AudioService -o PomodoroUno\Services
```

### Phase 2: ViewModels Implementation (Week 1-2)

#### 2.1 MVVM Setup with CommunityToolkit
```powershell
# Create ViewModels structure
mkdir PomodoroUno\ViewModels

# Generate ViewModel files
dotnet new class -n MainViewModel -o PomodoroUno\ViewModels
dotnet new class -n TimerViewModel -o PomodoroUno\ViewModels
dotnet new class -n TasksViewModel -o PomodoroUno\ViewModels
dotnet new class -n SessionHistoryViewModel -o PomodoroUno\ViewModels
dotnet new class -n SettingsViewModel -o PomodoroUno\ViewModels
```

#### 2.2 Dependency Injection Configuration
```csharp
// App.xaml.cs configuration
public static IHost Host { get; private set; }

protected override void OnLaunched(LaunchActivatedEventArgs args)
{
    Host = HostBuilder.Create()
        .ConfigureServices(services =>
        {
            // Services
            services.AddSingleton<ITimerService, TimerService>();
            services.AddSingleton<IDataService, SqliteDataService>();
            services.AddSingleton<IAudioService, AudioService>();
            
            // ViewModels
            services.AddTransient<MainViewModel>();
            services.AddTransient<TimerViewModel>();
            services.AddTransient<TasksViewModel>();
            services.AddTransient<SessionHistoryViewModel>();
            services.AddTransient<SettingsViewModel>();
        })
        .Build();
}
```

### Phase 3: UI Implementation (Week 2-3)

#### 3.1 Page Structure Creation
```powershell
# Create Views structure
mkdir PomodoroUno\Views
mkdir PomodoroUno\Views\Controls
mkdir PomodoroUno\Views\Pages

# Generate View files
dotnet new page -n MainPage -o PomodoroUno\Views\Pages
dotnet new page -n TimerPage -o PomodoroUno\Views\Pages
dotnet new page -n TasksPage -o PomodoroUno\Views\Pages
dotnet new page -n HistoryPage -o PomodoroUno\Views\Pages
dotnet new page -n SettingsPage -o PomodoroUno\Views\Pages

# Generate custom controls
dotnet new usercontrol -n CircularTimer -o PomodoroUno\Views\Controls
dotnet new usercontrol -n TaskItem -o PomodoroUno\Views\Controls
dotnet new usercontrol -n SessionCard -o PomodoroUno\Views\Controls
```

#### 3.2 Navigation Setup
```csharp
// Navigation configuration using Uno.Extensions.Navigation
services.AddNavigation(navigationBuilder =>
{
    navigationBuilder
        .AddNavigationRoute("Main", typeof(MainPage))
        .AddNavigationRoute("Timer", typeof(TimerPage))
        .AddNavigationRoute("Tasks", typeof(TasksPage))
        .AddNavigationRoute("History", typeof(HistoryPage))
        .AddNavigationRoute("Settings", typeof(SettingsPage));
});
```

---

## Implementation Phases

### Phase 1: Core Timer Functionality (Priority: High)
1. **Timer Service Implementation**
   - Countdown logic using System.Reactive
   - State management (Running, Paused, Stopped)
   - Mode switching (Pomodoro, Breaks)
   - Background timer support

2. **Timer UI**
   - Circular progress indicator
   - Digital time display
   - Control buttons (Start/Pause/Reset)
   - Mode selector

### Phase 2: Data Persistence (Priority: High)
1. **SQLite Integration**
   - Database initialization
   - Migration from existing schema
   - CRUD operations for tasks
   - Session management

2. **Data Migration Tool**
   ```powershell
   # Create migration utility
   dotnet new console -n DataMigrator -o Tools\DataMigrator
   dotnet add Tools\DataMigrator\DataMigrator.csproj package Microsoft.Data.Sqlite
   ```

### Phase 3: Task Management (Priority: Medium)
1. **Task CRUD Operations**
   - Add new tasks
   - Mark as complete
   - Delete tasks
   - Reorder tasks

2. **Task UI Components**
   - Task input field
   - Task list with checkboxes
   - Swipe-to-delete gesture
   - Drag-to-reorder

### Phase 4: Audio Notifications (Priority: Medium)
1. **Audio Service**
   - Load audio resources
   - Play/Stop/Loop functionality
   - Volume control
   - Platform-specific implementation

2. **Notification Integration**
   - Local notifications
   - Vibration support
   - Custom notification sounds

### Phase 5: Session History (Priority: Low)
1. **History View**
   - Session list
   - Statistics display
   - Expandable details
   - Date filtering

2. **Analytics**
   - Daily/Weekly/Monthly stats
   - Productivity charts
   - Export functionality

---

## Data Layer Migration

### SQLite Database Schema
```sql
-- Keep existing schema structure
CREATE TABLE IF NOT EXISTS tasks (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    text TEXT NOT NULL,
    completed BOOLEAN DEFAULT 0,
    sessionId TEXT NOT NULL,
    completedAt DATETIME
);

CREATE TABLE IF NOT EXISTS sessions (
    id TEXT PRIMARY KEY,
    startTime DATETIME DEFAULT CURRENT_TIMESTAMP,
    endTime DATETIME,
    mode TEXT
);

-- Add indexes for performance
CREATE INDEX idx_tasks_sessionId ON tasks(sessionId);
CREATE INDEX idx_sessions_startTime ON sessions(startTime);
```

### Data Access Implementation
```csharp
// Services/SqliteDataService.cs
public class SqliteDataService : IDataService
{
    private readonly string _connectionString;
    
    public SqliteDataService()
    {
        var dbPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "pomodoro.db"
        );
        _connectionString = $"Data Source={dbPath}";
        InitializeDatabase();
    }
    
    private async Task InitializeDatabase()
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();
        
        var createTables = @"
            CREATE TABLE IF NOT EXISTS tasks (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                text TEXT NOT NULL,
                completed BOOLEAN DEFAULT 0,
                sessionId TEXT NOT NULL,
                completedAt DATETIME
            );
            
            CREATE TABLE IF NOT EXISTS sessions (
                id TEXT PRIMARY KEY,
                startTime DATETIME DEFAULT CURRENT_TIMESTAMP,
                endTime DATETIME,
                mode TEXT
            );";
        
        using var command = new SqliteCommand(createTables, connection);
        await command.ExecuteNonQueryAsync();
    }
    
    public async Task<List<PomodoroTask>> GetTasksAsync(string sessionId)
    {
        // Implementation
    }
    
    public async Task<PomodoroTask> AddTaskAsync(PomodoroTask task)
    {
        // Implementation
    }
    
    public async Task UpdateTaskAsync(PomodoroTask task)
    {
        // Implementation
    }
    
    public async Task DeleteTaskAsync(int taskId)
    {
        // Implementation
    }
}
```

---

## UI/UX Migration

### Design System
1. **Material Design 3 Theme**
   - Primary: #FF6B6B (Tomato red)
   - Secondary: #4ECDC4 (Teal)
   - Surface: #F7F7F7
   - OnSurface: #2D2D2D

2. **Typography**
   - Display: Segoe UI Bold 72sp (Timer)
   - Headline: Segoe UI Semibold 24sp
   - Body: Segoe UI Regular 16sp

3. **Components**
   - Material You dynamic theming
   - Rounded corners (12dp)
   - Elevation shadows
   - Ripple effects

### Responsive Layout
```xml
<!-- Adaptive layout for different screen sizes -->
<Grid>
    <VisualStateManager.VisualStateGroups>
        <VisualStateGroup>
            <VisualState x:Name="WideState">
                <VisualState.StateTriggers>
                    <AdaptiveTrigger MinWindowWidth="768"/>
                </VisualState.StateTriggers>
                <!-- Wide layout -->
            </VisualState>
            <VisualState x:Name="NarrowState">
                <VisualState.StateTriggers>
                    <AdaptiveTrigger MinWindowWidth="0"/>
                </VisualState.StateTriggers>
                <!-- Narrow layout -->
            </VisualState>
        </VisualStateGroup>
    </VisualStateManager.VisualStateGroups>
</Grid>
```

---

## Platform-Specific Considerations

### Android-Specific Features
1. **Permissions**
   ```xml
   <!-- AndroidManifest.xml -->
   <uses-permission android:name="android.permission.VIBRATE" />
   <uses-permission android:name="android.permission.WAKE_LOCK" />
   <uses-permission android:name="android.permission.POST_NOTIFICATIONS" />
   <uses-permission android:name="android.permission.SCHEDULE_EXACT_ALARM" />
   ```

2. **Background Service**
   ```csharp
   // Android foreground service for timer
   [Service(ForegroundServiceType = ForegroundService.TypeDataSync)]
   public class TimerForegroundService : Service
   {
       private ITimerService _timerService;
       private NotificationManager _notificationManager;
       
       public override StartCommandResult OnStartCommand(Intent intent, StartCommandFlags flags, int startId)
       {
           CreateNotificationChannel();
           var notification = BuildNotification();
           StartForeground(1001, notification);
           
           return StartCommandResult.Sticky;
       }
       
       private Notification BuildNotification()
       {
           // Build foreground notification
       }
   }
   ```

3. **Widget Support**
   - Home screen widget for quick timer access
   - App shortcuts for timer modes

### Performance Optimization
1. **Startup Performance**
   - AOT compilation
   - Lazy loading of views
   - Resource optimization

2. **Memory Management**
   - Dispose patterns for timers
   - Image caching
   - List virtualization

---

## Testing Strategy

### Unit Testing
```powershell
# Create test project
dotnet new xunit -n PomodoroUno.Tests -o PomodoroUno.Tests
dotnet add PomodoroUno.Tests\PomodoroUno.Tests.csproj reference PomodoroUno\PomodoroUno.csproj

# Add testing packages
dotnet add PomodoroUno.Tests package Moq --version 4.20.72
dotnet add PomodoroUno.Tests package FluentAssertions --version 6.12.1
dotnet add PomodoroUno.Tests package xunit.runner.visualstudio --version 2.8.2
```

### Test Categories
1. **Unit Tests**
   - Timer logic
   - Data service operations
   - ViewModel commands
   - Converters

2. **Integration Tests**
   - Database operations
   - Service interactions
   - Navigation flows

3. **UI Tests**
   ```powershell
   # Add UI testing
   dotnet add PomodoroUno.Mobile package Uno.UITest --version 5.5.0
   ```

---

## Deployment Pipeline

### Android Build Configuration
```powershell
# 1. Configure Android signing
# Generate keystore
keytool -genkey -v -keystore pomodoro.keystore `
  -alias pomodoro -keyalg RSA -keysize 2048 `
  -validity 10000

# 2. Configure build properties
# PomodoroUno.Mobile/PomodoroUno.Mobile.csproj
```

```xml
<PropertyGroup Condition="'$(Configuration)' == 'Release'">
  <AndroidKeyStore>true</AndroidKeyStore>
  <AndroidSigningKeyStore>pomodoro.keystore</AndroidSigningKeyStore>
  <AndroidSigningKeyAlias>pomodoro</AndroidSigningKeyAlias>
  <AndroidSigningKeyPass>$(KeyPassword)</AndroidSigningKeyPass>
  <AndroidSigningStorePass>$(StorePassword)</AndroidSigningStorePass>
  <AndroidPackageFormat>apk;aab</AndroidPackageFormat>
  <AndroidUseAapt2>true</AndroidUseAapt2>
  <EnableProguard>true</EnableProguard>
</PropertyGroup>
```

### Build Commands
```powershell
# Development build
dotnet build PomodoroUno.Mobile/PomodoroUno.Mobile.csproj `
  -c Debug `
  -f net9.0-android

# Release build for deployment
dotnet publish PomodoroUno.Mobile/PomodoroUno.Mobile.csproj `
  -c Release `
  -f net9.0-android `
  -p:AndroidKeyStore=true `
  -p:AndroidSigningKeyStore=pomodoro.keystore `
  -p:AndroidSigningKeyAlias=pomodoro

# Generate APK and AAB
dotnet build PomodoroUno.Mobile/PomodoroUno.Mobile.csproj `
  -c Release `
  -f net9.0-android `
  -p:AndroidPackageFormat="apk;aab"
```

### CI/CD Pipeline (GitHub Actions)
```yaml
# .github/workflows/android-build.yml
name: Android Build

on:
  push:
    branches: [ main, develop ]
  pull_request:
    branches: [ main ]

jobs:
  build:
    runs-on: windows-latest
    
    steps:
    - uses: actions/checkout@v4
    
    - name: Setup .NET
      uses: actions/setup-dotnet@v4
      with:
        dotnet-version: '9.0.x'
    
    - name: Install Uno Tools
      run: |
        dotnet tool install -g uno.check
        uno-check --fix --non-interactive
    
    - name: Restore dependencies
      run: dotnet restore
    
    - name: Build
      run: |
        dotnet build PomodoroUno.Mobile/PomodoroUno.Mobile.csproj `
          -c Release `
          -f net9.0-android
    
    - name: Run tests
      run: dotnet test PomodoroUno.Tests/PomodoroUno.Tests.csproj
    
    - name: Upload APK
      uses: actions/upload-artifact@v4
      with:
        name: android-apk
        path: PomodoroUno.Mobile/bin/Release/net9.0-android/*.apk
```

---

## Timeline & Milestones

### Week 1: Foundation & Setup
**Goal**: Establish project structure and core services

- [ ] Day 1-2: Environment setup and project initialization
  ```powershell
  # Initialize project
  dotnet new unoapp -o PomodoroUno --preset=recommended
  # Add packages
  dotnet restore
  ```

- [ ] Day 3-4: Model and service layer implementation
  - Create data models
  - Implement ITimerService
  - Setup SQLite database

- [ ] Day 5: Dependency injection and hosting
  - Configure DI container
  - Setup IHost
  - Register services

### Week 2: Core Features
**Goal**: Implement timer and task management

- [ ] Day 1-2: Timer functionality
  - Timer ViewModel
  - Countdown logic
  - Mode switching

- [ ] Day 3-4: Task management
  - Task CRUD operations
  - Task ViewModel
  - Data persistence

- [ ] Day 5: Integration testing
  - Service tests
  - ViewModel tests

### Week 3: UI Implementation
**Goal**: Create responsive UI with Material Design

- [ ] Day 1-2: Main timer UI
  - Circular timer control
  - Control buttons
  - Mode selector

- [ ] Day 3-4: Task management UI
  - Task list view
  - Add/Edit/Delete UI
  - Swipe gestures

- [ ] Day 5: Polish and animations
  - Transitions
  - Loading states
  - Error handling

### Week 4: Advanced Features
**Goal**: Audio, notifications, and history

- [ ] Day 1-2: Audio integration
  - Audio service
  - Notification sounds
  - Settings management

- [ ] Day 3-4: Session history
  - History view
  - Statistics
  - Data visualization

- [ ] Day 5: Platform optimizations
  - Performance tuning
  - Memory optimization
  - Startup time

### Week 5: Testing & Deployment
**Goal**: Complete testing and prepare for release

- [ ] Day 1-2: Comprehensive testing
  - UI tests
  - Integration tests
  - Performance tests

- [ ] Day 3-4: Bug fixes and polish
  - Address test findings
  - UI refinements
  - Accessibility

- [ ] Day 5: Deployment preparation
  - Build release version
  - Generate signed APK/AAB
  - Documentation

---

## Key Implementation Files

See `implementation-examples.md` for detailed code examples.

---

## Migration Checklist

### Pre-Migration
- [x] Analyze existing codebase
- [x] Document current features
- [x] Plan architecture
- [ ] Setup development environment
- [ ] Create project structure

### Core Migration
- [ ] Initialize Uno project
- [ ] Migrate data models
- [ ] Implement services
- [ ] Create ViewModels
- [ ] Build UI screens

### Feature Parity
- [ ] Timer functionality
- [ ] Task management
- [ ] Session history
- [ ] Audio notifications
- [ ] Settings persistence

### Platform Optimization
- [ ] Android-specific features
- [ ] Performance tuning
- [ ] Background service
- [ ] Local notifications
- [ ] App widgets

### Quality Assurance
- [ ] Unit tests (>80% coverage)
- [ ] Integration tests
- [ ] UI automation tests
- [ ] Performance profiling
- [ ] Accessibility audit

### Deployment
- [ ] Configure signing
- [ ] Setup CI/CD
- [ ] Build release APK
- [ ] Generate AAB for Play Store
- [ ] Create deployment documentation

---

## Risk Assessment & Mitigation

### Technical Risks
1. **SQLite Migration**
   - Risk: Data compatibility issues
   - Mitigation: Create migration tool, extensive testing

2. **Background Timer**
   - Risk: Android battery optimization
   - Mitigation: Use foreground service, proper wake locks

3. **Audio Playback**
   - Risk: Platform-specific issues
   - Mitigation: Abstract audio service, fallback options

### Schedule Risks
1. **Learning Curve**
   - Risk: Uno Platform complexity
   - Mitigation: Allocate extra time for Week 1

2. **Feature Creep**
   - Risk: Adding unplanned features
   - Mitigation: Strict MVP definition, phase approach

---

## Success Criteria

### Functional Requirements
- ✅ Timer operates accurately in all modes
- ✅ Tasks persist across sessions
- ✅ Audio notifications work reliably
- ✅ Session history displays correctly
- ✅ App works offline

### Performance Metrics
- App startup: < 2 seconds
- Timer accuracy: ±1 second
- Memory usage: < 100MB
- Battery impact: < 5% per hour
- APK size: < 25MB

### Quality Metrics
- Code coverage: > 80%
- Crash-free rate: > 99.5%
- UI responsiveness: 60 FPS
- Accessibility: WCAG 2.1 AA

---

## Conclusion

This migration plan provides a comprehensive roadmap for converting the React-based Pomodoro app to Uno Platform targeting Android. The CLI-first approach ensures reproducibility and automation, while the phased implementation allows for incremental progress and testing.

### Next Steps
1. Execute Week 1 foundation setup
2. Create development branch
3. Initialize Uno project using CLI
4. Begin service layer implementation
5. Set up CI/CD pipeline

### Support Resources
- [Uno Platform Documentation](https://platform.uno/docs/)
- [Uno Platform Samples](https://github.com/unoplatform/uno.samples)
- [Community Discord](https://discord.gg/unoplatform)
- [Stack Overflow Tag](https://stackoverflow.com/questions/tagged/uno-platform)

---

**Document Version**: 1.0  
**Last Updated**: 2025-01-19  
**Author**: Migration Team  
**Status**: Ready for Implementation
