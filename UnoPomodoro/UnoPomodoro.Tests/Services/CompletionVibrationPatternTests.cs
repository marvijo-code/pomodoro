using FluentAssertions;
using UnoPomodoro.Services;
using Xunit;

namespace UnoPomodoro.Tests.Services;

public class CompletionVibrationPatternTests
{
    [Fact]
    public void Build_ShouldCreateFinitePatternMatchingRequestedDuration()
    {
        var pattern = CompletionVibrationPattern.Build(5);

        pattern.Should().NotBeEmpty();
        pattern[0].Should().Be(0);
        CompletionVibrationPattern.GetDurationMilliseconds(pattern).Should().Be(5000);
    }

    [Fact]
    public void Build_ShouldClampToAtLeastOneSecond()
    {
        var pattern = CompletionVibrationPattern.Build(0);

        CompletionVibrationPattern.GetDurationMilliseconds(pattern).Should().Be(1000);
    }
}
