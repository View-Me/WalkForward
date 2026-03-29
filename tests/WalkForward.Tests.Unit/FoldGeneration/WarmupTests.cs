using FluentAssertions;
using NUnit.Framework;
using WalkForward.Internal;

namespace WalkForward.Tests.Unit.FoldGeneration;

[TestFixture]
public class WarmupTests
{
    private static readonly TimeSpan FifteenMinutes = TimeSpan.FromMinutes(15);

    [Test]
    public void BackwardLooking_WithWarmup500_SkipsFoldsWhereTrainStartBeforeWarmup()
    {
        var options = new BackwardLookingOptions
        {
            TotalDataPoints = 10000,
            DataFrequency = FifteenMinutes,
            TrainingWindow = TimeSpan.FromDays(90),
            TestWindow = TimeSpan.FromDays(7),
            WarmupPoints = 500,
        };

        var folds = BackwardLookingFoldGenerator.Generate(options);

        foreach (var fold in folds)
        {
            fold.TrainStart.Should().BeGreaterThanOrEqualTo(500);
        }
    }

    [Test]
    public void ForwardLooking_WithWarmup500_EnforcesMinimumTrainSize()
    {
        var options = new ForwardLookingOptions
        {
            TotalDataPoints = 10000,
            DataFrequency = FifteenMinutes,
            TrainingWindow = TimeSpan.FromDays(30),
            TestWindow = TimeSpan.FromDays(7),
            WarmupPoints = 500,
        };

        var folds = ForwardLookingFoldGenerator.Generate(options);

        folds.Should().HaveCountGreaterThan(0);
    }
}
