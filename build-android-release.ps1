param(
    [string]$Configuration = "Release",
    [string]$Framework = "net9.0-android",
    [string]$AndroidSdk,
    [string]$DeviceId,
    [switch]$SkipInstall,
    [switch]$SkipLaunch
)

$ErrorActionPreference = "Stop"
$packageId = "com.marvijocode.pomodoro"

function Resolve-AndroidSdk([string]$Requested) {
    if ($Requested) { return (Resolve-Path $Requested -ErrorAction Stop).Path }
    if ($env:ANDROID_SDK_ROOT) { return (Resolve-Path $env:ANDROID_SDK_ROOT -ErrorAction Stop).Path }
    $fallback = Join-Path $env:LOCALAPPDATA "Android/sdk"
    if (Test-Path $fallback) { return (Resolve-Path $fallback).Path }
    throw "Android SDK directory not found. Pass -AndroidSdk or set ANDROID_SDK_ROOT."
}

function Resolve-Adb([string]$SdkRoot) {
    $candidates = @()
    $cmd = Get-Command adb.exe -ErrorAction SilentlyContinue
    if ($cmd) { $candidates += $cmd.Source }
    if ($SdkRoot) { $candidates += (Join-Path $SdkRoot 'platform-tools/adb.exe') }
    if ($env:ANDROID_HOME) { $candidates += (Join-Path $env:ANDROID_HOME 'platform-tools/adb.exe') }
    $candidates += (Join-Path $env:LOCALAPPDATA 'Android/Sdk/platform-tools/adb.exe')
    foreach ($c in $candidates | Where-Object { $_ }) {
        if (Test-Path $c) { return (Resolve-Path $c).Path }
    }
    throw "adb.exe not found. Install Platform-Tools or add adb to PATH."
}

function Resolve-Emulator([string]$SdkRoot) {
    $candidates = @()
    if ($env:ANDROID_HOME) { $candidates += (Join-Path $env:ANDROID_HOME 'emulator/emulator.exe') }
    if ($SdkRoot) { $candidates += (Join-Path $SdkRoot 'emulator/emulator.exe') }
    if ($env:ANDROID_SDK_ROOT) { $candidates += (Join-Path $env:ANDROID_SDK_ROOT 'emulator/emulator.exe') }
    $candidates += (Join-Path $env:LOCALAPPDATA 'Android/Sdk/emulator/emulator.exe')
    foreach ($c in $candidates | Where-Object { $_ }) {
        if (Test-Path $c) { return (Resolve-Path $c).Path }
    }
    return $null
}

function Get-ConnectedDevices([string]$AdbPath) {
    return (& $AdbPath devices) |
        ForEach-Object { if ($_ -match '^(\S+)\s+device$') { $matches[1] } } |
        Where-Object { $_ } |
        Select-Object -Unique
}

function Wait-ForDeviceReady([string]$AdbPath, [string]$DeviceId, [int]$TimeoutSeconds = 300) {
    if (-not $DeviceId) {
        throw "Device id is empty while waiting for readiness."
    }

    $waited = 0
    while ($waited -lt $TimeoutSeconds) {
        $stateOutput = & $AdbPath -s $DeviceId get-state 2>$null
        $state = if ($stateOutput) { $stateOutput.Trim() } else { '' }
        if ($state -eq 'device') {
            try {
                $bootOutput = & $AdbPath -s $DeviceId shell getprop sys.boot_completed
                $boot = if ($bootOutput) { $bootOutput.Trim() } else { '' }
            } catch {
                $boot = ''
            }
            if ($boot -eq '1') { return }
        }
        Start-Sleep -Seconds 3
        $waited += 3
    }
    throw "Device '$DeviceId' not ready after $TimeoutSeconds seconds."
}

function Ensure-Device([string]$AdbPath, [string]$SdkRoot, [string]$DesiredId) {
    $devices = Get-ConnectedDevices $AdbPath
    if ($DesiredId) {
        if ($devices -notcontains $DesiredId) {
            throw "Device '$DesiredId' not detected. Connected devices: $($devices -join ', ')"
        }
        Wait-ForDeviceReady $AdbPath $DesiredId 180
        return $DesiredId
    }
    if ($devices -and $devices.Count -gt 0) {
        $first = $devices[0]
        Wait-ForDeviceReady $AdbPath $first 180
        return $first
    }

    $emulator = Resolve-Emulator $SdkRoot
    if (-not $emulator) {
        throw "No connected devices found and emulator.exe not available. Connect a device or install the Android emulator."
    }
    $avds = @(& $emulator -list-avds | Where-Object { $_ -and $_.Trim() -ne '' })
    if (-not $avds -or $avds.Count -eq 0) {
        throw "No Android Virtual Devices found. Create one with Android Studio's Device Manager."
    }
    $avdName = $avds[0].Trim()
    Write-Host "Starting emulator '$avdName'..." -ForegroundColor Cyan
    Start-Process -FilePath $emulator -ArgumentList @('-avd', $avdName, '-netdelay', 'none', '-netspeed', 'full') | Out-Null
    & $AdbPath start-server | Out-Null
    Write-Host "Waiting for emulator to connect..." -ForegroundColor Yellow
    & $AdbPath wait-for-device | Out-Null

    $deadline = (Get-Date).AddMinutes(5)
    do {
        Start-Sleep -Seconds 5
        $devices = Get-ConnectedDevices $AdbPath
        $connected = $devices | Select-Object -First 1
    } while (-not $connected -and (Get-Date) -lt $deadline)

    if (-not $connected) {
        throw "Emulator failed to appear within 5 minutes."
    }

    Wait-ForDeviceReady $AdbPath $connected 300
    return $connected
}

function Install-And-Launch([string]$AdbPath, [string]$DeviceId, [string]$ApkPath, [string]$PackageId, [switch]$SkipLaunch) {
    if (-not (Test-Path $ApkPath)) {
        throw "APK not found at $ApkPath"
    }

    Write-Host "Installing $ApkPath on $DeviceId..." -ForegroundColor Yellow
    try {
        & $AdbPath -s $DeviceId uninstall $PackageId | Out-Null
    } catch {
        Write-Verbose "Uninstall warning: $_"
    }

    & $AdbPath -s $DeviceId install -r "$ApkPath"
    if ($LASTEXITCODE -ne 0) {
        throw "adb install failed with exit code $LASTEXITCODE"
    }
    Write-Host "APK installed." -ForegroundColor Green

    if (-not $SkipLaunch) {
        Write-Host "Launching $PackageId..." -ForegroundColor Yellow
        & $AdbPath -s $DeviceId shell monkey -p $PackageId -c android.intent.category.LAUNCHER 1 | Out-Null
        Write-Host "App launched." -ForegroundColor Green
    }
}

$repoRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectPath = Join-Path $repoRoot "UnoPomodoro/UnoPomodoro/UnoPomodoro.csproj"
if (-not (Test-Path $projectPath)) {
    throw "Unable to locate project file at $projectPath"
}

$androidSdkPath = Resolve-AndroidSdk $AndroidSdk
$binlog = Join-Path (Split-Path $projectPath -Parent) "uno-android-release.binlog"

$arguments = @(
    "publish",
    $projectPath,
    "-c", $Configuration,
    "-f", $Framework,
    "-p:AndroidSdkDirectory=$androidSdkPath",
    "-bl:$binlog"
)

Write-Host "Publishing Uno Android app..."
Write-Host "Project: $projectPath"
Write-Host "Configuration: $Configuration"
Write-Host "Framework: $Framework"
Write-Host "Android SDK: $androidSdkPath"
Write-Host "Binlog: $binlog"

& dotnet @arguments
if ($LASTEXITCODE -ne 0) {
    throw "dotnet publish failed with exit code $LASTEXITCODE"
}

$outDir = Join-Path (Split-Path $projectPath -Parent) "bin/$Configuration/$Framework"
$apkPath = Join-Path $outDir "${packageId}-Signed.apk"
$aabPath = Join-Path $outDir "${packageId}-Signed.aab"

Write-Host "Build output directory: $outDir"
if (Test-Path $apkPath) {
    Write-Host "APK: $apkPath"
} else {
    Write-Warning "APK not found at $apkPath; check build output for details."
}

if (Test-Path $aabPath) {
    Write-Host "AAB: $aabPath"
}

if (-not $SkipInstall) {
    $adbPath = Resolve-Adb $androidSdkPath
    Write-Host "ADB Path: $adbPath"
    $device = Ensure-Device $adbPath $androidSdkPath $DeviceId
    Write-Host "Using device: $device" -ForegroundColor Cyan
    Install-And-Launch -AdbPath $adbPath -DeviceId $device -ApkPath $apkPath -PackageId $packageId -SkipLaunch:$SkipLaunch
} else {
    Write-Host "SkipInstall flag set; not deploying APK." -ForegroundColor Yellow
}

Write-Host "Done." -ForegroundColor Green
