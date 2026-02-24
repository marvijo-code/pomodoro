using FluentAssertions;
using UnoPomodoro.Services;
using Xunit;

namespace UnoPomodoro.Tests.Services;

public class AppUpdateVersionComparerTests
{
    [Theory]
    [InlineData("v1.0.1", "1.0", true)]
    [InlineData("1.0.0", "1.0.0", false)]
    [InlineData("v1.2.0-beta.1", "1.1.9", true)]
    [InlineData("v1.2.0-beta.1", "1.2.0", false)]
    [InlineData("invalid", "1.0.0", false)]
    public void IsNewerVersion_HandlesReleaseTags(string latest, string current, bool expected)
    {
        var result = AppUpdateVersionComparer.IsNewerVersion(latest, current);
        result.Should().Be(expected);
    }
}
