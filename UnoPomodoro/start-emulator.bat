@echo off
set EMULATOR="C:\Users\marvi\AppData\Local\Android\Sdk\emulator\emulator.exe"

echo Listing available AVDs...
%EMULATOR% -list-avds > avds.txt

set /p AVD_NAME=<avds.txt
echo Starting emulator: %AVD_NAME%

start "" %EMULATOR% -avd %AVD_NAME% -netdelay none -netspeed full

echo Emulator starting in background...
del avds.txt
