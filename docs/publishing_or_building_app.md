# UnoPomodoro Android Release Build Guide

This guide records the steps required to produce a trimmed Uno Android release build and describes the helper script that automates the process.

## Prerequisites

- .NET 9 SDK (matches `global.json`).
- Microsoft OpenJDK 17 (the Android `sdkmanager` requires classfile version 61+).
- Android SDK Platform/Tools 35 installed into a location you can write to (we recommend `%LOCALAPPDATA%\Android\sdk`).
- The repo cloned and a shell running from `c:\dev\pomodoro`.

> If you store the SDK under `%LOCALAPPDATA%\Android\sdk`, consider setting `ANDROID_SDK_ROOT` to that path for convenience.

## Installing Android Platform 35 (one-time)

1. Ensure Java 17 is on `PATH` (or invoke `sdkmanager` within a session where `JAVA_HOME` points to your JDK 17 install).
2. Run `"C:/Program Files (x86)/Android/android-sdk/cmdline-tools/12.0/bin/sdkmanager.bat" --sdk_root="%LOCALAPPDATA%/Android/sdk" --install "platforms;android-35"`.
   - Add `platform-tools` and the latest `build-tools` if they are not already present.
3. Verify `platforms/android-35/android.jar` exists under your SDK root.

This keeps the SDK under your user profile so you do not need elevated permissions and `dotnet publish` can consume it via `-p:AndroidSdkDirectory`.

## Release Build Script

Use `build-android-release.ps1` (located at the repo root) to produce the signed Release artifacts **and** deploy/launch them on a connected device or emulator.

```
powershell -ExecutionPolicy Bypass -File .\build-android-release.ps1 [-Configuration Release] [-Framework net9.0-android] [-AndroidSdk <path>] [-DeviceId <adb-id>] [-SkipInstall] [-SkipLaunch]
```

- Defaults to `Release`/`net9.0-android` and uses, in order, the `-AndroidSdk` parameter, `ANDROID_SDK_ROOT`, or `%LOCALAPPDATA%\Android\sdk`.
- Emits a binary MSBuild log at `UnoPomodoro/uno-android-release.binlog` for troubleshooting.
- Automatically resolves `adb`, picks the requested device (or the first connected one), and will attempt to boot the first available AVD if no hardware is attached.
- Installs the freshly built APK and launches `com.marvijocode.pomodoro` unless you pass `-SkipInstall`/`-SkipLaunch`.
- Prints the locations of the generated `.apk` and `.aab` files, e.g. `UnoPomodoro/UnoPomodoro/bin/Release/net9.0-android/com.marvijocode.pomodoro-Signed.apk` (~21 MB after the linker/arm64-only changes).

## Expected Output & Verification

1. Run the script. The publish step should finish with warnings only (e.g., CA14xx platform warnings from `MainActivity.Android.cs`).
2. Let the script install + launch the APK on the selected device (or pass `-SkipInstall`/`-SkipLaunch` if you only need the artifacts).
3. Confirm both `.apk` and `.aab` exist in `UnoPomodoro/UnoPomodoro/bin/Release/net9.0-android/` for sideloading or store upload.

## Troubleshooting

- **Android SDK not found**: Pass `-AndroidSdk <path>` or set `ANDROID_SDK_ROOT`. Ensure the path contains `platforms/android-35`.
- **sdkmanager Java error**: Install/point to JDK 17. Older JDK11 builds cannot load the CLI (classfile version mismatch).
- **Permissions denied under Program Files**: Install/clone the SDK under `%LOCALAPPDATA%` or run the CLI elevated (recommended: move to user directory as described above).
- **Large APK**: Ensure you run the Release configuration. Debug builds embed every ABI + symbol and exceed 300 MB by design. Release builds now use assembly store + `SdkOnly` linking and target only `android-arm64`.

For deeper investigation, open the binlog with MSBuild Structured Log Viewer (`https://msbuildlog.com/`) and inspect the `_CreateAndroidApk` target output.
