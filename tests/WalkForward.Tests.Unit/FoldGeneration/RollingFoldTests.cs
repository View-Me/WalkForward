using FluentAssertions;
using NUnit.Framework;
using WalkForward.Internal;

namespace WalkForward.Tests.Unit.FoldGeneration;

[TestFixture]
public class RollingFoldTests
{
    private static readonly TimeSpan FifteenMinutes = TimeSpan.FromMinutes(15);

    [Test]
    public void Generate_10000Points_30DayTrain_7DayTest_ProducesFoldsWalkingForwards()
    {
        var options = new RollingOptions
        {
            TotalDataPoints = 10000,
            DataFrequency = FifteenMinutes,
            TrainingWindow = TimeSpan.FromDays(30),
            TestWindow = TimeSpan.FromDays(7),
        };

        var folds = RollingFoldGenerator.Generate(options);

        folds.Should().HaveCountGreaterThan(0);
    }

    [Test]
    public void Generate_Fold0TrainStartsAtZero()
    {
        var options = new RollingOptions
        {
            TotalDataPoints = 10000,
            DataFrequency = FifteenMinutes,
            TrainingWindow = TimeSpan.FromDays(30),
            TestWindow = TimeSpan.FromDays(7),
        };

        var folds = RollingFoldGenerator.Generate(options);

        folds[0].TrainStart.Should().Be(0);
        folds[0].TrainEnd.Should().Be(2880);
        folds[0].TestStart.Should().Be(2880);
        folds[0].TestEnd.Should().Be(3552);
    }

    [Test]
    public void Generate_TrainingWindowSizeIsFixedAcrossAllFolds()
    {
        var options = new RollingOptions
        {
            TotalDataPoints = 10000,
            DataFrequency = FifteenMinutes,
            TrainingWindow = TimeSpan.FromDays(30),
            TestWindow = TimeSpan.FromDays(7),
        };

        var folds = RollingFoldGenerator.Generate(options);
        var expectedTrainLength = 2880;

        foreach (var fold in folds)
        {
            fold.TrainLength.Should().Be(
                expectedTrainLength,
                $"Fold {fold.FoldIndex} should have fixed train window size");
        }
    }

    [Test]
    public void Generate_FoldBoundariesAreNonOverlapping()
    {
        var options = new RollingOptions
        {
            TotalDataPoints = 10000,
            DataFrequency = FifteenMinutes,
            TrainingWindow = TimeSpan.FromDays(30),
            TestWindow = TimeSpan.FromDays(7),
        };

        var folds = RollingFoldGenerator.Generate(options);

        for (var i = 0; i < folds.Count; i++)
        {
            var fold = folds[i];
            fold.TrainEnd.Should().BeLessThanOrEqualTo(
                fold.TestStart,
                $"Fold {i}: train should end before test starts");
        }
    }

    [Test]
    public void Generate_LastFoldTestEndDoesNotExceedTotalDataPoints()
    {
        var options = new RollingOptions
        {
            TotalDataPoints = 10000,
            DataFrequency = FifteenMinutes,
            TrainingWindow = TimeSpan.FromDays(30),
            TestWindow = TimeSpan.FromDays(7),
        };

        var folds = RollingFoldGenerator.Generate(options);

        foreach (var fold in folds)
        {
            fold.TestEnd.Should().BeLessThanOrEqualTo(
                10000,
                $"Fold {fold.FoldIndex}: test end should not exceed total data points");
        }
    }
}
