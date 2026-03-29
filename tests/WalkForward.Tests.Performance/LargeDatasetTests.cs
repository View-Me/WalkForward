using FluentAssertions;
using NUnit.Framework;

namespace WalkForward.Tests.Performance;

[TestFixture]
public class LargeDatasetTests
{
    [Test]
    [CancelAfter(5000)]
    public void BackwardLookingFoldGeneration_100kDataPoints_CompletesWithin5Seconds()
    {
        var folds = new FoldBuilder()
            .WithDataPoints(100_000)
            .WithDataFrequency(TimeSpan.FromMinutes(15))
            .BackwardLooking()
            .WithTrainingWindow(TimeSpan.FromDays(90))
            .WithTestWindow(TimeSpan.FromDays(7))
            .Build();

        folds.Should().NotBeEmpty();
    }

    [Test]
    [CancelAfter(5000)]
    public void ForwardLookingFoldGeneration_100kDataPoints_CompletesWithin5Seconds()
    {
        var folds = new FoldBuilder()
            .WithDataPoints(100_000)
            .WithDataFrequency(TimeSpan.FromMinutes(15))
            .ForwardLooking()
            .WithTrainingWindow(TimeSpan.FromDays(90))
            .WithTestWindow(TimeSpan.FromDays(7))
            .Build();

        folds.Should().NotBeEmpty();
    }

    [Test]
    [CancelAfter(10000)]
    public void BackwardLookingFoldGeneration_1MillionDataPoints_CompletesWithin10Seconds()
    {
        var folds = new FoldBuilder()
            .WithDataPoints(1_000_000)
            .WithDataFrequency(TimeSpan.FromMinutes(1))
            .BackwardLooking()
            .WithTrainingWindow(TimeSpan.FromDays(30))
            .WithTestWindow(TimeSpan.FromDays(7))
            .Build();

        folds.Should().NotBeEmpty();
    }

    [Test]
    [CancelAfter(10000)]
    public void ForwardLookingFoldGeneration_1MillionDataPoints_CompletesWithin10Seconds()
    {
        var folds = new FoldBuilder()
            .WithDataPoints(1_000_000)
            .WithDataFrequency(TimeSpan.FromMinutes(1))
            .ForwardLooking()
            .WithTrainingWindow(TimeSpan.FromDays(30))
            .WithTestWindow(TimeSpan.FromDays(7))
            .Build();

        folds.Should().NotBeEmpty();
    }
}
