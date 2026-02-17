param(
    [string]$Configuration = "Release",
    [string]$Framework = "net10.0-android",
    [string]$AndroidSdk,
    [string]$DeviceId,
    [switch]$SkipInstall,
    [switch]$SkipLaunch
)

$ErrorActionPreference = "Stop"

# 1. Build the project
$repoRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectPath = Join-Path $repoRoot "UnoPomodoro/UnoPomodoro/UnoPomodoro.csproj"

if (-not (Test-Path $projectPath)) {
    throw "Unable to locate project file at $projectPath"
}

# Resolve Android SDK for build
function Resolve-AndroidSdk([string]$Requested) {
    if ($Requested) { return (Resolve-Path $Requested -ErrorAction Stop).Path }
    if ($env:ANDROID_SDK_ROOT) { return (Resolve-Path $env:ANDROID_SDK_ROOT -ErrorAction Stop).Path }
    $fallback = Join-Path $env:LOCALAPPDATA "Android/sdk"
    if (Test-Path $fallback) { return (Resolve-Path $fallback).Path }
    throw "Android SDK directory not found. Pass -AndroidSdk or set ANDROID_SDK_ROOT."
}

$androidSdkPath = Resolve-AndroidSdk $AndroidSdk
$binlog = Join-Path (Split-Path $projectPath -Parent) "uno-android-$Configuration.binlog"

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

# 2. Install and Run
if (-not $SkipInstall) {
    $installScript = Join-Path $repoRoot "install-app.ps1"
    if (-not (Test-Path $installScript)) {
        throw "Install script not found at $installScript"
    }

    $installArgs = @(
        "-Configuration", $Configuration,
        "-Framework", $Framework
    )
    if ($AndroidSdk) { $installArgs += "-AndroidSdk"; $installArgs += $AndroidSdk }
    if ($DeviceId) { $installArgs += "-DeviceId"; $installArgs += $DeviceId }
    if ($SkipLaunch) { $installArgs += "-SkipLaunch" }

    Write-Host "Invoking install-app.ps1..."
    & $installScript @installArgs
} else {
    Write-Host "SkipInstall flag set; build complete." -ForegroundColor Green
}

