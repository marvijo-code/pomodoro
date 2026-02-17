param(
    [string]$Configuration = 'Debug',
    [string]$Framework = 'net10.0-android',
    [string]$PackageId = 'com.marvijocode.pomodoro'
)
$ErrorActionPreference = 'Stop'

$repoRoot = $PSScriptRoot
if (-not $repoRoot) {
    $repoRoot = (Resolve-Path '.').Path
}

function Find-AdbPath {
    $roots = @(
        $env:ANDROID_SDK_ROOT,
        $env:ANDROID_HOME,
        (Join-Path $env:LOCALAPPDATA 'Android\Sdk'),
        'C:\Android',
        'C:\Program Files (x86)\Android\android-sdk',
        'C:\Program Files\Android\android-sdk'
    ) | Where-Object { $_ -and (Test-Path $_) }

    foreach ($r in $roots) {
        $candidate = Join-Path $r 'platform-tools\adb.exe'
        if (Test-Path $candidate) { return (Resolve-Path $candidate).Path }
    }

    $cmd = Get-Command adb.exe -ErrorAction SilentlyContinue
    if ($cmd) { return (Resolve-Path $cmd.Source).Path }

    throw 'adb.exe not found. Install Android SDK Platform-Tools.'
}

$adb = Find-AdbPath
Write-Host ("ADB Path: $adb")
& $adb version | Out-Host

# Locate the most recent APK inside the Uno build output
$apkRoot = Join-Path $repoRoot 'UnoPomodoro/UnoPomodoro/bin'
$apkDir = Join-Path $apkRoot $Configuration
$apkDir = Join-Path $apkDir $Framework
if (!(Test-Path $apkDir)) { throw "APK directory not found: $apkDir. Build the project first." }

$apk = Get-ChildItem -Recurse -Path $apkDir -Filter '*.apk' |
    Sort-Object LastWriteTime -Descending |
    Select-Object -First 1
if (-not $apk) { throw "APK not found under $apkDir. Build the project first." }

Write-Host ("Installing: " + $apk.FullName)
& $adb install -r "$($apk.FullName)" | Out-Host

Write-Host ("Launching package: $PackageId")
# Use monkey to issue a single launch intent
& $adb shell monkey -p $PackageId -c android.intent.category.LAUNCHER 1 | Out-Host

