using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Graphics;
using Android.OS;
using Android.Views;
using Android.Widget;
using AndroidX.Core.View;

namespace UnoPomodoro.Droid;

[Activity(
    MainLauncher = true,
    ConfigurationChanges = global::Uno.UI.ActivityHelper.AllConfigChanges,
    WindowSoftInputMode = SoftInput.AdjustNothing | SoftInput.StateHidden
)]
public class MainActivity : Microsoft.UI.Xaml.ApplicationActivity
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        global::AndroidX.Core.SplashScreen.SplashScreen.InstallSplashScreen(this);

        base.OnCreate(savedInstanceState);

        EnableImmersiveFullscreen();
    }

    private void EnableImmersiveFullscreen()
    {
        if (Window == null)
        {
            return;
        }

        Window.SetStatusBarColor(Color.Black);
        Window.SetNavigationBarColor(Color.Black);

        if (Build.VERSION.SdkInt >= BuildVersionCodes.R)
        {
            Window.SetDecorFitsSystemWindows(false);
            var controller = Window.InsetsController;
            if (controller != null)
            {
                controller.Hide(WindowInsets.Type.StatusBars() | WindowInsets.Type.NavigationBars());
                controller.SystemBarsBehavior = (int)WindowInsetsControllerBehavior.ShowTransientBarsBySwipe;
            }
        }
        else
        {
#pragma warning disable CA1422 // Validate platform compatibility - legacy immersive mode for older devices
            Window.AddFlags(WindowManagerFlags.Fullscreen);
            var systemUiFlags = (StatusBarVisibility)(
                SystemUiFlags.Fullscreen |
                SystemUiFlags.HideNavigation |
                SystemUiFlags.ImmersiveSticky |
                SystemUiFlags.LayoutStable |
                SystemUiFlags.LayoutFullscreen |
                SystemUiFlags.LayoutHideNavigation);
            Window.DecorView.SystemUiVisibility = systemUiFlags;
#pragma warning restore CA1422
        }
    }

}
