using System;
using System.Text.Json;

namespace UnoPomodoro.Services;

public static class AppUpdateReleaseParser
{
    public static AppUpdateRelease? ParseLatestRelease(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        var tagName = root.TryGetProperty("tag_name", out var tagElement)
            ? tagElement.GetString()
            : null;
        var releaseUrl = root.TryGetProperty("html_url", out var releaseUrlElement)
            ? releaseUrlElement.GetString()
            : null;

        if (string.IsNullOrWhiteSpace(tagName) || string.IsNullOrWhiteSpace(releaseUrl))
        {
            return null;
        }

        string assetUrl = string.Empty;
        string assetName = string.Empty;

        if (root.TryGetProperty("assets", out var assetsElement) &&
            assetsElement.ValueKind == JsonValueKind.Array)
        {
            foreach (var asset in assetsElement.EnumerateArray())
            {
                var name = asset.TryGetProperty("name", out var nameElement)
                    ? nameElement.GetString()
                    : null;
                var downloadUrl = asset.TryGetProperty("browser_download_url", out var urlElement)
                    ? urlElement.GetString()
                    : null;

                if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(downloadUrl))
                {
                    continue;
                }

                if (!name.EndsWith(".apk", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                assetUrl = downloadUrl;
                assetName = name;

                if (name.Contains("Signed", StringComparison.OrdinalIgnoreCase))
                {
                    break;
                }
            }
        }

        return new AppUpdateRelease
        {
            TagName = tagName,
            ReleaseUrl = releaseUrl,
            AssetUrl = assetUrl,
            AssetName = assetName
        };
    }
}
