using Android.App;
using Android.Content;
using Android.OS;
using AndroidX.Core.App;
using System;
using System.Threading;
using System.Threading.Tasks;
using UnoPomodoro.Services;

namespace UnoPomodoro.Platforms.Android;

[Service(
    Name = "com.marvijocode.pomodoro.SignalForegroundService",
    Exported = false,
    ForegroundServiceType = global::Android.Content.PM.ForegroundService.TypeSpecialUse)]
public sealed class SignalForegroundService : Service
{
    private const int MaxSignalDurationMs = 5_000;
    private const int MaxSignalIntervalMs = 5 * 60 * 1_000;
    private const string ChannelId = "signal_loop";
    private const string ChannelName = "Signal Loop";
    private const int NotificationId = 1004;
    private const string StartAction = "com.marvijocode.pomodoro.action.SIGNAL_LOOP_START";
    private const string StopAction = "com.marvijocode.pomodoro.action.SIGNAL_LOOP_STOP";
    private const string ExtraUseSound = "useSound";
    private const string ExtraUseVibration = "useVibration";
    private const string ExtraDurationMs = "durationMs";
    private const string ExtraIntervalMs = "intervalMs";
    private const string ExtraSoundVolumePercent = "soundVolumePercent";

    private CancellationTokenSource? _signalLoopCts;
    private PowerManager.WakeLock? _wakeLock;
    private ISoundService? _soundService;
    private IVibrationService? _vibrationService;
    private bool _useSound;
    private bool _useVibration;
    private int _durationMs = 500;
    private int _intervalMs = 300;
    private double _soundVolumePercent = 100;

    public static bool IsSignalLoopRunning { get; private set; }

    public static void Start(Context context, bool useSound, bool useVibration, int durationMs, int intervalMs, double soundVolumePercent)
    {
        var intent = new Intent(context, typeof(SignalForegroundService));
        intent.SetAction(StartAction);
        intent.PutExtra(ExtraUseSound, useSound);
        intent.PutExtra(ExtraUseVibration, useVibration);
        intent.PutExtra(ExtraDurationMs, durationMs);
        intent.PutExtra(ExtraIntervalMs, intervalMs);
        intent.PutExtra(ExtraSoundVolumePercent, soundVolumePercent);

        if (OperatingSystem.IsAndroidVersionAtLeast(26))
        {
            context.StartForegroundService(intent);
        }
        else
        {
            context.StartService(intent);
        }
    }

    public static void Stop(Context context)
    {
        var intent = new Intent(context, typeof(SignalForegroundService));
        context.StopService(intent);
    }

    public override void OnCreate()
    {
        base.OnCreate();
        _soundService = new SoundService();
        _vibrationService = new VibrationService();
        CreateNotificationChannel();
    }

    public override IBinder? OnBind(Intent? intent)
    {
        return null;
    }

    public override StartCommandResult OnStartCommand(Intent? intent, StartCommandFlags flags, int startId)
    {
        var action = intent?.Action ?? StartAction;

        if (action == StopAction)
        {
            StopSignalLoop();
            StopSelf();
            return StartCommandResult.NotSticky;
        }

        _useSound = intent?.GetBooleanExtra(ExtraUseSound, false) ?? _useSound;
        _useVibration = intent?.GetBooleanExtra(ExtraUseVibration, false) ?? _useVibration;
        _durationMs = Math.Clamp(intent?.GetIntExtra(ExtraDurationMs, _durationMs) ?? _durationMs, 100, MaxSignalDurationMs);
        _intervalMs = Math.Clamp(intent?.GetIntExtra(ExtraIntervalMs, _intervalMs) ?? _intervalMs, 0, MaxSignalIntervalMs);
        _soundVolumePercent = Math.Clamp(intent?.GetDoubleExtra(ExtraSoundVolumePercent, _soundVolumePercent) ?? _soundVolumePercent, 0, 100);

        if (_soundService != null)
        {
            _soundService.Volume = _soundVolumePercent / 100.0;
        }

        AcquireWakeLock();
        StartForegroundNotification();
        StartSignalLoop();

        return StartCommandResult.RedeliverIntent;
    }

    public override void OnDestroy()
    {
        StopSignalLoop();

        if (_soundService is IDisposable disposableSoundService)
        {
            disposableSoundService.Dispose();
        }

        ReleaseWakeLock();

        if (OperatingSystem.IsAndroidVersionAtLeast(24))
        {
            StopForeground(StopForegroundFlags.Remove);
        }
        else
        {
#pragma warning disable CS0618
            StopForeground(true);
#pragma warning restore CS0618
        }

        base.OnDestroy();
    }

    private void StartSignalLoop()
    {
        StopSignalLoopCore();

        if (!_useSound && !_useVibration)
        {
            return;
        }

        var tokenSource = new CancellationTokenSource();
        _signalLoopCts = tokenSource;
        IsSignalLoopRunning = true;
        _ = RunSignalLoopAsync(tokenSource);
    }

    private async Task RunSignalLoopAsync(CancellationTokenSource tokenSource)
    {
        var token = tokenSource.Token;

        try
        {
            while (!token.IsCancellationRequested)
            {
                if (_useSound)
                {
                    _soundService?.PlayNotificationSound(_durationMs);
                }

                if (_useVibration && _vibrationService?.IsSupported == true)
                {
                    _vibrationService.Vibrate(_durationMs);
                }

                await Task.Delay(_durationMs, token);

                if (_intervalMs > 0)
                {
                    await Task.Delay(_intervalMs, token);
                }
            }
        }
        catch (TaskCanceledException)
        {
            // expected when stopping the loop
        }
        finally
        {
            if (ReferenceEquals(_signalLoopCts, tokenSource))
            {
                _signalLoopCts = null;
                IsSignalLoopRunning = false;
            }
        }
    }

    private void StopSignalLoop()
    {
        StopSignalLoopCore();
        ReleaseWakeLock();
    }

    private void StopSignalLoopCore()
    {
        var tokenSource = _signalLoopCts;
        _signalLoopCts = null;

        if (tokenSource != null)
        {
            tokenSource.Cancel();
            tokenSource.Dispose();
        }

        _soundService?.StopNotificationSound();
        _vibrationService?.Cancel();
        IsSignalLoopRunning = false;
    }

    private void StartForegroundNotification()
    {
        var notification = new NotificationCompat.Builder(this, ChannelId)
            .SetContentTitle("Signal running")
            .SetContentText(BuildNotificationText())
            .SetSmallIcon(global::Android.Resource.Drawable.IcLockIdleAlarm)
            .SetOngoing(true)
            .SetPriority(NotificationCompat.PriorityLow)
            .SetVisibility((int)NotificationVisibility.Public)
            .AddAction(global::Android.Resource.Drawable.IcMediaPause, "Stop", CreateStopPendingIntent())
            .Build();

        if (OperatingSystem.IsAndroidVersionAtLeast(34))
        {
            StartForeground(NotificationId, notification, global::Android.Content.PM.ForegroundService.TypeSpecialUse);
        }
        else
        {
            StartForeground(NotificationId, notification);
        }
    }

    private string BuildNotificationText()
    {
        var signalKind = _useSound && _useVibration
            ? "sound and vibration"
            : _useSound
                ? "sound"
                : "vibration";

        return $"Repeats {signalKind} for {FormatMilliseconds(_durationMs)} with {FormatMilliseconds(_intervalMs)} gap.";
    }

    private static string FormatMilliseconds(int milliseconds)
    {
        if (milliseconds < 1000)
        {
            return $"{milliseconds} ms";
        }

        var duration = TimeSpan.FromMilliseconds(milliseconds);
        if (duration.TotalMinutes >= 1)
        {
            if (duration.Seconds == 0 && duration.Milliseconds == 0)
            {
                return $"{(int)duration.TotalMinutes} min";
            }

            if (duration.Milliseconds == 0)
            {
                return $"{(int)duration.TotalMinutes} min {duration.Seconds} s";
            }

            return $"{duration.TotalMinutes:0.#} min";
        }

        return milliseconds % 1000 == 0
            ? $"{duration.TotalSeconds:0} s"
            : $"{duration.TotalSeconds:0.#} s";
    }

    private PendingIntent CreateStopPendingIntent()
    {
        var stopIntent = new Intent(this, typeof(SignalForegroundService));
        stopIntent.SetAction(StopAction);

        var flags = PendingIntentFlags.UpdateCurrent;
        if (OperatingSystem.IsAndroidVersionAtLeast(23))
        {
            flags |= PendingIntentFlags.Immutable;
        }

        return PendingIntent.GetService(this, NotificationId + 1, stopIntent, flags)!;
    }

    private void CreateNotificationChannel()
    {
        if (!OperatingSystem.IsAndroidVersionAtLeast(26))
        {
            return;
        }

        var notificationManager = GetSystemService(NotificationService) as NotificationManager;
        var channel = new NotificationChannel(ChannelId, ChannelName, NotificationImportance.Low)
        {
            Description = "Runs repeating vibration or sound signals while the app is in the background"
        };
        channel.SetShowBadge(false);
        channel.LockscreenVisibility = NotificationVisibility.Public;
        notificationManager?.CreateNotificationChannel(channel);
    }

    private void AcquireWakeLock()
    {
        if (_wakeLock == null)
        {
            var powerManager = GetSystemService(PowerService) as PowerManager;
            var wakeLock = powerManager?.NewWakeLock(WakeLockFlags.Partial, "UnoPomodoro::SignalWakeLock");
            if (wakeLock != null)
            {
                wakeLock.SetReferenceCounted(false);
                _wakeLock = wakeLock;
            }
        }

        if (_wakeLock != null && !_wakeLock.IsHeld)
        {
            _wakeLock.Acquire();
        }
    }

    private void ReleaseWakeLock()
    {
        if (_wakeLock == null || !_wakeLock.IsHeld)
        {
            return;
        }

        try
        {
            _wakeLock.Release();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error releasing signal wake lock: {ex.Message}");
        }
    }
}
