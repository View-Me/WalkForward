using FluentAssertions;
using NUnit.Framework;
using WalkForward.Internal;

namespace WalkForward.Tests.Unit.FoldGeneration;

[TestFixture]
public class AutoFoldCountTests
{
    private static readonly TimeSpan FifteenMinutes = TimeSpan.FromMinutes(15);

    [Test]
    public void Anchored_MaxFoldsNull_AutoComputesFoldCount()
    {
        var options = new AnchoredOptions
        {
            TotalDataPoints = 10000,
            DataFrequency = FifteenMinutes,
            TrainingWindow = TimeSpan.FromDays(90),
            TestWindow = TimeSpan.FromDays(7),
        };

        var folds = AnchoredFoldGenerator.Generate(options);

        folds.Should().HaveCount(2);
    }

    [Test]
    public void Rolling_MaxFoldsNull_AutoComputesFoldCount()
    {
        var options = new RollingOptions
        {
            TotalDataPoints = 10000,
            DataFrequency = FifteenMinutes,
            TrainingWindow = TimeSpan.FromDays(30),
            TestWindow = TimeSpan.FromDays(7),
        };

        var folds = RollingFoldGenerator.Generate(options);

        // Verify the folds don't exceed data and auto-compute is correct
        folds.Should().HaveCountGreaterThan(0);
        folds.Should().HaveCount(10);
    }

    [Test]
    public void Anchored_MaxFolds3_ReturnsAtMost3Folds()
    {
        var options = new AnchoredOptions
        {
            TotalDataPoints = 10000,
            DataFrequency = FifteenMinutes,
            TrainingWindow = TimeSpan.FromDays(10),
            TestWindow = TimeSpan.FromDays(3),
            MaxFolds = 3,
        };

        var folds = AnchoredFoldGenerator.Generate(options);

        folds.Should().HaveCountLessThanOrEqualTo(3);
    }

    [Test]
    public void Rolling_MaxFolds3_ReturnsAtMost3Folds()
    {
        var options = new RollingOptions
        {
            TotalDataPoints = 10000,
            DataFrequency = FifteenMinutes,
            TrainingWindow = TimeSpan.FromDays(10),
            TestWindow = TimeSpan.FromDays(3),
            MaxFolds = 3,
        };

        var folds = RollingFoldGenerator.Generate(options);

        folds.Should().HaveCountLessThanOrEqualTo(3);
    }
}
