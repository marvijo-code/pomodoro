namespace UnoPomodoro.Services;

public interface IVibrationService
{
    /// <summary>
    /// Vibrates the device for the specified duration in milliseconds.
    /// </summary>
    void Vibrate(int durationMs = 500);

    /// <summary>
    /// Vibrates the device with a pattern.
    /// Pattern is an array of durations in ms: [pause, vibrate, pause, vibrate, ...]
    /// </summary>
    void VibratePattern(long[] pattern, bool repeat = false);

    /// <summary>
    /// Cancels any ongoing vibration.
    /// </summary>
    void Cancel();

    /// <summary>
    /// Whether vibration is supported on this device.
    /// </summary>
    bool IsSupported { get; }
}
