using System;

namespace UnoPomodoro.Services;

public static class AppUpdateVersionComparer
{
    public static bool IsNewerVersion(string latestVersion, string currentVersion)
    {
        if (!TryParseVersion(latestVersion, out var latest) || !TryParseVersion(currentVersion, out var current))
        {
            return false;
        }

        return latest > current;
    }

    private static bool TryParseVersion(string value, out Version version)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            version = new Version(0, 0);
            return false;
        }

        var normalized = value.Trim();
        if (normalized.StartsWith("v", StringComparison.OrdinalIgnoreCase))
        {
            normalized = normalized[1..];
        }

        var suffixIndex = normalized.IndexOfAny(['-', '+']);
        if (suffixIndex >= 0)
        {
            normalized = normalized[..suffixIndex];
        }

        var parsed = Version.TryParse(normalized, out var candidate);
        version = candidate ?? new Version(0, 0);
        return parsed;
    }
}
