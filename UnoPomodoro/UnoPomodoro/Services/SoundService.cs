using System;
using System.Threading;
using System.Threading.Tasks;

#if __ANDROID__
using Android.Media;
using Android.Content;
#else
using Windows.Media.Core;
using Windows.Media.Playback;
#endif

namespace UnoPomodoro.Services;

public class SoundService : ISoundService, IDisposable
{
#if __ANDROID__
    private Android.Media.MediaPlayer? _mediaPlayer;
#else
    private MediaPlayer? _mediaPlayer;
#endif
    private double _volume = 1.0;
    private int _duration = 5; // Default 5 seconds
    private CancellationTokenSource? _cancellationTokenSource;

    public double Volume
    {
        get => _volume;
        set
        {
            _volume = Math.Clamp(value, 0.0, 1.0);
#if __ANDROID__
            // Volume for Ringtone is controlled via system settings
            // For MediaPlayer we can set it
            _mediaPlayer?.SetVolume((float)_volume, (float)_volume);
#else
            if (_mediaPlayer != null)
            {
                _mediaPlayer.Volume = _volume;
            }
#endif
        }
    }

    public int Duration
    {
        get => _duration;
        set => _duration = Math.Max(1, value);
    }

    public SoundService()
    {
        InitializeMediaPlayer();
    }

    private void InitializeMediaPlayer()
    {
        try 
        {
#if __ANDROID__
            var context = Android.App.Application.Context;
            
            // Use Android MediaPlayer with system alarm/notification sound
            _mediaPlayer = new Android.Media.MediaPlayer();
            
            // Get default notification sound
            var notificationUri = RingtoneManager.GetDefaultUri(RingtoneType.Notification);
            if (notificationUri == null)
            {
                notificationUri = RingtoneManager.GetDefaultUri(RingtoneType.Alarm);
            }
            
            if (notificationUri != null)
            {
                _mediaPlayer.SetDataSource(context, notificationUri);
                _mediaPlayer.SetAudioAttributes(
                    new AudioAttributes.Builder()
                        ?.SetUsage(AudioUsageKind.Alarm)
                        ?.SetContentType(AudioContentType.Sonification)
                        ?.Build());
                _mediaPlayer.Looping = true;
                _mediaPlayer.Prepare();
                _mediaPlayer.SetVolume((float)_volume, (float)_volume);
            }
            
            System.Diagnostics.Debug.WriteLine($"SoundService initialized with notification URI: {notificationUri}");
#else
            _mediaPlayer = new MediaPlayer();
            _mediaPlayer.Volume = _volume;
            _mediaPlayer.MediaEnded += OnMediaEnded;
            _mediaPlayer.Source = MediaSource.CreateFromUri(new Uri("ms-appx:///Assets/Audio/notification.wav"));
#endif
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error initializing MediaPlayer: {ex.Message}");
        }
    }

#if !__ANDROID__
    private void OnMediaEnded(MediaPlayer sender, object args)
    {
        if (_cancellationTokenSource != null && !_cancellationTokenSource.IsCancellationRequested)
        {
            try 
            {
                _mediaPlayer?.Play();
            }
            catch { /* Ignore */ }
        }
    }
#endif

    public void PlayNotificationSound()
    {
        System.Diagnostics.Debug.WriteLine($"PlayNotificationSound called. Volume={_volume}, Duration={_duration}");
        
#if __ANDROID__
        if (_mediaPlayer == null) 
        {
            System.Diagnostics.Debug.WriteLine("MediaPlayer is null, reinitializing...");
            InitializeMediaPlayer();
        }
        if (_mediaPlayer == null) 
        {
            System.Diagnostics.Debug.WriteLine("MediaPlayer still null after init!");
            return;
        }

        StopNotificationSound();

        try
        {
            _cancellationTokenSource = new CancellationTokenSource();
            var token = _cancellationTokenSource.Token;

            _mediaPlayer.SetVolume((float)_volume, (float)_volume);
            _mediaPlayer.SeekTo(0);
            _mediaPlayer.Start();
            
            System.Diagnostics.Debug.WriteLine($"MediaPlayer started. IsPlaying={_mediaPlayer.IsPlaying}");

            Task.Delay(TimeSpan.FromSeconds(_duration), token).ContinueWith(t =>
            {
                if (!t.IsCanceled)
                {
                    System.Diagnostics.Debug.WriteLine("Duration elapsed, stopping sound...");
                    StopNotificationSound();
                }
            }, TaskScheduler.Default);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error playing sound: {ex.Message}");
        }
#else
        if (_mediaPlayer == null) InitializeMediaPlayer();
        if (_mediaPlayer == null) return;

        StopNotificationSound();

        try
        {
            _cancellationTokenSource = new CancellationTokenSource();
            var token = _cancellationTokenSource.Token;

            _mediaPlayer.Play();

            Task.Delay(TimeSpan.FromSeconds(_duration), token).ContinueWith(t =>
            {
                if (!t.IsCanceled)
                {
                    StopNotificationSound();
                }
            }, TaskScheduler.Default);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error playing sound: {ex.Message}");
        }
#endif
    }

    public void StopNotificationSound()
    {
        _cancellationTokenSource?.Cancel();
        _cancellationTokenSource = null;

#if __ANDROID__
        if (_mediaPlayer != null)
        {
            try 
            {
                if (_mediaPlayer.IsPlaying)
                {
                    _mediaPlayer.Pause();
                }
                _mediaPlayer.SeekTo(0);
            }
            catch (Exception ex)
            { 
                System.Diagnostics.Debug.WriteLine($"Error stopping sound: {ex.Message}");
            }
        }
#else
        if (_mediaPlayer != null)
        {
            try 
            {
                _mediaPlayer.Pause();
                _mediaPlayer.PlaybackSession.Position = TimeSpan.Zero;
            }
            catch { /* Ignore errors when stopping */ }
        }
#endif
    }

    public void Dispose()
    {
        StopNotificationSound();
#if __ANDROID__
        _mediaPlayer?.Release();
        _mediaPlayer = null;
#else
        if (_mediaPlayer != null)
        {
            _mediaPlayer.MediaEnded -= OnMediaEnded;
            _mediaPlayer.Dispose();
            _mediaPlayer = null;
        }
#endif
    }
}
