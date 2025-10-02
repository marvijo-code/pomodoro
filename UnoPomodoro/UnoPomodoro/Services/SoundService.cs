using System;
using System.Threading.Tasks;
using Windows.Media.Core;
using Windows.Media.Playback;
using Microsoft.UI.Xaml;

namespace UnoPomodoro.Services;

public class SoundService : IDisposable
{
    private MediaPlayer? _mediaPlayer;
    private bool _isPlaying = false;

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
            // Load the notification sound from assets
            _mediaPlayer.Source = MediaSource.CreateFromUri(new Uri("ms-appx:///Assets/Audio/notification.wav"));
            _mediaPlayer.Play();
            _isPlaying = true;
        }
        catch (Exception ex)
        {
            // Handle any playback errors
            System.Diagnostics.Debug.WriteLine($"Error playing sound: {ex.Message}");
        }
    }

    public void StopNotificationSound()
    {
        if (_mediaPlayer != null && _isPlaying)
        {
            _mediaPlayer.Pause();
            _isPlaying = false;
        }
    }

    private void OnMediaEnded(MediaPlayer sender, object args)
    {
        _isPlaying = false;
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
