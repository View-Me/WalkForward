using FluentAssertions;
using NUnit.Framework;

namespace WalkForward.Tests.Performance;

[TestFixture]
public class LargeDatasetTests
{
    [Test]
    [CancelAfter(5000)]
    public void AnchoredFoldGeneration_100kDataPoints_CompletesWithin5Seconds()
    {
        var folds = new WalkForwardBuilder()
            .WithDataPoints(100_000)
            .WithDataFrequency(TimeSpan.FromMinutes(15))
            .Anchored()
            .WithTrainingWindow(TimeSpan.FromDays(90))
            .WithTestWindow(TimeSpan.FromDays(7))
            .Build();

        folds.Should().NotBeEmpty();
    }

    [Test]
    [CancelAfter(5000)]
    public void RollingFoldGeneration_100kDataPoints_CompletesWithin5Seconds()
    {
        var folds = new WalkForwardBuilder()
            .WithDataPoints(100_000)
            .WithDataFrequency(TimeSpan.FromMinutes(15))
            .Rolling()
            .WithTrainingWindow(TimeSpan.FromDays(90))
            .WithTestWindow(TimeSpan.FromDays(7))
            .Build();

        folds.Should().NotBeEmpty();
    }

    [Test]
    [CancelAfter(10000)]
    public void AnchoredFoldGeneration_1MillionDataPoints_CompletesWithin10Seconds()
    {
        var folds = new WalkForwardBuilder()
            .WithDataPoints(1_000_000)
            .WithDataFrequency(TimeSpan.FromMinutes(1))
            .Anchored()
            .WithTrainingWindow(TimeSpan.FromDays(30))
            .WithTestWindow(TimeSpan.FromDays(7))
            .Build();

        folds.Should().NotBeEmpty();
    }

    [Test]
    [CancelAfter(10000)]
    public void RollingFoldGeneration_1MillionDataPoints_CompletesWithin10Seconds()
    {
        var folds = new WalkForwardBuilder()
            .WithDataPoints(1_000_000)
            .WithDataFrequency(TimeSpan.FromMinutes(1))
            .Rolling()
            .WithTrainingWindow(TimeSpan.FromDays(30))
            .WithTestWindow(TimeSpan.FromDays(7))
            .Build();

        folds.Should().NotBeEmpty();
    }
}
