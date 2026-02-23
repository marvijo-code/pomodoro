using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Graphics;
using Android.Views;
using AndroidX.Core.View;
using AndroidX.Core.App;
using AndroidX.Core.Content;

namespace UnoPomodoro.Droid;

[Activity(
    MainLauncher = true,
    ConfigurationChanges = global::Uno.UI.ActivityHelper.AllConfigChanges,
    WindowSoftInputMode = SoftInput.AdjustNothing | SoftInput.StateHidden,
    ShowWhenLocked = true,
    TurnScreenOn = true
)]
public class MainActivity : Microsoft.UI.Xaml.ApplicationActivity
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        global::AndroidX.Core.SplashScreen.SplashScreen.InstallSplashScreen(this);

        base.OnCreate(savedInstanceState);

        EnableImmersiveFullscreen();
        
        // Request battery optimization exemption for reliable timer operation
        RequestBatteryOptimizationExemption();
        
        // Request POST_NOTIFICATIONS permission (required on Android 13+)
        RequestNotificationPermission();
    }
    
    protected override void OnResume()
    {
        base.OnResume();
        
        // Sync timer with wall clock when app resumes
        // This handles cases where power saving mode may have delayed timer ticks
        TimerResyncHelper.SyncTimer();
    }
    
    private void RequestNotificationPermission()
    {
        if (OperatingSystem.IsAndroidVersionAtLeast(33))
        {
            const string postNotificationsPermission = "android.permission.POST_NOTIFICATIONS";
            if (ContextCompat.CheckSelfPermission(this, postNotificationsPermission) != Permission.Granted)
            {
                ActivityCompat.RequestPermissions(this, new[] { postNotificationsPermission }, 1001);
            }
        }
    }
    
    private void RequestBatteryOptimizationExemption()
    {
        if (OperatingSystem.IsAndroidVersionAtLeast(23))
        {
            var powerManager = GetSystemService(PowerService) as PowerManager;
            var packageName = PackageName;
            
            if (powerManager != null && !string.IsNullOrEmpty(packageName) && 
                !powerManager.IsIgnoringBatteryOptimizations(packageName))
            {
                // Note: This just checks, we don't auto-prompt as it can be intrusive
                // The foreground service + wake lock should handle most cases
                System.Diagnostics.Debug.WriteLine("Battery optimization is active. Timer may be affected in extreme power saving modes.");
            }
        }
    }

    private void EnableImmersiveFullscreen()
    {
        if (Window == null)
        {
            return;
        }

        if (!OperatingSystem.IsAndroidVersionAtLeast(35))
        {
#pragma warning disable CA1422 // Validate platform compatibility - API obsolete on Android 35+
            Window.SetStatusBarColor(Color.Black);
            Window.SetNavigationBarColor(Color.Black);
#pragma warning restore CA1422
        }

        if (OperatingSystem.IsAndroidVersionAtLeast(30))
        {
            WindowCompat.SetDecorFitsSystemWindows(Window, false);

            var controller = WindowCompat.GetInsetsController(Window, Window.DecorView);
            if (controller != null)
            {
                controller.Hide(WindowInsetsCompat.Type.StatusBars() | WindowInsetsCompat.Type.NavigationBars());
                controller.SystemBarsBehavior = WindowInsetsControllerCompat.BehaviorShowTransientBarsBySwipe;
            }
        }
        else
        {
            Window.AddFlags(WindowManagerFlags.Fullscreen);
            var flags =
                SystemUiFlags.Fullscreen |
                SystemUiFlags.HideNavigation |
                SystemUiFlags.ImmersiveSticky |
                SystemUiFlags.LayoutStable |
                SystemUiFlags.LayoutFullscreen |
                SystemUiFlags.LayoutHideNavigation;
            Window.DecorView.SystemUiFlags = flags;
        }
    }

}
