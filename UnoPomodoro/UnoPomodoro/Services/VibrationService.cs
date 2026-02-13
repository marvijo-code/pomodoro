using System;

#if __ANDROID__
using Android.Content;
using Android.OS;
#endif

namespace UnoPomodoro.Services;

public interface IVibrationService
{
    void Vibrate(int durationMs = 500);
    void VibratePattern(long[] pattern, bool repeat = false);
    void Cancel();
    bool IsSupported { get; }
}

public class VibrationService : IVibrationService
{
#if __ANDROID__
    private readonly Vibrator? _vibrator;

    public VibrationService()
    {
        var context = Android.App.Application.Context;
        _vibrator = context.GetSystemService(Context.VibratorService) as Vibrator;
    }

    public bool IsSupported => _vibrator?.HasVibrator ?? false;

    public void Vibrate(int durationMs = 500)
    {
        try
        {
            if (_vibrator == null || !IsSupported) return;

            if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
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
            if (_vibrator == null || !IsSupported) return;

            if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
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
