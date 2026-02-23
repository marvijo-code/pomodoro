using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Microsoft.UI.Xaml.Media;

[assembly: Android.App.UsesPermission(Android.Manifest.Permission.Vibrate)]
[assembly: Android.App.UsesPermission(Android.Manifest.Permission.WakeLock)]
[assembly: Android.App.UsesPermission(Android.Manifest.Permission.ForegroundService)]
[assembly: Android.App.UsesPermission(Android.Manifest.Permission.ForegroundServiceSpecialUse)]
[assembly: Android.App.UsesPermission(Android.Manifest.Permission.PostNotifications)]
[assembly: Android.App.UsesPermission(Android.Manifest.Permission.UseFullScreenIntent)]

namespace UnoPomodoro.Droid;

[global::Android.App.ApplicationAttribute(
    Label = "@string/ApplicationName",
    Icon = "@mipmap/icon",
    LargeHeap = true,
    HardwareAccelerated = true,
    Theme = "@style/Theme.App.Starting"
)]
public class Application : Microsoft.UI.Xaml.NativeApplication
{
    static Application()
    {
        App.InitializeLogging();
    }
    
    public Application(IntPtr javaReference, JniHandleOwnership transfer)
        : base(() => new App(), javaReference, transfer)
    {
    }

}

