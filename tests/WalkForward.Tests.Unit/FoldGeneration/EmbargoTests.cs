using FluentAssertions;
using NUnit.Framework;
using WalkForward.Internal;

namespace WalkForward.Tests.Unit.FoldGeneration;

[TestFixture]
public class EmbargoTests
{
    private static readonly TimeSpan FifteenMinutes = TimeSpan.FromMinutes(15);

    [Test]
    public void BackwardLooking_With4hEmbargo_Creates16IndexGap()
    {
        var options = new BackwardLookingOptions
        {
            TotalDataPoints = 10000,
            DataFrequency = FifteenMinutes,
            TrainingWindow = TimeSpan.FromDays(90),
            TestWindow = TimeSpan.FromDays(7),
            Embargo = TimeSpan.FromHours(4),
        };

        var folds = BackwardLookingFoldGenerator.Generate(options);

        foreach (var fold in folds)
        {
            var embargoGap = fold.TestStart - fold.TrainEnd;
            embargoGap.Should().Be(16);
            fold.EmbargoLength.Should().Be(16);
        }
    }

    [Test]
    public void ForwardLooking_With4hEmbargo_Creates16IndexGap()
    {
        var options = new ForwardLookingOptions
        {
            TotalDataPoints = 10000,
            DataFrequency = FifteenMinutes,
            TrainingWindow = TimeSpan.FromDays(30),
            TestWindow = TimeSpan.FromDays(7),
            Embargo = TimeSpan.FromHours(4),
        };

        var folds = ForwardLookingFoldGenerator.Generate(options);

        foreach (var fold in folds)
        {
            var embargoGap = fold.TestStart - fold.TrainEnd;
            embargoGap.Should().Be(16);
            fold.EmbargoLength.Should().Be(16);
        }
    }

    [Test]
    public void NonExactEmbargo_RoundsUpTo16Indices()
    {
        var options = new BackwardLookingOptions
        {
            TotalDataPoints = 10000,
            DataFrequency = FifteenMinutes,
            TrainingWindow = TimeSpan.FromDays(90),
            TestWindow = TimeSpan.FromDays(7),
            Embargo = TimeSpan.FromMinutes(230),
        };

        var folds = BackwardLookingFoldGenerator.Generate(options);

        foreach (var fold in folds)
        {
            var embargoGap = fold.TestStart - fold.TrainEnd;
            embargoGap.Should().Be(16);
        }
    }

    [Test]
    public void ZeroEmbargo_TestStartsImmediatelyAfterTrainEnds()
    {
        var options = new BackwardLookingOptions
        {
            TotalDataPoints = 10000,
            DataFrequency = FifteenMinutes,
            TrainingWindow = TimeSpan.FromDays(90),
            TestWindow = TimeSpan.FromDays(7),
        };

        var folds = BackwardLookingFoldGenerator.Generate(options);

        foreach (var fold in folds)
        {
            fold.TestStart.Should().Be(
                fold.TrainEnd,
                $"Fold {fold.FoldIndex}: with zero embargo, test starts at train end");
            fold.EmbargoLength.Should().Be(0);
        }
    }
}
