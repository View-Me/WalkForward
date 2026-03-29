using FluentAssertions;
using NUnit.Framework;
using WalkForward.Internal;

namespace WalkForward.Tests.Unit.FoldGeneration;

[TestFixture]
public class AutoFoldCountTests
{
    private static readonly TimeSpan FifteenMinutes = TimeSpan.FromMinutes(15);

    [Test]
    public void BackwardLooking_MaxFoldsNull_AutoComputesFoldCount()
    {
        var options = new BackwardLookingOptions
        {
            TotalDataPoints = 10000,
            DataFrequency = FifteenMinutes,
            TrainingWindow = TimeSpan.FromDays(90),
            TestWindow = TimeSpan.FromDays(7),
        };

        var folds = BackwardLookingFoldGenerator.Generate(options);

        folds.Should().HaveCount(2);
    }

    [Test]
    public void ForwardLooking_MaxFoldsNull_AutoComputesFoldCount()
    {
        var options = new ForwardLookingOptions
        {
            TotalDataPoints = 10000,
            DataFrequency = FifteenMinutes,
            TrainingWindow = TimeSpan.FromDays(30),
            TestWindow = TimeSpan.FromDays(7),
        };

        var folds = ForwardLookingFoldGenerator.Generate(options);

        // Verify the folds don't exceed data and auto-compute is correct
        folds.Should().HaveCountGreaterThan(0);
        folds.Should().HaveCount(10);
    }

    [Test]
    public void BackwardLooking_MaxFolds3_ReturnsAtMost3Folds()
    {
        var options = new BackwardLookingOptions
        {
            TotalDataPoints = 10000,
            DataFrequency = FifteenMinutes,
            TrainingWindow = TimeSpan.FromDays(10),
            TestWindow = TimeSpan.FromDays(3),
            MaxFolds = 3,
        };

        var folds = BackwardLookingFoldGenerator.Generate(options);

        folds.Should().HaveCountLessThanOrEqualTo(3);
    }

    [Test]
    public void ForwardLooking_MaxFolds3_ReturnsAtMost3Folds()
    {
        var options = new ForwardLookingOptions
        {
            TotalDataPoints = 10000,
            DataFrequency = FifteenMinutes,
            TrainingWindow = TimeSpan.FromDays(10),
            TestWindow = TimeSpan.FromDays(3),
            MaxFolds = 3,
        };

        var folds = ForwardLookingFoldGenerator.Generate(options);

        folds.Should().HaveCountLessThanOrEqualTo(3);
    }
}
