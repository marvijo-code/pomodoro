#!/bin/bash
# Build the Uno Pomodoro Android app in Release mode

echo "Building Uno Pomodoro Android app (Release)..."

# Navigate to the script directory
cd "$(dirname "$0")"

# Build the Android project in Release mode
dotnet build UnoPomodoro/UnoPomodoro.csproj -c Release -f net9.0-android

if [ $? -eq 0 ]; then
    echo "Build succeeded!"
    
    # Locate the built APK
    APK_DIR="UnoPomodoro/bin/Release/net9.0-android"
    APK=$(find "$APK_DIR" -name "*-Signed.apk" -type f 2>/dev/null | head -1)
    
    if [ -n "$APK" ]; then
        SIZE=$(du -h "$APK" | cut -f1)
        echo "Release APK: $APK"
        echo "Size: $SIZE"
    else
        echo "Warning: APK not found in $APK_DIR"
    fi
else
    echo "Build failed. Please check the errors above."
    exit 1
fi
