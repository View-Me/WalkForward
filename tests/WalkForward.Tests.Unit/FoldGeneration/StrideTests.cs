using FluentAssertions;
using NUnit.Framework;
using WalkForward.Internal;

namespace WalkForward.Tests.Unit.FoldGeneration;

[TestFixture]
public class StrideTests
{
    private static readonly TimeSpan FifteenMinutes = TimeSpan.FromMinutes(15);

    [Test]
    public void ForwardLooking_DefaultStride_EqualsTestWindowSize()
    {
        var options = new ForwardLookingOptions
        {
            TotalDataPoints = 10000,
            DataFrequency = FifteenMinutes,
            TrainingWindow = TimeSpan.FromDays(30),
            TestWindow = TimeSpan.FromDays(7),
        };

        var folds = ForwardLookingFoldGenerator.Generate(options);

        if (folds.Count >= 2)
        {
            var stride = folds[1].TrainStart - folds[0].TrainStart;
            stride.Should().Be(672, "default stride should equal test window size");
        }
    }

    [Test]
    public void ForwardLooking_CustomStride3Days_ConsecutiveFoldsAre288Apart()
    {
        var options = new ForwardLookingOptions
        {
            TotalDataPoints = 10000,
            DataFrequency = FifteenMinutes,
            TrainingWindow = TimeSpan.FromDays(30),
            TestWindow = TimeSpan.FromDays(7),
            Stride = TimeSpan.FromDays(3),
        };

        var folds = ForwardLookingFoldGenerator.Generate(options);

        folds.Should().HaveCountGreaterThanOrEqualTo(2);

        for (var i = 1; i < folds.Count; i++)
        {
            var stride = folds[i].TrainStart - folds[i - 1].TrainStart;
            stride.Should().Be(
                288,
                $"Folds {i - 1} to {i}: stride should be 288 indices (3 days)");
        }
    }

    [Test]
    public void BackwardLooking_DoesNotUseStride_FoldsStepByTestWindowSize()
    {
        var options = new BackwardLookingOptions
        {
            TotalDataPoints = 10000,
            DataFrequency = FifteenMinutes,
            TrainingWindow = TimeSpan.FromDays(90),
            TestWindow = TimeSpan.FromDays(7),
        };

        var folds = BackwardLookingFoldGenerator.Generate(options);

        if (folds.Count >= 2)
        {
            var step = folds[0].TestEnd - folds[1].TestEnd;
            step.Should().Be(672, "backward-looking mode steps by test window size");
        }
    }
}
