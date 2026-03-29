using FluentAssertions;
using NUnit.Framework;
using WalkForward.Internal;

namespace WalkForward.Tests.Unit.FoldGeneration;

[TestFixture]
public class BackwardLookingFoldTests
{
    private static readonly TimeSpan FifteenMinutes = TimeSpan.FromMinutes(15);

    [Test]
    public void Generate_10000Points_90DayTrain_7DayTest_ProducesFoldsWalkingBackwards()
    {
        var options = new BackwardLookingOptions
        {
            TotalDataPoints = 10000,
            DataFrequency = FifteenMinutes,
            TrainingWindow = TimeSpan.FromDays(90),
            TestWindow = TimeSpan.FromDays(7),
        };

        var folds = BackwardLookingFoldGenerator.Generate(options);

        // Auto: (10000 - 8640 - 0) / 672 = 2.02 -> floor = 2
        folds.Should().HaveCount(2);
    }

    [Test]
    public void Generate_Fold0HasTestWindowEndingAtDataEnd()
    {
        var options = new BackwardLookingOptions
        {
            TotalDataPoints = 10000,
            DataFrequency = FifteenMinutes,
            TrainingWindow = TimeSpan.FromDays(90),
            TestWindow = TimeSpan.FromDays(7),
        };

        var folds = BackwardLookingFoldGenerator.Generate(options);

        folds[0].TestEnd.Should().Be(10000);
        folds[0].TestStart.Should().Be(10000 - 672);
    }

    [Test]
    public void Generate_EachFoldTrainingWindowStartsEarlier()
    {
        var options = new BackwardLookingOptions
        {
            TotalDataPoints = 10000,
            DataFrequency = FifteenMinutes,
            TrainingWindow = TimeSpan.FromDays(90),
            TestWindow = TimeSpan.FromDays(7),
        };

        var folds = BackwardLookingFoldGenerator.Generate(options);

        folds[0].TrainStart.Should().Be(688);
        folds[0].TrainEnd.Should().Be(9328);
        folds[1].TrainStart.Should().Be(16);
        folds[1].TrainEnd.Should().Be(8656);
    }

    [Test]
    public void Generate_FoldBoundariesAreNonOverlapping()
    {
        var options = new BackwardLookingOptions
        {
            TotalDataPoints = 10000,
            DataFrequency = FifteenMinutes,
            TrainingWindow = TimeSpan.FromDays(90),
            TestWindow = TimeSpan.FromDays(7),
        };

        var folds = BackwardLookingFoldGenerator.Generate(options);

        for (var i = 0; i < folds.Count; i++)
        {
            var fold = folds[i];

            // Train and test within same fold should not overlap
            fold.TrainEnd.Should().BeLessThanOrEqualTo(
                fold.TestStart,
                $"Fold {i}: train should end before test starts");
        }

        // Adjacent folds: fold[i].TestEnd <= fold[i-1].TestStart (walking backwards)
        for (var i = 1; i < folds.Count; i++)
        {
            folds[i].TestEnd.Should().BeLessThanOrEqualTo(
                folds[i - 1].TestStart,
                $"Fold {i} test should end before fold {i - 1} test starts");
        }
    }

    [Test]
    public void Generate_WithWarmup500_SkipsFoldsBeforeWarmup()
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

        folds.Should().HaveCount(1);
        folds[0].TrainStart.Should().BeGreaterThanOrEqualTo(500);
    }

    [Test]
    public void Generate_RequestedMoreFoldsThanPossible_ReturnsFewerFolds()
    {
        var options = new BackwardLookingOptions
        {
            TotalDataPoints = 10000,
            DataFrequency = FifteenMinutes,
            TrainingWindow = TimeSpan.FromDays(90),
            TestWindow = TimeSpan.FromDays(7),
            MaxFolds = 10,
        };

        var folds = BackwardLookingFoldGenerator.Generate(options);

        folds.Should().HaveCount(2);
    }
}
