namespace UnoPomodoro.Services;

public sealed class AppUpdateRelease
{
    public string TagName { get; set; } = string.Empty;
    public string ReleaseUrl { get; set; } = string.Empty;
    public string AssetUrl { get; set; } = string.Empty;
    public string AssetName { get; set; } = string.Empty;
}
