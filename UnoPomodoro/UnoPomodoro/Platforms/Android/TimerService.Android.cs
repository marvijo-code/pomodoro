using Android.Content;
using UnoPomodoro.Platforms.Android;
using UnoPomodoro.Droid;

namespace UnoPomodoro.Services
{
    public partial class TimerService
    {
        partial void StartPlatformBackgroundService()
        {
            // Register this instance for lifecycle sync
            TimerResyncHelper.Register(this);
            
            var context = Android.App.Application.Context;
            var intent = new Intent(context, typeof(TimerForegroundService));
            intent.PutExtra("mode", CurrentMode);
            intent.PutExtra("totalSeconds", TotalDurationSeconds);

            if (OperatingSystem.IsAndroidVersionAtLeast(26))
            {
                context.StartForegroundService(intent);
            }
            else
            {
                context.StartService(intent);
            }
        }

        partial void StopPlatformBackgroundService()
        {
            var context = Android.App.Application.Context;
            var intent = new Intent(context, typeof(TimerForegroundService));
            context.StopService(intent);
        }
        
        partial void UpdatePlatformNotification(int remainingSeconds)
        {
            try
            {
                TimerForegroundService.Instance?.UpdateNotification(remainingSeconds);
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating notification: {ex.Message}");
            }
        }
        
        partial void RegisterAlarmServicesPlatform(
            ISoundService? soundService,
            IVibrationService? vibrationService,
            bool soundEnabled,
            bool vibrationEnabled,
            int vibrationDurationSeconds)
        {
            TimerForegroundService.RegisterAlarmServices(
                soundService, vibrationService, soundEnabled, vibrationEnabled, vibrationDurationSeconds);
        }
        
        partial void UpdateAlarmSettingsPlatform(bool soundEnabled, bool vibrationEnabled, int vibrationDurationSeconds)
        {
            TimerForegroundService.UpdateAlarmSettings(soundEnabled, vibrationEnabled, vibrationDurationSeconds);
        }
    }
}
