using FluentAssertions;
using NUnit.Framework;

namespace WalkForward.Tests.Unit.Builder;

[TestFixture]
public class FoldBuilderTests
{
    [Test]
    public void BackwardLooking_returns_BackwardLookingBuilder_instance()
    {
        var builder = new FoldBuilder()
            .WithDataPoints(10000)
            .WithDataFrequency(TimeSpan.FromMinutes(15));

        var backwardLooking = builder.BackwardLooking();

        backwardLooking.Should().BeOfType<BackwardLookingBuilder>();
    }

    [Test]
    public void ForwardLooking_returns_ForwardLookingBuilder_instance()
    {
        var builder = new FoldBuilder()
            .WithDataPoints(10000)
            .WithDataFrequency(TimeSpan.FromMinutes(15));

        var forwardLooking = builder.ForwardLooking();

        forwardLooking.Should().BeOfType<ForwardLookingBuilder>();
    }

    [Test]
    public void Build_without_DataPoints_throws_ArgumentOutOfRangeException()
    {
        var builder = new FoldBuilder()
            .WithDataFrequency(TimeSpan.FromMinutes(15))
            .BackwardLooking()
            .WithTrainingWindow(TimeSpan.FromDays(90))
            .WithTestWindow(TimeSpan.FromDays(7));

        var act = () => builder.Build();

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Test]
    public void Build_without_DataFrequency_throws_ArgumentOutOfRangeException()
    {
        var builder = new FoldBuilder()
            .WithDataPoints(10000)
            .BackwardLooking()
            .WithTrainingWindow(TimeSpan.FromDays(90))
            .WithTestWindow(TimeSpan.FromDays(7));

        var act = () => builder.Build();

        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}
