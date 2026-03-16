using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Android.Content;
using Android.Net;
using Android.Provider;
using AndroidX.Core.Content;

namespace UnoPomodoro.Platforms.Android;

internal static class AndroidAppUpdateInstaller
{
    private static readonly HttpClient HttpClient = new();

    public static bool IsApkUrl(string? url)
    {
        return !string.IsNullOrWhiteSpace(url) &&
            Uri.TryCreate(url, UriKind.Absolute, out var uri) &&
            uri.AbsolutePath.EndsWith(".apk", StringComparison.OrdinalIgnoreCase);
    }

    public static async Task<bool> DownloadAndInstallAsync(string apkUrl)
    {
        var context = global::Android.App.Application.Context;
        if (context == null)
        {
            return false;
        }

        if (OperatingSystem.IsAndroidVersionAtLeast(26) &&
            !context.PackageManager!.CanRequestPackageInstalls())
        {
            var permissionIntent = new Intent(Settings.ActionManageUnknownAppSources);
            permissionIntent.SetData(Uri.Parse($"package:{context.PackageName}"));
            permissionIntent.AddFlags(ActivityFlags.NewTask);
            context.StartActivity(permissionIntent);
            return false;
        }

        if (!HttpClient.DefaultRequestHeaders.UserAgent.Any())
        {
            HttpClient.DefaultRequestHeaders.UserAgent.ParseAdd("PomodoroVijoApp/1.0");
        }

        using var response = await HttpClient.GetAsync(apkUrl);
        response.EnsureSuccessStatusCode();

        var fileName = response.Content.Headers.ContentDisposition?.FileNameStar
            ?? response.Content.Headers.ContentDisposition?.FileName?.Trim('"')
            ?? Path.GetFileName(new Uri(apkUrl).LocalPath);

        if (string.IsNullOrWhiteSpace(fileName))
        {
            fileName = "pomodoro-update.apk";
        }

        if (!fileName.EndsWith(".apk", StringComparison.OrdinalIgnoreCase))
        {
            fileName += ".apk";
        }

        var updatesDirectory = Path.Combine(context.CacheDir!.AbsolutePath!, "updates");
        Directory.CreateDirectory(updatesDirectory);

        var apkPath = Path.Combine(updatesDirectory, fileName);
        await using (var fileStream = File.Create(apkPath))
        {
            await response.Content.CopyToAsync(fileStream);
        }

        var apkFile = new Java.IO.File(apkPath);
        var apkUri = FileProvider.GetUriForFile(
            context,
            $"{context.PackageName}.fileprovider",
            apkFile);

        var installIntent = new Intent(Intent.ActionView);
        installIntent.SetDataAndType(apkUri, "application/vnd.android.package-archive");
        installIntent.AddFlags(ActivityFlags.NewTask);
        installIntent.AddFlags(ActivityFlags.GrantReadUriPermission);
        installIntent.AddFlags(ActivityFlags.ClearTop);

        context.StartActivity(installIntent);
        return true;
    }
}
