# Install and run the app on connected device
$adbPath = Join-Path $env:LOCALAPPDATA 'Android\Sdk\platform-tools\adb.exe'
$apkPath = "UnoPomodoro\bin\Debug\net8.0-android\com.marvijocode.pomodoro-Signed.apk"

Write-Host "Checking devices..." -ForegroundColor Cyan
& $adbPath devices

Write-Host "`nInstalling APK..." -ForegroundColor Yellow
& $adbPath install -r $apkPath

Write-Host "`nLaunching app..." -ForegroundColor Yellow
& $adbPath shell monkey -p com.marvijocode.pomodoro -c android.intent.category.LAUNCHER 1

Write-Host "`nApp launched!" -ForegroundColor Green
