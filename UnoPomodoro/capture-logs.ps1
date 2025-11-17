param(
    [string]$PackageId = 'com.marvijocode.pomodoro',
    [int]$SleepAfterLaunchSeconds = 4
)
$ErrorActionPreference = 'Stop'

function Resolve-Adb {
    $cmd = Get-Command adb -ErrorAction SilentlyContinue
    if ($cmd) { return 'adb' }
    $candidates = @()
    if ($env:ANDROID_HOME) { $candidates += (Join-Path $env:ANDROID_HOME 'platform-tools/adb.exe') }
    if ($env:ANDROID_SDK_ROOT) { $candidates += (Join-Path $env:ANDROID_SDK_ROOT 'platform-tools/adb.exe') }
    $candidates += (Join-Path $env:LOCALAPPDATA 'Android\Sdk\platform-tools\adb.exe')
    foreach ($p in $candidates) { if (Test-Path $p) { return $p } }
    throw 'adb not found. Ensure Android SDK platform-tools are installed.'
}

$adb = Resolve-Adb
& $adb start-server | Out-Null

# pick first device in 'device' state
$deviceId = ((& $adb devices) | ForEach-Object { if ($_ -match '^(\S+)\s+device$') { $matches[1] } } | Select-Object -First 1)
if (-not $deviceId) { throw 'No connected device/emulator in state "device".' }

$logsDir = Join-Path $PSScriptRoot 'logs'
if (-not (Test-Path $logsDir)) { New-Item -ItemType Directory -Path $logsDir | Out-Null }

# Relaunch app and capture logs
& $adb -s $deviceId logcat -c
& $adb -s $deviceId shell am force-stop $PackageId | Out-Null
& $adb -s $deviceId shell monkey -p $PackageId -c android.intent.category.LAUNCHER 1 | Out-Null
Start-Sleep -Seconds $SleepAfterLaunchSeconds

# Save logs
$ts = Get-Date -Format 'yyyyMMdd_HHmmss'
$errorsPath = Join-Path $logsDir ("errors_" + $ts + ".txt")
$logPath    = Join-Path $logsDir ("logcat_" + $ts + ".txt")
$rtPath     = Join-Path $logsDir ("runtime_" + $ts + ".txt")

& $adb -s $deviceId logcat -d -v time *:E | Out-File -FilePath $errorsPath -Encoding utf8
& $adb -s $deviceId logcat -d -v time | Select-Object -Last 1500 | Out-File -FilePath $logPath -Encoding utf8
& $adb -s $deviceId logcat -d -v time | Select-String -Pattern 'AndroidRuntime|FATAL EXCEPTION|mono|dotnet|System\.' | Out-File -FilePath $rtPath -Encoding utf8

Write-Host "Logs written to:`n$errorsPath`n$rtPath`n$logPath" -ForegroundColor Green

# Write a concise summary to a non-ignored file at project root
$summaryPath = Join-Path $PSScriptRoot 'crash-summary.txt'
$errorsTail = Get-Content $errorsPath -Encoding UTF8 | Select-Object -Last 120
$runtimeTail = Get-Content $rtPath -Encoding UTF8 | Select-Object -Last 200
$header = @(
    "==== Crash Summary (" + (Get-Date).ToString('u') + ") ====",
    "Device: $deviceId",
    "Package: $PackageId",
    "Errors tail (last 120 lines):",
    '----------------------------------------'
)
$footer = @(
    '----------------------------------------',
    'Runtime indicators tail (last 200 lines):',
    '----------------------------------------'
)
@($header + $errorsTail + $footer + $runtimeTail) | Out-File -FilePath $summaryPath -Encoding utf8
Write-Host "Summary written to: $summaryPath" -ForegroundColor Green
