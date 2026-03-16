using FluentAssertions;
using UnoPomodoro.Services;
using Xunit;

namespace UnoPomodoro.Tests.Services;

public class AppUpdateReleaseParserTests
{
    [Fact]
    public void ParseLatestRelease_ShouldPreferSignedApkAsset()
    {
        var json = """
        {
          "tag_name": "v1.2.0",
          "html_url": "https://github.com/marvijo-code/pomodoro/releases/tag/v1.2.0",
          "assets": [
            {
              "name": "pomodoro-debug.apk",
              "browser_download_url": "https://example.com/pomodoro-debug.apk"
            },
            {
              "name": "pomodoro-Signed.apk",
              "browser_download_url": "https://example.com/pomodoro-Signed.apk"
            }
          ]
        }
        """;

        var release = AppUpdateReleaseParser.ParseLatestRelease(json);

        release.Should().NotBeNull();
        release!.TagName.Should().Be("v1.2.0");
        release.AssetName.Should().Be("pomodoro-Signed.apk");
        release.AssetUrl.Should().Be("https://example.com/pomodoro-Signed.apk");
    }

    [Fact]
    public void ParseLatestRelease_ShouldReturnReleaseWithoutApkWhenMissing()
    {
        var json = """
        {
          "tag_name": "v1.2.0",
          "html_url": "https://github.com/marvijo-code/pomodoro/releases/tag/v1.2.0",
          "assets": [
            {
              "name": "notes.txt",
              "browser_download_url": "https://example.com/notes.txt"
            }
          ]
        }
        """;

        var release = AppUpdateReleaseParser.ParseLatestRelease(json);

        release.Should().NotBeNull();
        release!.AssetUrl.Should().BeEmpty();
        release.ReleaseUrl.Should().Be("https://github.com/marvijo-code/pomodoro/releases/tag/v1.2.0");
    }
}
