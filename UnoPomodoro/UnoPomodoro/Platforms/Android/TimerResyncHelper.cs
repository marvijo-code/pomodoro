using UnoPomodoro.Services;

namespace UnoPomodoro.Droid;

/// <summary>
/// Helper class to allow Android lifecycle methods to resync the timer.
/// This helps handle power saving mode scenarios where timer ticks may be delayed.
/// </summary>
public static class TimerResyncHelper
{
    private static TimerService? _timerService;
    
    /// <summary>
    /// Registers the timer service for resyncing from Android lifecycle events.
    /// </summary>
    public static void Register(TimerService timerService)
    {
        _timerService = timerService;
    }
    
    /// <summary>
    /// Unregisters the timer service.
    /// </summary>
    public static void Unregister()
    {
        _timerService = null;
    }
    
    /// <summary>
    /// Syncs the timer with the wall clock. Call this when the app resumes.
    /// </summary>
    public static void SyncTimer()
    {
        try
        {
            _timerService?.SyncWithWallClock();
        }
        catch (System.Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error syncing timer: {ex.Message}");
        }
    }
}
