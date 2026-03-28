using FluentAssertions;
using NUnit.Framework;

namespace WalkForward.Tests.Unit.Builder;

[TestFixture]
public class WalkForwardBuilderTests
{
    [Test]
    public void Anchored_returns_AnchoredBuilder_instance()
    {
        var builder = new WalkForwardBuilder()
            .WithDataPoints(10000)
            .WithDataFrequency(TimeSpan.FromMinutes(15));

        var anchored = builder.Anchored();

        anchored.Should().BeOfType<AnchoredBuilder>();
    }

    [Test]
    public void Rolling_returns_RollingBuilder_instance()
    {
        var builder = new WalkForwardBuilder()
            .WithDataPoints(10000)
            .WithDataFrequency(TimeSpan.FromMinutes(15));

        var rolling = builder.Rolling();

        rolling.Should().BeOfType<RollingBuilder>();
    }

    [Test]
    public void Build_without_DataPoints_throws_ArgumentOutOfRangeException()
    {
        var builder = new WalkForwardBuilder()
            .WithDataFrequency(TimeSpan.FromMinutes(15))
            .Anchored()
            .WithTrainingWindow(TimeSpan.FromDays(90))
            .WithTestWindow(TimeSpan.FromDays(7));

        var act = () => builder.Build();

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Test]
    public void Build_without_DataFrequency_throws_ArgumentOutOfRangeException()
    {
        var builder = new WalkForwardBuilder()
            .WithDataPoints(10000)
            .Anchored()
            .WithTrainingWindow(TimeSpan.FromDays(90))
            .WithTestWindow(TimeSpan.FromDays(7));

        var act = () => builder.Build();

        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}
