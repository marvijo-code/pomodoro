# Migration Plan: `web-old` Pomodoro to Uno Platform (Android)

This document defines a complete, CLI-first plan to migrate the existing React + Express + SQLite Pomodoro app in `web-old/` into an Uno Platform Single Project app focused on Android. It covers project creation, dependencies, data storage, UI structure, notifications/sound, lifecycle, testing, and delivery.


## Goals
- Replace `web-old/` React UI and `server.js` backend with a single Uno Platform Android app.
- Preserve core features:
  - Pomodoro timer with modes: pomodoro (25), short break (5), long break (15).
  - Tasks CRUD per session.
  - Sessions history with completion stats and per-session task list.
  - Audible alarm and optional notifications when timer completes.
  - Sound enable/disable toggle; test sound button.
- Local offline storage using SQLite on-device.
- MVVM-based architecture to keep logic testable.
- Keep the plan CLI-first. Avoid writing code here; where snippets are needed, keep them minimal.


## Source App Summary (from `web-old/`)
- Frontend: React + Vite (`src/App.jsx`, `src/main.jsx`, `src/index.css`).
- Backend: `server.js` (Express + `sqlite3`) exposing endpoints:
  - `GET /api/tasks/:sessionId`
  - `GET /api/sessions` (with aggregated stats)
  - `POST /api/sessions`
  - `POST /api/tasks`
  - `PUT /api/tasks/:id` (toggle completion)
  - `DELETE /api/tasks/:id`
- Data: `tasks.db` with tables `tasks` and `sessions`.
- Assets: `public/notification.wav`.


## Target Architecture (Uno Platform Single Project)
- Single Project using Uno Platform and .NET 8 (LTS). Skia renderer default.
- Platforms: Android only (can add more later).
- UI: XAML + C# with MVVM.
- Data: SQLite on device (either `sqlite-net-pcl` or `Microsoft.Data.Sqlite`).
- Alarms/Notifications: Android local notifications via NotificationCompat; sound via `Android.Media.MediaPlayer` and bundled asset.
- No embedded web server; replace REST with a repository/service layer.


## Prerequisites (CLI)
- Install/Update Uno prerequisites checker:
  - dotnet tool install -g uno.check
  - dotnet tool update -g uno.check
- Verify environment:
  - uno-check
- Ensure .NET SDK 8 or 9 installed: dotnet --version
- Android workload (if not already installed):
  - dotnet workload install android

References:
- Uno templates install: dotnet new install Uno.Templates
- CLI template docs: https://platform.uno/docs/articles/get-started-dotnet-new.html
- Platform flags: https://platform.uno/docs/articles/getting-started/wizard/using-wizard.html


## Create the Uno Project (Android-only)
- Install templates (if not done yet):
  - dotnet new install Uno.Templates

- Create a blank preset, Android-only, Skia renderer (default), targeting .NET 8:
  - dotnet new unoapp -preset blank -platforms android -tfm net8.0 -o UnoPomodoro

- Alternative (native renderer):
  - dotnet new unoapp -preset blank -platforms android -tfm net8.0 -renderer native -o UnoPomodoro

- Optional (recommended preset with Uno.Extensions, more scaffolding):
  - dotnet new unoapp -preset recommended -platforms android -tfm net8.0 -o UnoPomodoro

Note: To target .NET 9 (STS), replace `-tfm net8.0` with `-tfm net9.0`.


## Project Identity and Android Manifest
- Set application id via MSBuild (ApplicationId) and generate manifest.
- Edit `UnoPomodoro/UnoPomodoro.csproj`:
  - Ensure `<GenerateApplicationManifest>true</GenerateApplicationManifest>`.
  - Add `<ApplicationId>com.marvijocode.pomodoro</ApplicationId>` inside a `<PropertyGroup>`.

References:
- ApplicationId property: https://learn.microsoft.com/dotnet/android/building-apps/build-properties#applicationid


## Dependencies (CLI)
Run in project root `UnoPomodoro/`:
- MVVM Toolkit (view models & commands):
  - dotnet add package CommunityToolkit.Mvvm

- SQLite (choose one):
  - Option A – sqlite-net-pcl (simplified ORM):
    - dotnet add package sqlite-net-pcl
  - Option B – Microsoft.Data.Sqlite (ADO.NET style):
    - dotnet add package Microsoft.Data.Sqlite

- Logging (optional, recommended):
  - dotnet add package Microsoft.Extensions.Logging.Abstractions

- Uno Toolkit UI (optional controls & helpers):
  - dotnet add package Uno.Toolkit.UI

No extra package is required for Android notifications (use AndroidX NotificationCompat via platform APIs included in .NET for Android); for vibration, use Uno’s `Windows.Phone.Devices.Notification.VibrationDevice` API.

- AndroidX (optional, for compatibility helpers and background scheduling):
  - dotnet add package Xamarin.AndroidX.Core
  - dotnet add package Xamarin.AndroidX.AppCompat
  - dotnet add package Xamarin.AndroidX.Work.Runtime
  
Notes:
- `Xamarin.AndroidX.Core` provides NotificationCompat APIs when not already transitively included.
- `Xamarin.AndroidX.Work.Runtime` is only needed if you choose WorkManager for background reliability.


## Assets (Alarm Sound)
- Reuse `web-old/public/notification.wav`.
- Copy to Uno project as Android asset:
  - PowerShell (from repo root):
    - New-Item -ItemType Directory -Path .\UnoPomodoro\Assets\Audio -Force | Out-Null
    - Copy-Item .\web-old\public\notification.wav .\UnoPomodoro\Assets\Audio\notification.wav -Force

- Ensure Build Action for `Assets/Audio/notification.wav` is set to `AndroidAsset` (verify in project file or via IDE). If using default SDK conventions, include explicitly if not picked up.


## Data Model and Storage
- Schema parity with `web-old`:
  - Table `sessions`:
    - id TEXT PRIMARY KEY
    - startTime DATETIME DEFAULT CURRENT_TIMESTAMP
    - endTime DATETIME NULL
    - mode TEXT
  - Table `tasks`:
    - id INTEGER PRIMARY KEY AUTOINCREMENT
    - text TEXT NOT NULL
    - completed BOOLEAN DEFAULT 0
    - sessionId TEXT NOT NULL (FK to sessions.id)
    - completedAt DATETIME NULL

- Initialization Plan:
  - Create a `DatabaseInitializer` service to ensure DB exists and tables are created on first run.
  - If migrating existing data, place `tasks.db` in app package as an asset and copy to app data on first run. Otherwise, create an empty DB.

- DB File Location (runtime):
  - Use app data directory (e.g., `Environment.SpecialFolder.LocalApplicationData`).

- Repository Layer:
  - `ISessionRepository` (CreateSession, GetSessionsWithStats, CloseSession)
  - `ITaskRepository` (GetBySession, Add, ToggleCompleted, Delete)

- CLI-only steps for data (no code here):
  - dotnet new classlib -o UnoPomodoro.Data
  - dotnet sln add .\UnoPomodoro.Data\UnoPomodoro.Data.csproj
  - dotnet add .\UnoPomodoro\UnoPomodoro.csproj reference .\UnoPomodoro.Data\UnoPomodoro.Data.csproj
  - dotnet add .\UnoPomodoro.Data\UnoPomodoro.Data.csproj package sqlite-net-pcl (or Microsoft.Data.Sqlite)


## API-to-Repository Mapping (from `web-old/server.js`)
- `GET /api/tasks/:sessionId` → `ITaskRepository.GetBySession(sessionId)`
- `POST /api/tasks` → `ITaskRepository.Add(text, sessionId)`
- `PUT /api/tasks/:id` → `ITaskRepository.ToggleCompleted(id, completed)`
- `DELETE /api/tasks/:id` → `ITaskRepository.Delete(id)`
- `POST /api/sessions` → `ISessionRepository.CreateSession(sessionId, mode, startTime)`
- `GET /api/sessions` → `ISessionRepository.GetSessionsWithStats()` (returns rows with `totalTasks`, `completedTasks`, and task lists)


## Feature Migration Plan

### 1) Timer Engine
- Replace JS `setInterval` with `DispatcherTimer` or a `PeriodicTimer` + Dispatcher marshaling for UI updates.
- Store `TargetEndTime` on Start. On resume/background return, recompute remaining seconds from wall clock to avoid drift and background suspension issues.
- Expose `Start`, `Pause`, `Reset`, `ChangeMode` methods in a `TimerService`.
- When time reaches zero:
  - Trigger local Android notification and play looping alarm sound until user acknowledges.

### 2) Modes
- Keep constants in settings/service: pomodoro (25m), shortBreak (5m), longBreak (15m).
- Allow future configurability via app settings.

### 3) Sessions
- On Start when not running:
  - Create new session row with `id = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()` string (parity with web).
  - Save current `mode` and `startTime`.
- On Stop when completed: set `endTime`.
- History screen shows sessions list with counts and completion stats (replicate `GET /api/sessions` aggregation locally via SQL query or LINQ over joined data).

### 4) Tasks
- Per-session list with CRUD:
  - Add Task(text)
  - Toggle Completed(taskId, bool)
  - Delete(taskId)
- For toggling: set `completedAt = now` when completed, null when uncompleted (parity with web).

### 5) Notifications & Alarm
- Android local notification on timer end using NotificationCompat.
- Sound Handling:
  - Use `Android.Media.MediaPlayer` to play `Assets/Audio/notification.wav` in loop.
  - Provide `Stop Alarm` button to stop sound.
- Permissions:
  - For API 33+: request `POST_NOTIFICATIONS` at runtime.
  - For vibration option: request `VIBRATE` permission in manifest.

### 6) Sound Toggle & Test
- Setting `IsSoundEnabled` stored in app preferences (e.g., `ApplicationData.Current.LocalSettings` or `Preferences` helper).
- Test Sound triggers playing `notification.wav` once (no notification).


## Android Manifest and Permissions
- Edit `Platforms/Android/AndroidManifest.xml`:
  - Add permissions:
    - <uses-permission android:name="android.permission.POST_NOTIFICATIONS" />
    - <uses-permission android:name="android.permission.VIBRATE" />
- Create notification channel at app startup for API 26+ (best practice) in Android-specific initialization.
- Ensure foreground execution is not required for short timers; for reliability in background, consider a Foreground Service for countdown completion (advanced, see below).

### Android-specific initialization points
- `Platforms/Android/MainApplication.cs`:
  - Create the Notification Channel in `OnCreate()` using `NotificationManager`.
  - Initialize any Android-only singletons (e.g., MediaPlayer wrapper).
- `Platforms/Android/MainActivity.cs`:
  - Request runtime permissions (API 33+ `POST_NOTIFICATIONS`).
  - Handle permission result callbacks.
- If implementing a Foreground Service, declare the service in `AndroidManifest.xml` under `<application>` and start/stop it from Android entry points as needed.


## UI Structure (XAML pages; no code in this plan)
- `MainPage` layout:
  - Mode buttons: Pomodoro, Short Break, Long Break.
  - Time display (MM:SS).
  - Controls: Start/Pause, Reset.
  - Tasks section: input + add button; list with checkbox and delete.
  - History section: Show/Hide; session list with stats; expand to show tasks.
  - Sound controls: Test Sound, Sound On/Off, Stop Alarm.
- Use Uno.Toolkit for styling if desired (NavigationBar, styling primitives).


## MVVM & Project Structure (CLI scaffolding)
- Add MVVM Toolkit (already above).
- Suggested structure inside `UnoPomodoro/`:
  - Folders: `ViewModels/`, `Views/`, `Services/`, `Data/` (or separate class library for `Data`).
- CLI to create folders (PowerShell):
  - New-Item -ItemType Directory -Path .\UnoPomodoro\ViewModels, .\UnoPomodoro\Views, .\UnoPomodoro\Services -Force | Out-Null


## Lifecycle & Background Behavior
- Avoid relying solely on per-second ticks in background (may be throttled/suspended).
- Strategy:
  - On start, store absolute end time. UI computes remaining by (EndTime - Now).
  - Use local notification scheduled or posted at completion time from a background handler:
    - Simple approach: when app resumes or timer tick hits zero, post notification.
    - Robust approach: Foreground Service or WorkManager to ensure execution at expected time even if backgrounded.
      - WorkManager: enqueue a `OneTimeWorkRequest` with an initial delay equal to (EndTime - Now). Requires `Xamarin.AndroidX.Work.Runtime`.
- Advanced (optional): implement Android Foreground Service for long timers to guarantee firing exactly at end-time.


## Testing
- Unit tests (Timer logic, Repositories):
  - dotnet new nunit -o UnoPomodoro.Tests
  - dotnet sln add .\UnoPomodoro.Tests\UnoPomodoro.Tests.csproj
  - dotnet add .\UnoPomodoro.Tests\UnoPomodoro.Tests.csproj package FluentAssertions
  - dotnet add .\UnoPomodoro.Tests\UnoPomodoro.Tests.csproj reference .\UnoPomodoro.Data\UnoPomodoro.Data.csproj

- UI Tests (Uno.UITest):
  - Create after app scaffold:
    - mkdir .\UnoPomodoro\UnoPomodoro.UITests
    - cd .\UnoPomodoro\UnoPomodoro.UITests
    - dotnet new unoapp-uitest

- Run tests:
  - dotnet test


## Build & Run (Android)
- From project folder `UnoPomodoro/`:
  - Build: dotnet build -f net8.0-android
  - Deploy & run on default emulator: dotnet build -t:Run -f net8.0-android
  - Deploy to a specific emulator/device (example): dotnet build -t:Install -f net8.0-android /p:AdbTarget=-e

References:
- Android build targets/properties: https://learn.microsoft.com/dotnet/android/building-apps/build-targets


## Data Migration (Optional)
- If you want historic data from `web-old/tasks.db`:
  - Place `tasks.db` into `UnoPomodoro/Assets/Database/tasks.db` (as `AndroidAsset`).
  - On first launch, copy from assets to app data if no DB exists.
  - Ensure schema compatibility with the new app (same columns/types).
  - Consider adding a `schema_version` table to handle future migrations.


## CI/CD (Optional, Android-focused)
- GitHub Actions (self-hosted Windows or Ubuntu with Android SDK):
  - Setup .NET 8/9, `dotnet workload install android`.
  - Cache NuGet and .gradle.
  - Build: dotnet build UnoPomodoro/UnoPomodoro.csproj -f net8.0-android -c Release
  - Sign & bundle:
    - Use `keytool` to generate keystore.
    - Provide signing properties via secrets and MSBuild properties: `AndroidKeyStore`, `AndroidSigningKeyStore`, `AndroidSigningKeyAlias`, `AndroidSigningStorePass`, `AndroidSigningKeyPass`.


## Risks & Considerations
- Background execution: Exact alarm timing in background may require a Foreground Service; otherwise compute-on-resume with notification may suffice.
- Notification permission (API 33+): Request at runtime or degrade gracefully.
- Asset audio compatibility: Ensure `notification.wav` is supported; use `.ogg` if necessary.
- Performance: Use absolute end-time to avoid timer drift; avoid long-lived 1s UI ticks in background.


## Milestones & Work Breakdown (CLI-first)
- 1) Bootstrap project:
  - dotnet new install Uno.Templates
  - dotnet new unoapp -preset blank -platforms android -tfm net8.0 -o UnoPomodoro
  - Configure ApplicationId; verify build runs on emulator.
- 2) Add dependencies and structure:
  - dotnet add package CommunityToolkit.Mvvm
  - dotnet add package sqlite-net-pcl (or Microsoft.Data.Sqlite)
  - dotnet add package Uno.Toolkit.UI (optional)
  - Create folders: ViewModels, Views, Services; add Data class lib if desired.
- 3) Data layer:
  - Implement DB init and repositories; add SQL schema creation; wire into DI (if used).
- 4) Timer & settings:
  - Implement TimerService using absolute end-time; wire to MainPage.
  - Implement sound toggle & test.
- 5) Tasks CRUD:
  - UI bindings + repository operations.
- 6) Sessions & history:
  - Aggregation query for stats; expandable task list per session.
- 7) Notifications & alarm sound:
  - Create notification channel; request POST_NOTIFICATIONS; MediaPlayer loop for sound; Stop button.
- 8) Lifecycle:
  - Handle suspend/resume; recompute remaining; ensure notification posted when finished.
- 9) Testing:
  - dotnet new nunit, dotnet new unoapp-uitest; cover timer/repo logic.
- 10) Packaging:
  - Configure signing; produce Release .apk/.aab.


## Appendix: Useful References
- Uno dotnet new templates: https://platform.uno/docs/articles/get-started-dotnet-new.html
- Uno Wizard options (platforms, flags): https://platform.uno/docs/articles/getting-started/wizard/using-wizard.html
- Using Uno.Sdk (Single Project): https://platform.uno/docs/articles/features/using-the-uno-sdk.html
- Android build properties: https://learn.microsoft.com/dotnet/android/building-apps/build-properties
- Vibration API: https://platform.uno/docs/articles/features/windows-phone-devices-notification-vibrationdevice.html
