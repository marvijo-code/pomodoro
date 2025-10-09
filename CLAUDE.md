# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

This is a Pomodoro timer application with multiple implementations:

1. **Uno Platform Implementation** (UnoPomodoro/) - Cross-platform C#/.NET app targeting Android using Uno Platform
2. **React Web Implementation** (web-old/) - Legacy React + Express web app with SQLite backend
3. **React Native Mobile** (web-old/PomodoroMobile/) - Expo-based mobile app

## Common Development Commands

### Uno Platform Implementation (Primary)
```bash
# Navigate to Uno project
cd UnoPomodoro

# Build for Android
dotnet build -f net9.0-android

# Run on Android (requires connected device or emulator)
dotnet build -t:Run -f net9.0-android

# Clean build
dotnet clean
```

### React Web Implementation (Legacy)
```bash
# Navigate to web project
cd web-old

# Install dependencies
npm install

# Start development server with backend
npm start

# Start backend server only
npm run server

# Start frontend only
npm run dev

# Build for production
npm run build

# Run linter
npm run lint
```

### React Native Mobile (Legacy)
```bash
# Navigate to mobile project
cd web-old/PomodoroMobile

# Install dependencies
npm install

# Start development server
expo start

# Start on specific platform
expo start --android
expo start --ios
expo start --web
```

## Architecture

### Uno Platform Implementation
- **Framework**: Uno Platform with WinUI controls
- **Language**: C# with .NET 9
- **Target**: Android (net9.0-android)
- **Database**: SQLite with sqlite-net-pcl
- **Architecture**: MVVM pattern with CommunityToolkit.Mvvm
- **UI**: Material Design with Uno.Material.WinUI

**Key Components:**
- `Services/` - Timer service and audio management
- `Data/` - Repository pattern with SQLite models
- `ViewModels/` - MVVM view models
- `Platforms/` - Platform-specific implementations
- `Assets/Audio/` - Notification sounds

### React Web Implementation
- **Frontend**: React 18 with Vite
- **Backend**: Express.js with SQLite
- **Database**: SQLite3 with tasks and sessions tables
- **Architecture**: Client-server REST API

**API Endpoints:**
- `GET /api/tasks/:sessionId` - Get tasks for session
- `GET /api/sessions` - Get all sessions with task counts
- `POST /api/sessions` - Start new session
- `POST /api/tasks` - Add new task
- `DELETE /api/tasks/:id` - Delete task
- `PUT /api/tasks/:id` - Toggle task completion

## Development Notes

### Uno Platform
- Uses Uno SDK 6.1.23
- Android deployment configured with embedded assemblies
- Audio notifications included as Android assets
- SQLite initialized in App.xaml.cs with proper Android handling

### React Implementation
- Development servers run on ports 8801 (backend) and 3000 (frontend)
- CORS configured for localhost development
- Database file: `tasks.db`
- Server supports concurrent development with `npm start`

## File Structure
```
/code/pomodoro/
├── UnoPomodoro/          # Primary Uno Platform implementation
│   ├── UnoPomodoro/      # Main app project
│   └── UnoPomodoro.Data/ # Data layer project
├── web-old/              # Legacy React implementation
│   ├── PomodoroMobile/   # React Native app
│   ├── src/              # React web app
│   └── server.js         # Express backend
└── scripts/              # Utility scripts
```

## Key Dependencies

### Uno Platform
- Uno.Sdk: 6.1.23
- CommunityToolkit.Mvvm: MVVM framework
- sqlite-net-pcl: SQLite ORM
- Uno.Material.WinUI: Material Design components
- Uno.Toolkit.WinUI: UI toolkit extensions

### React Web
- React 18, Vite 5
- Express 4, SQLite3
- Axios for HTTP requests
- ESLint for code quality