#!/bin/bash
EMULATOR="/c/Users/marvi/AppData/Local/Android/Sdk/emulator/emulator.exe"

echo "Listing available AVDs..."
AVD_NAME=$("$EMULATOR" -list-avds | head -n 1)

if [ -z "$AVD_NAME" ]; then
    echo "Error: No AVDs found"
    exit 1
fi

echo "Starting emulator: $AVD_NAME"
"$EMULATOR" -avd "$AVD_NAME" -netdelay none -netspeed full &

echo "Emulator starting in background..."
