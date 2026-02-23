using System;

#if __ANDROID__
using Android.Content;
using Android.Content.PM;
using Android.OS;
using AndroidX.Core.Content;
#endif

namespace UnoPomodoro.Services;

public class VibrationService : IVibrationService
{
#if __ANDROID__
    private readonly Vibrator? _vibrator;
    private readonly Context _context;

    public VibrationService()
    {
        _context = Android.App.Application.Context;

        if (OperatingSystem.IsAndroidVersionAtLeast(31))
        {
            var manager = _context.GetSystemService(Context.VibratorManagerService) as VibratorManager;
            _vibrator = manager?.DefaultVibrator;
        }
        else
        {
            _vibrator = _context.GetSystemService(Context.VibratorService) as Vibrator;
        }
    }

    public bool IsSupported => HasVibratePermission() && (_vibrator?.HasVibrator ?? false);

    private bool HasVibratePermission()
    {
        return ContextCompat.CheckSelfPermission(
            _context,
            global::Android.Manifest.Permission.Vibrate) == Permission.Granted;
    }

    public void Vibrate(int durationMs = 500)
    {
        try
        {
            if (_vibrator == null || !IsSupported) return;

            if (OperatingSystem.IsAndroidVersionAtLeast(26))
            {
                _vibrator.Vibrate(VibrationEffect.CreateOneShot(durationMs, VibrationEffect.DefaultAmplitude));
            }
            else
            {
#pragma warning disable CS0618
                _vibrator.Vibrate(durationMs);
#pragma warning restore CS0618
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Vibration error: {ex.Message}");
        }
    }

    public void VibratePattern(long[] pattern, bool repeat = false)
    {
        try
        {
            if (_vibrator == null || !IsSupported || pattern.Length == 0) return;

            if (OperatingSystem.IsAndroidVersionAtLeast(26))
            {
                _vibrator.Vibrate(VibrationEffect.CreateWaveform(pattern, repeat ? 0 : -1));
            }
            else
            {
#pragma warning disable CS0618
                _vibrator.Vibrate(pattern, repeat ? 0 : -1);
#pragma warning restore CS0618
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Vibration pattern error: {ex.Message}");
        }
    }

    public void Cancel()
    {
        try
        {
            _vibrator?.Cancel();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Cancel vibration error: {ex.Message}");
        }
    }
#else
    public bool IsSupported => false;
    public void Vibrate(int durationMs = 500) { }
    public void VibratePattern(long[] pattern, bool repeat = false) { }
    public void Cancel() { }
#endif
}
