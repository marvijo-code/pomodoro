param()
$ErrorActionPreference = 'Stop'

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
& $adb version
& $adb devices
