# PowerShell script to build the Uno Pomodoro Android app in Release mode

Write-Host "Building Uno Pomodoro Android app (Release)..." -ForegroundColor Green

# Navigate to the script directory
Set-Location -Path $PSScriptRoot

# Build the Android project in Release mode
dotnet build UnoPomodoro/UnoPomodoro.csproj -c Release -f net9.0-android

if ($LASTEXITCODE -eq 0) {
    Write-Host "Build succeeded!" -ForegroundColor Green
    
    # Locate the built APK
    $apkDir = Join-Path $PSScriptRoot 'UnoPomodoro/bin/Release/net9.0-android'
    $apk = Get-ChildItem -Path $apkDir -Filter '*-Signed.apk' -ErrorAction SilentlyContinue | Sort-Object LastWriteTime -Descending | Select-Object -First 1
    
    if ($apk) {
        Write-Host "Release APK: $($apk.FullName)" -ForegroundColor Cyan
        Write-Host "Size: $([math]::Round($apk.Length / 1MB, 2)) MB" -ForegroundColor Cyan
    } else {
        Write-Host "Warning: APK not found in $apkDir" -ForegroundColor Yellow
    }
} else {
    Write-Host "Build failed. Please check the errors above." -ForegroundColor Red
    exit 1
}
