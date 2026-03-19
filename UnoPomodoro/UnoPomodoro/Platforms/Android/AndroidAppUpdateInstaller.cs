using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Android.Content;
using Android.Content.PM;
using Android.Provider;
using AndroidX.Core.Content;

namespace UnoPomodoro.Platforms.Android;

internal static class AndroidAppUpdateInstaller
{
    private static readonly HttpClient HttpClient = new();
    private const string ApkMimeType = "application/vnd.android.package-archive";

    public static bool CanAttemptInPlaceUpdate(out string unsupportedReason)
    {
        unsupportedReason = string.Empty;

        var context = global::Android.App.Application.Context;
        if (context?.ApplicationInfo == null)
        {
            return true;
        }

        if (context.ApplicationInfo.Flags.HasFlag(ApplicationInfoFlags.Debuggable))
        {
            unsupportedReason = "This build is installed as a local/debug package, so Android will block an in-place update from the signed release APK. Open the release page, uninstall this build, then install the new release.";
            return false;
        }

        return true;
    }

    public static bool IsApkUrl(string? url)
    {
        return !string.IsNullOrWhiteSpace(url) &&
            System.Uri.TryCreate(url, System.UriKind.Absolute, out var uri) &&
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
            permissionIntent.SetData(global::Android.Net.Uri.Parse($"package:{context.PackageName}"));
            permissionIntent.AddFlags(ActivityFlags.NewTask);
            context.StartActivity(permissionIntent);
            return false;
        }

        if (!HttpClient.DefaultRequestHeaders.UserAgent.Any())
        {
            HttpClient.DefaultRequestHeaders.UserAgent.ParseAdd("PomodoroVijoApp/1.0");
        }

        using var response = await HttpClient.GetAsync(apkUrl, HttpCompletionOption.ResponseHeadersRead);
        response.EnsureSuccessStatusCode();

        var fileName = ResolveApkFileName(response.Content.Headers.ContentDisposition, apkUrl);

        var updatesDirectory = Path.Combine(context.CacheDir!.AbsolutePath!, "updates");
        Directory.CreateDirectory(updatesDirectory);

        var apkPath = Path.Combine(updatesDirectory, fileName);
        await using (var fileStream = File.Create(apkPath))
        {
            await response.Content.CopyToAsync(fileStream);
        }

        EnsureDownloadedPackageMatchesInstalledApp(context, apkPath);

        var apkFile = new Java.IO.File(apkPath);
        var apkUri = FileProvider.GetUriForFile(
            context,
            $"{context.PackageName}.fileprovider",
            apkFile);

        if (apkUri == null)
        {
            throw new InvalidOperationException("Android could not create a package installer URI for the downloaded update.");
        }

        var installIntent = CreateInstallIntent(apkUri);
        installIntent.AddFlags(ActivityFlags.NewTask);
        installIntent.AddFlags(ActivityFlags.GrantReadUriPermission);
        installIntent.AddFlags(ActivityFlags.ClearTop);
        installIntent.PutExtra(Intent.ExtraReturnResult, false);

        context.StartActivity(installIntent);
        return true;
    }

    private static string ResolveApkFileName(ContentDispositionHeaderValue? contentDisposition, string apkUrl)
    {
        var fileName = contentDisposition?.FileNameStar
            ?? contentDisposition?.FileName?.Trim('"')
            ?? Path.GetFileName(new System.Uri(apkUrl).LocalPath);

        if (string.IsNullOrWhiteSpace(fileName))
        {
            fileName = "pomodoro-update.apk";
        }

        foreach (var invalidCharacter in Path.GetInvalidFileNameChars())
        {
            fileName = fileName.Replace(invalidCharacter, '-');
        }

        if (!fileName.EndsWith(".apk", StringComparison.OrdinalIgnoreCase))
        {
            fileName += ".apk";
        }

        return fileName;
    }

    private static Intent CreateInstallIntent(global::Android.Net.Uri apkUri)
    {
        if (OperatingSystem.IsAndroidVersionAtLeast(29))
        {
            var viewIntent = new Intent(Intent.ActionView);
            viewIntent.SetDataAndType(apkUri, ApkMimeType);
            return viewIntent;
        }

#pragma warning disable CA1422 // Intent.ActionInstallPackage remains the supported path on older Android versions.
        var installIntent = new Intent(Intent.ActionInstallPackage);
#pragma warning restore CA1422
        installIntent.SetData(apkUri);
        return installIntent;
    }

    private static void EnsureDownloadedPackageMatchesInstalledApp(Context context, string apkPath)
    {
        var packageManager = context.PackageManager
            ?? throw new InvalidOperationException("Android package manager is unavailable while validating the downloaded update.");

        var installedPackage = packageManager.GetPackageInfo(context.PackageName!, GetSignatureQueryFlags());
        var archivePackage = packageManager.GetPackageArchiveInfo(apkPath, GetSignatureQueryFlags());
        if (archivePackage == null)
        {
            throw new InvalidOperationException("Android could not inspect the downloaded update package.");
        }

        if (!string.Equals(archivePackage.PackageName, context.PackageName, StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"Downloaded update targets {archivePackage.PackageName}, but this app is installed as {context.PackageName}.");
        }

        var installedSignerHashes = GetSignatureHashes(installedPackage);
        var archiveSignerHashes = GetSignatureHashes(archivePackage);
        if (installedSignerHashes.Count == 0 || archiveSignerHashes.Count == 0)
        {
            throw new InvalidOperationException("Android could not verify that the downloaded APK matches the installed app.");
        }

        if (!installedSignerHashes.SetEquals(archiveSignerHashes))
        {
            throw new InvalidOperationException("The downloaded APK is signed differently from the installed app, so Android cannot update it in place. Open the release page, uninstall the current app, then install the new release.");
        }
    }

    private static PackageInfoFlags GetSignatureQueryFlags()
    {
        if (OperatingSystem.IsAndroidVersionAtLeast(28))
        {
            return PackageInfoFlags.SigningCertificates;
        }

#pragma warning disable CA1422 // Signature queries on older Android versions still use the legacy flag.
        return PackageInfoFlags.Signatures;
#pragma warning restore CA1422
    }

    private static HashSet<string> GetSignatureHashes(PackageInfo? packageInfo)
    {
        var hashes = new HashSet<string>(StringComparer.Ordinal);
        if (packageInfo == null)
        {
            return hashes;
        }

        foreach (var signature in GetSignatures(packageInfo))
        {
            var signatureBytes = signature?.ToByteArray();
            if (signatureBytes == null || signatureBytes.Length == 0)
            {
                continue;
            }

            hashes.Add(Convert.ToHexString(SHA256.HashData(signatureBytes)));
        }

        return hashes;
    }

    private static Signature[] GetSignatures(PackageInfo packageInfo)
    {
        if (OperatingSystem.IsAndroidVersionAtLeast(28) && packageInfo.SigningInfo != null)
        {
            var currentSigners = packageInfo.SigningInfo.GetApkContentsSigners()?.ToArray();
            if (currentSigners?.Length > 0)
            {
                return currentSigners;
            }

            var signingHistory = packageInfo.SigningInfo.GetSigningCertificateHistory()?.ToArray();
            if (signingHistory?.Length > 0)
            {
                return signingHistory;
            }
        }

#pragma warning disable CA1422 // Legacy signature arrays are still required on pre-28 Android.
        return packageInfo.Signatures?.ToArray() ?? Array.Empty<Signature>();
#pragma warning restore CA1422
    }
}
