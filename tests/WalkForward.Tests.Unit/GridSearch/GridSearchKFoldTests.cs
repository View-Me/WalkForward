using FluentAssertions;
using NUnit.Framework;
using WalkForward.GridSearch;

namespace WalkForward.Tests.Unit.GridSearch;

[TestFixture]
public class GridSearchKFoldTests
{
    private const int DataPoints = 10000;
    private static readonly TimeSpan Frequency = TimeSpan.FromMinutes(15);

    // --- CVLD-01: WithInnerFolds builder method ---

    [Test]
    public void WithInnerFolds_ReturnsBuilderForChaining()
    {
        var builder = new FoldBuilder()
            .WithDataPoints(DataPoints)
            .WithDataFrequency(Frequency)
            .GridSearch();

        var result = builder.WithInnerFolds(5);

        result.Should().BeSameAs(builder);
    }

    [Test]
    public void Build_WithInnerFolds0_IdenticalToNoInnerFolds()
    {
        var withZero = new FoldBuilder()
            .WithDataPoints(DataPoints)
            .WithDataFrequency(Frequency)
            .GridSearch()
            .WithTrainWindows(TimeSpan.FromDays(30))
            .WithTestWindows(TimeSpan.FromDays(7))
            .BackwardLooking()
            .WithInnerFolds(0)
            .Evaluate(fold => fold.FoldIndex + 1.0)
            .Build();

        var without = new FoldBuilder()
            .WithDataPoints(DataPoints)
            .WithDataFrequency(Frequency)
            .GridSearch()
            .WithTrainWindows(TimeSpan.FromDays(30))
            .WithTestWindows(TimeSpan.FromDays(7))
            .BackwardLooking()
            .Evaluate(fold => fold.FoldIndex + 1.0)
            .Build();

        withZero.Cells[0].MeanFitness.Should().BeApproximately(without.Cells[0].MeanFitness, 0.0001);
    }

    [Test]
    public void Build_WithInnerFolds1_IdenticalToNoInnerFolds()
    {
        var withOne = new FoldBuilder()
            .WithDataPoints(DataPoints)
            .WithDataFrequency(Frequency)
            .GridSearch()
            .WithTrainWindows(TimeSpan.FromDays(30))
            .WithTestWindows(TimeSpan.FromDays(7))
            .BackwardLooking()
            .WithInnerFolds(1)
            .Evaluate(fold => fold.FoldIndex + 1.0)
            .Build();

        var without = new FoldBuilder()
            .WithDataPoints(DataPoints)
            .WithDataFrequency(Frequency)
            .GridSearch()
            .WithTrainWindows(TimeSpan.FromDays(30))
            .WithTestWindows(TimeSpan.FromDays(7))
            .BackwardLooking()
            .Evaluate(fold => fold.FoldIndex + 1.0)
            .Build();

        withOne.Cells[0].MeanFitness.Should().BeApproximately(without.Cells[0].MeanFitness, 0.0001);
    }

    [Test]
    public void Build_WithInnerFolds5_ChangesFitnessValues()
    {
        // With inner folds, the fitness callback receives inner sub-folds (with TestLength
        // reflecting sub-fold hold-out size), so returning TestLength gives a different
        // MeanFitness than the outer fold's TestLength.
        var withKFold = new FoldBuilder()
            .WithDataPoints(DataPoints)
            .WithDataFrequency(Frequency)
            .GridSearch()
            .WithTrainWindows(TimeSpan.FromDays(30))
            .WithTestWindows(TimeSpan.FromDays(7))
            .BackwardLooking()
            .WithInnerFolds(5)
            .Evaluate(fold => fold.TestLength)
            .Build();

        var withoutKFold = new FoldBuilder()
            .WithDataPoints(DataPoints)
            .WithDataFrequency(Frequency)
            .GridSearch()
            .WithTrainWindows(TimeSpan.FromDays(30))
            .WithTestWindows(TimeSpan.FromDays(7))
            .BackwardLooking()
            .Evaluate(fold => fold.TestLength)
            .Build();

        // Without K-fold, TestLength is the outer test window (672 points for 7d at 15min)
        // With K-fold, TestLength is sub-fold hold-out size (~576 for k=5, 2880/5=576)
        withKFold.Cells[0].MeanFitness.Should().NotBeApproximately(
            withoutKFold.Cells[0].MeanFitness, 1.0,
            "inner K-fold should change the fitness values compared to no inner folds");
    }

    // --- CVLD-02: Inner K-fold temporal ordering and weighted average ---

    [Test]
    public void Build_WithInnerFolds_TemporalOrdering()
    {
        var receivedFolds = new List<Fold>();

        var result = new FoldBuilder()
            .WithDataPoints(DataPoints)
            .WithDataFrequency(Frequency)
            .GridSearch()
            .WithTrainWindows(TimeSpan.FromDays(30))
            .WithTestWindows(TimeSpan.FromDays(7))
            .BackwardLooking()
            .WithInnerFolds(3)
            .Evaluate(fold =>
            {
                receivedFolds.Add(fold);
                return 1.0;
            })
            .Build();

        // Each outer fold produces 3 inner sub-folds.
        // Total callbacks = outerFoldCount * 3
        var outerFoldCount = result.Cells[0].FoldCount;
        receivedFolds.Should().HaveCount(outerFoldCount * 3);

        // Verify temporal ordering within each group of 3 sub-folds
        for (var outer = 0; outer < outerFoldCount; outer++)
        {
            var group = receivedFolds.Skip(outer * 3).Take(3).ToList();

            // Sequential: each sub-fold starts at or after the previous one ends
            group[0].TestStart.Should().BeLessThan(group[1].TestStart,
                "sub-folds should be temporally ordered (ascending TestStart)");
            group[1].TestStart.Should().BeLessThan(group[2].TestStart,
                "sub-folds should be temporally ordered (ascending TestStart)");

            // Non-overlapping: each sub-fold's TestEnd <= next sub-fold's TestStart
            group[0].TestEnd.Should().BeLessThanOrEqualTo(group[1].TestStart,
                "sub-folds should be non-overlapping");
            group[1].TestEnd.Should().BeLessThanOrEqualTo(group[2].TestStart,
                "sub-folds should be non-overlapping");
        }
    }

    [Test]
    public void Build_WithInnerFolds_WeightedAverageByFoldSize()
    {
        // Use K=7 on 30d train (2880 points): 2880/7 = 411 remainder 3
        // Sub-fold sizes: 411, 411, 411, 411, 411, 411, 414 (last absorbs remainder)
        // Callback returns fold.TestLength as fitness.
        // We capture all (TestLength, fitness) pairs and compute expected weighted average.
        var subFoldLengths = new List<int>();

        var result = new FoldBuilder()
            .WithDataPoints(DataPoints)
            .WithDataFrequency(Frequency)
            .GridSearch()
            .WithTrainWindows(TimeSpan.FromDays(30))
            .WithTestWindows(TimeSpan.FromDays(7))
            .BackwardLooking()
            .WithInnerFolds(7)
            .Evaluate(fold =>
            {
                subFoldLengths.Add(fold.TestLength);
                return fold.TestLength;
            })
            .Build();

        var outerFoldCount = result.Cells[0].FoldCount;

        // Verify we can compute the expected weighted average from captured sub-fold lengths
        // For each outer fold (group of 7 sub-folds), compute weighted average
        var expectedFitnesses = new List<double>();
        for (var outer = 0; outer < outerFoldCount; outer++)
        {
            var group = subFoldLengths.Skip(outer * 7).Take(7).ToList();
            var weightedSum = group.Sum(len => (double)len * len); // fitness=len, weight=len
            var totalWeight = group.Sum();
            expectedFitnesses.Add(weightedSum / totalWeight);
        }

        var expectedMean = expectedFitnesses.Average();
        result.Cells[0].MeanFitness.Should().BeApproximately(expectedMean, 0.001,
            "MeanFitness should reflect weighted average by fold size, not simple average");
    }

    [Test]
    public void Build_WithInnerFolds_LastSubFoldAbsorbsRemainder()
    {
        var receivedFolds = new List<Fold>();

        var result = new FoldBuilder()
            .WithDataPoints(DataPoints)
            .WithDataFrequency(Frequency)
            .GridSearch()
            .WithTrainWindows(TimeSpan.FromDays(30))
            .WithTestWindows(TimeSpan.FromDays(7))
            .BackwardLooking()
            .WithInnerFolds(7)
            .Evaluate(fold =>
            {
                receivedFolds.Add(fold);
                return 1.0;
            })
            .Build();

        var outerFoldCount = result.Cells[0].FoldCount;

        // For each outer fold group, the last sub-fold's TestEnd should equal
        // the outer fold's TrainEnd (no data points are dropped)
        for (var outer = 0; outer < outerFoldCount; outer++)
        {
            var group = receivedFolds.Skip(outer * 7).Take(7).ToList();
            var lastSubFold = group[^1];

            // 2880 / 7 = 411 remainder 3 -> last sub-fold has 414 points
            lastSubFold.TestLength.Should().Be(414,
                "last sub-fold should absorb remainder (2880 mod 7 = 3 extra points)");

            // First 6 sub-folds should each have 411 points
            for (var k = 0; k < 6; k++)
            {
                group[k].TestLength.Should().Be(411,
                    $"sub-fold {k} should have base size of 411");
            }
        }
    }

    [Test]
    public void Build_WithInnerFolds_TrainWindowTooSmall_FallsBackToSingleEvaluation()
    {
        // Use K=200 on a train window with only 96 points (1 day at 15-min frequency)
        // 96 / 200 = 0 -> falls back to single evaluation (same as no inner folds)
        var withLargeK = new FoldBuilder()
            .WithDataPoints(DataPoints)
            .WithDataFrequency(Frequency)
            .GridSearch()
            .WithTrainWindows(TimeSpan.FromDays(1))
            .WithTestWindows(TimeSpan.FromDays(0.5))
            .BackwardLooking()
            .WithInnerFolds(200)
            .Evaluate(fold => fold.FoldIndex + 1.0)
            .Build();

        var withoutInnerFolds = new FoldBuilder()
            .WithDataPoints(DataPoints)
            .WithDataFrequency(Frequency)
            .GridSearch()
            .WithTrainWindows(TimeSpan.FromDays(1))
            .WithTestWindows(TimeSpan.FromDays(0.5))
            .BackwardLooking()
            .Evaluate(fold => fold.FoldIndex + 1.0)
            .Build();

        withLargeK.Cells[0].MeanFitness.Should().BeApproximately(
            withoutInnerFolds.Cells[0].MeanFitness, 0.0001,
            "when train window is too small for K sub-folds, should fall back to single evaluation");
    }

    // --- D-03: Labeler runs on outer folds only ---

    [Test]
    public void Build_WithInnerFoldsAndLabeler_LabelerRunsOnOuterFoldsOnly()
    {
        var labelerCallCount = 0;

        var result = new FoldBuilder()
            .WithDataPoints(DataPoints)
            .WithDataFrequency(Frequency)
            .GridSearch()
            .WithTrainWindows(TimeSpan.FromDays(30))
            .WithTestWindows(TimeSpan.FromDays(7))
            .BackwardLooking()
            .WithInnerFolds(5)
            .Evaluate(_ => 1.0)
            .WithLabeler(fold =>
            {
                labelerCallCount++;
                return new[] { "A" };
            })
            .Build();

        var totalOuterFolds = result.Cells.Sum(c => c.FoldCount);

        // Labeler should be invoked once per outer fold, NOT once per inner sub-fold
        labelerCallCount.Should().Be(totalOuterFolds,
            "labeler should be called once per outer fold, not K times per outer fold");

        // If labeler ran on inner sub-folds, count would be totalOuterFolds * 5
        labelerCallCount.Should().NotBe(totalOuterFolds * 5,
            "labeler must NOT run on inner sub-folds");
    }
}
