using Android.Content;
using UnoPomodoro.Platforms.Android;

namespace UnoPomodoro.Services
{
    public partial class TimerService
    {
        partial void StartPlatformBackgroundService()
        {
            var context = Android.App.Application.Context;
            var intent = new Intent(context, typeof(TimerForegroundService));
            
            if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.O)
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
    }
}
