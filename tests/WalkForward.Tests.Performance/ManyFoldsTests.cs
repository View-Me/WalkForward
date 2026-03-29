using FluentAssertions;
using NUnit.Framework;

namespace WalkForward.Tests.Performance;

[TestFixture]
public class ManyFoldsTests
{
    [Test]
    [CancelAfter(5000)]
    public void BackwardLookingMode_Generates100PlusFolds()
    {
        var folds = new FoldBuilder()
            .WithDataPoints(500_000)
            .WithDataFrequency(TimeSpan.FromMinutes(1))
            .BackwardLooking()
            .WithTrainingWindow(TimeSpan.FromDays(1))
            .WithTestWindow(TimeSpan.FromHours(4))
            .Build();

        folds.Count.Should().BeGreaterThanOrEqualTo(
            100,
            "500k 1-min data points with 1-day train / 4-hour test should generate 100+ folds");
    }

    [Test]
    [CancelAfter(5000)]
    public void ForwardLookingMode_Generates100PlusFolds()
    {
        var folds = new FoldBuilder()
            .WithDataPoints(500_000)
            .WithDataFrequency(TimeSpan.FromMinutes(1))
            .ForwardLooking()
            .WithTrainingWindow(TimeSpan.FromDays(1))
            .WithTestWindow(TimeSpan.FromHours(4))
            .Build();

        folds.Count.Should().BeGreaterThanOrEqualTo(
            100,
            "500k 1-min data points with 1-day train / 4-hour test should generate 100+ folds");
    }

    [Test]
    [CancelAfter(1000)]
    public void ConsistencyCompute_1000Folds_CompletesInstantly()
    {
        var returns = Enumerable.Range(0, 1000)
            .Select(i => (double)((i % 3) - 1) * 0.01)
            .ToArray();

        var metrics = Consistency.Compute(returns);

        metrics.Should().NotBeNull();
    }
}
