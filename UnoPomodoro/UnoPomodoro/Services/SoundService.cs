using System;
using System.Threading.Tasks;
using Windows.Media.Core;
using Windows.Media.Playback;
using Microsoft.UI.Xaml;

namespace UnoPomodoro.Services;

public class SoundService : ISoundService, IDisposable
{
    private MediaPlayer? _mediaPlayer;
    private bool _isPlaying = false;
    private int _repeatCount = 0;
    private const int MAX_REPEATS = 3;

    public SoundService()
    {
        // Initialize MediaPlayer
        _mediaPlayer = new MediaPlayer();
        _mediaPlayer.MediaEnded += OnMediaEnded;
    }

    public void PlayNotificationSound()
    {
        if (_isPlaying || _mediaPlayer == null) return;

        try
        {
            // Reset repeat counter and start playing
            _repeatCount = 0;
            PlaySound();
        }
        catch (Exception ex)
        {
            // Handle any playback errors
            System.Diagnostics.Debug.WriteLine($"Error playing sound: {ex.Message}");
        }
    }

    private void PlaySound()
    {
        if (_mediaPlayer == null) return;

        // Load the notification sound from assets
        _mediaPlayer.Source = MediaSource.CreateFromUri(new Uri("ms-appx:///Assets/Audio/notification.wav"));
        _mediaPlayer.Play();
        _isPlaying = true;
    }

    public void StopNotificationSound()
    {
        if (_mediaPlayer != null && _isPlaying)
        {
            _mediaPlayer.Pause();
            _isPlaying = false;
            _repeatCount = 0;
        }
    }

    private void OnMediaEnded(MediaPlayer sender, object args)
    {
        _repeatCount++;

        if (_repeatCount < MAX_REPEATS)
        {
            // Play the sound again
            PlaySound();
        }
        else
        {
            // All repeats completed
            _isPlaying = false;
            _repeatCount = 0;
        }
    }

    public void Dispose()
    {
        if (_mediaPlayer != null)
        {
            _mediaPlayer.MediaEnded -= OnMediaEnded;
            _mediaPlayer.Dispose();
            _mediaPlayer = null!; // null-forgiving operator to suppress warning
        }
    }
}
