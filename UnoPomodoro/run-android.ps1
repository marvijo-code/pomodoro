# PowerShell script to build and run the Uno Pomodoro Android app

Write-Host "Building and running Uno Pomodoro Android app..." -ForegroundColor Green

# Navigate to the script directory
Set-Location -Path $PSScriptRoot

# Resolve the main Android project file
$projectPath = Join-Path $PSScriptRoot 'UnoPomodoro/UnoPomodoro.csproj'
if (-not (Test-Path $projectPath)) {
    Write-Host "Error: Project file not found at $projectPath" -ForegroundColor Red
    exit 1
}

# Build the Android project
dotnet build $projectPath -f net9.0-android

# Check if build succeeded
if ($LASTEXITCODE -eq 0) {
    Write-Host "Build succeeded!" -ForegroundColor Green
    
    # Locate the built APK
    $apkDir = Join-Path $PSScriptRoot 'UnoPomodoro/bin/Debug/net9.0-android'
    $apk = Get-ChildItem -Path $apkDir -Filter '*.apk' -Recurse -ErrorAction SilentlyContinue | Sort-Object LastWriteTime -Descending | Select-Object -First 1
    if (-not $apk) {
        Write-Host "Error: APK not found in $apkDir" -ForegroundColor Red
        exit 1
    }
    Write-Host "Found APK: $($apk.FullName)" -ForegroundColor Cyan

    # Ensure adb is available (try PATH, ANDROID_HOME, ANDROID_SDK_ROOT, and default LocalAppData)
    $adbCmd = $null
    try {
        adb version | Out-Null
        $adbCmd = 'adb'
    } catch {
        $candidates = @()
        if ($env:ANDROID_HOME) { $candidates += Join-Path $env:ANDROID_HOME 'platform-tools/adb.exe' }
        if ($env:ANDROID_SDK_ROOT) { $candidates += Join-Path $env:ANDROID_SDK_ROOT 'platform-tools/adb.exe' }
        $defaultSdk = Join-Path $env:LOCALAPPDATA 'Android\Sdk\platform-tools\adb.exe'
        $candidates += $defaultSdk
        $adbCmd = ($candidates | Where-Object { Test-Path $_ } | Select-Object -First 1)
        if (-not $adbCmd) {
            Write-Host "Error: 'adb' not found. Checked PATH, ANDROID_HOME, ANDROID_SDK_ROOT, and $defaultSdk" -ForegroundColor Red
            exit 1
        }
    }

    # Ensure a device/emulator is connected and ready
    $devices = (& $adbCmd devices) |
        ForEach-Object { if ($_ -match '^(\S+)\s+device$') { $matches[1] } } |
        Where-Object { $_ } |
        Select-Object -Unique
    if (-not $devices -or $devices.Count -eq 0) {
        Write-Host "No device detected. Attempting to start an Android emulator..." -ForegroundColor Yellow

        # Locate emulator executable
        $emulatorCmd = $null
        $emuCandidates = @()
        if ($env:ANDROID_HOME) { $emuCandidates += Join-Path $env:ANDROID_HOME 'emulator\emulator.exe' }
        if ($env:ANDROID_SDK_ROOT) { $emuCandidates += Join-Path $env:ANDROID_SDK_ROOT 'emulator\emulator.exe' }
        $defaultEmu = Join-Path $env:LOCALAPPDATA 'Android\Sdk\emulator\emulator.exe'
        $emuCandidates += $defaultEmu
        $emulatorCmd = ($emuCandidates | Where-Object { Test-Path $_ } | Select-Object -First 1)
        if (-not $emulatorCmd) {
            Write-Host "Error: Android Emulator not found. Checked ANDROID_HOME, ANDROID_SDK_ROOT, and $defaultEmu" -ForegroundColor Red
            exit 1
        }

        # List available AVDs
        $avds = & $emulatorCmd -list-avds | Where-Object { $_ -and $_.Trim() -ne '' }
        if (-not $avds -or $avds.Count -eq 0) {
            Write-Host "Error: No Android Virtual Devices (AVDs) found. Create one with Android Studio's Device Manager." -ForegroundColor Red
            exit 1
        }
        $avdName = $avds[0]
        Write-Host "Starting emulator: $avdName" -ForegroundColor Cyan

        # Start emulator (non-blocking)
        Start-Process -FilePath $emulatorCmd -ArgumentList @('-avd', $avdName, '-netdelay', 'none', '-netspeed', 'full') | Out-Null

        # Ensure adb server is running
        & $adbCmd start-server | Out-Null

        # Wait for device to appear
        Write-Host "Waiting for emulator to come online..." -ForegroundColor Yellow
        & $adbCmd wait-for-device

        # Wait for boot completion with timeout
        $maxWaitSeconds = 300
        $elapsed = 0
        while ($true) {
            try {
                $boot = (& $adbCmd shell getprop sys.boot_completed).Trim()
            } catch {
                $boot = ''
            }
            if ($boot -eq '1') { break }
            Start-Sleep -Seconds 5
            $elapsed += 5
            if ($elapsed -ge $maxWaitSeconds) {
                Write-Host "Error: Emulator did not boot within $maxWaitSeconds seconds." -ForegroundColor Red
                exit 1
            }
        }
        Write-Host "Emulator boot completed." -ForegroundColor Green

        # Refresh device list (only entries with "device" status)
        $devices = (& $adbCmd devices) |
            ForEach-Object { if ($_ -match '^(\S+)\s+device$') { $matches[1] } } |
            Where-Object { $_ } |
            Select-Object -Unique
        if (-not $devices -or $devices.Count -eq 0) {
            Write-Host "Error: Emulator failed to register as a device." -ForegroundColor Red
            exit 1
        }
    }
    # Wait until selected device is online/ready
    $deviceId = $devices[0]
    Write-Host "Using device: $deviceId" -ForegroundColor Cyan
    & $adbCmd -s $deviceId wait-for-device | Out-Null
    $state = (& $adbCmd -s $deviceId get-state 2>$null).Trim()
    $tries = 0
    while ($state -ne 'device' -and $tries -lt 60) {
        Start-Sleep -Seconds 2
        $state = (& $adbCmd -s $deviceId get-state 2>$null).Trim()
        $tries++
    }
    # Also confirm boot completion
    $elapsed = 0
    while ($true) {
        try { $boot = (& $adbCmd -s $deviceId shell getprop sys.boot_completed).Trim() } catch { $boot = '' }
        if ($boot -eq '1') { break }
        Start-Sleep -Seconds 2
        $elapsed += 2
        if ($elapsed -ge 120) { break }
    }

    # Install APK
    Write-Host "Installing APK..." -ForegroundColor Yellow
    # Always do a clean standard install (no fastdeploy)
    & $adbCmd -s $deviceId uninstall com.example.unopomodoro | Out-Null
    & $adbCmd -s $deviceId install -r "$($apk.FullName)"
    if ($LASTEXITCODE -ne 0) {
        Write-Host "APK install failed." -ForegroundColor Red
        exit $LASTEXITCODE
    }

    # Launch the app using package id from csproj
    $packageId = 'com.example.unopomodoro'
    Write-Host "Launching $packageId ..." -ForegroundColor Yellow
    & $adbCmd -s $deviceId shell monkey -p $packageId -c android.intent.category.LAUNCHER 1 | Out-Null
    Write-Host "App launched." -ForegroundColor Green
} else {
    Write-Host "Build failed. Please check the errors above." -ForegroundColor Red
    exit 1
}
