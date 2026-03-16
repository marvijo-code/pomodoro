using System;
using Android.App;
using Android.Content;
using UnoPomodoro.Services;

namespace UnoPomodoro.Platforms.Android;

[BroadcastReceiver(Enabled = true, Exported = true, DirectBootAware = true)]
[IntentFilter(new[]
{
    Intent.ActionBootCompleted,
    Intent.ActionLockedBootCompleted,
    Intent.ActionMyPackageReplaced
})]
public class TimerBootReceiver : BroadcastReceiver
{
    public override void OnReceive(Context? context, Intent? intent)
    {
        if (context == null)
        {
            return;
        }

        try
        {
            var settingsService = new SettingsService();
            if (string.IsNullOrWhiteSpace(settingsService.AppRuntimeStateJson))
            {
                return;
            }

            var runtimeState = System.Text.Json.JsonSerializer.Deserialize<AppRuntimeState>(settingsService.AppRuntimeStateJson);
            if (runtimeState == null || !runtimeState.IsRunning || runtimeState.TargetEndUtcTicks <= 0)
            {
                return;
            }

            var soundService = new SoundService();
            var vibrationService = new VibrationService();
            TimerForegroundService.RegisterAlarmServices(
                soundService,
                vibrationService,
                settingsService.IsSoundEnabled,
                settingsService.IsVibrationEnabled,
                settingsService.VibrationDuration);

            var serviceIntent = new Intent(context, typeof(TimerForegroundService));
            serviceIntent.PutExtra("mode", runtimeState.Mode ?? "pomodoro");
            serviceIntent.PutExtra("totalSeconds", Math.Max(1, runtimeState.TotalDurationSeconds));
            serviceIntent.PutExtra("targetEndUtcTicks", runtimeState.TargetEndUtcTicks);

            if (OperatingSystem.IsAndroidVersionAtLeast(26))
            {
                context.StartForegroundService(serviceIntent);
            }
            else
            {
                context.StartService(serviceIntent);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error restoring timer after boot: {ex.Message}");
        }
    }
}
