using FluentAssertions;
using NUnit.Framework;

namespace WalkForward.Tests.Unit.Degradation;

[TestFixture]
public class DegradationSegmentTests
{
    private const int DataPoints = 10000;
    private static readonly TimeSpan Frequency = TimeSpan.FromMinutes(15);
    private static readonly string[] DualLabelAb = ["A", "B"];

    // --- SEGM-01: Labeler callback invocation ---
    [Test]
    public void Build_WithLabeler_InvokesLabelerPerFold()
    {
        var labeledFolds = new List<Fold>();

        var result = new FoldBuilder()
            .WithDataPoints(DataPoints)
            .WithDataFrequency(Frequency)
            .Degrade()
            .WithTrainingWindow(TimeSpan.FromDays(30))
            .WithTestWindow(TimeSpan.FromDays(7))
            .BackwardLooking()
            .EvaluateInSample(_ => 1.0)
            .EvaluateOutOfSample(_ => 0.8)
            .WithLabeler(fold =>
            {
                labeledFolds.Add(fold);
                return ["A"];
            })
            .Build();

        var foldCount = result.FoldResults.Count;
        foldCount.Should().BeGreaterThan(0);
        labeledFolds.Should().HaveCount(foldCount, "labeler should be invoked once per fold");
    }

    [Test]
    public void Build_WithLabeler_MultiLabel_FoldContributesToMultipleSegments()
    {
        var result = new FoldBuilder()
            .WithDataPoints(DataPoints)
            .WithDataFrequency(Frequency)
            .Degrade()
            .WithTrainingWindow(TimeSpan.FromDays(30))
            .WithTestWindow(TimeSpan.FromDays(7))
            .BackwardLooking()
            .EvaluateInSample(_ => 1.0)
            .EvaluateOutOfSample(_ => 0.8)
            .WithLabeler(_ => DualLabelAb)
            .Build();

        result.SegmentResults.Should().ContainKey("A");
        result.SegmentResults.Should().ContainKey("B");

        // Both segments should have the same fold count and metrics since all folds get both labels
        result.SegmentResults["A"].FoldResults.Count.Should()
            .Be(result.SegmentResults["B"].FoldResults.Count);
    }

    [Test]
    public void Build_WithoutLabeler_SegmentResultsEmpty()
    {
        var result = new FoldBuilder()
            .WithDataPoints(DataPoints)
            .WithDataFrequency(Frequency)
            .Degrade()
            .WithTrainingWindow(TimeSpan.FromDays(30))
            .WithTestWindow(TimeSpan.FromDays(7))
            .BackwardLooking()
            .EvaluateInSample(_ => 1.0)
            .EvaluateOutOfSample(_ => 0.8)
            .Build();

        result.SegmentResults.Should().BeEmpty(
            "no labeler set should produce empty SegmentResults");
    }

    // --- SEGM-02: Per-segment degradation computation ---
    [Test]
    public void Build_WithLabeler_SegmentResultsContainsPerSegmentDegradation()
    {
        // All folds labeled "X" with IS=1.0, OOS=0.8
        // Expected per-segment: IS mean=1.0, OOS mean=0.8, degradation=20%, WFE=0.8
        var result = new FoldBuilder()
            .WithDataPoints(DataPoints)
            .WithDataFrequency(Frequency)
            .Degrade()
            .WithTrainingWindow(TimeSpan.FromDays(30))
            .WithTestWindow(TimeSpan.FromDays(7))
            .BackwardLooking()
            .EvaluateInSample(_ => 1.0)
            .EvaluateOutOfSample(_ => 0.8)
            .WithLabeler(_ => ["X"])
            .Build();

        result.SegmentResults.Should().ContainKey("X");
        var segX = result.SegmentResults["X"];
        segX.InSampleMeanFitness.Should().BeApproximately(1.0, 0.0001);
        segX.OutOfSampleMeanFitness.Should().BeApproximately(0.8, 0.0001);
        segX.DegradationPercent.Should().BeApproximately(20.0, 0.0001);
        segX.WalkForwardEfficiency.Should().BeApproximately(0.8, 0.0001);
    }

    [Test]
    public void Build_WithLabeler_PerSegmentDegradationMetrics()
    {
        // Assign "high" to even folds (IS=2.0, OOS=1.0 -> 50% degradation, WFE=0.5)
        // Assign "low" to odd folds (IS=1.0, OOS=0.9 -> 10% degradation, WFE=0.9)
        var result = new FoldBuilder()
            .WithDataPoints(DataPoints)
            .WithDataFrequency(Frequency)
            .Degrade()
            .WithTrainingWindow(TimeSpan.FromDays(30))
            .WithTestWindow(TimeSpan.FromDays(7))
            .BackwardLooking()
            .EvaluateInSample(fold => fold.FoldIndex % 2 == 0 ? 2.0 : 1.0)
            .EvaluateOutOfSample(fold => fold.FoldIndex % 2 == 0 ? 1.0 : 0.9)
            .WithLabeler(fold => fold.FoldIndex % 2 == 0 ? ["high"] : ["low"])
            .Build();

        result.SegmentResults.Should().ContainKey("high");
        result.SegmentResults.Should().ContainKey("low");

        var high = result.SegmentResults["high"];
        high.InSampleMeanFitness.Should().BeApproximately(2.0, 0.0001);
        high.OutOfSampleMeanFitness.Should().BeApproximately(1.0, 0.0001);
        high.DegradationPercent.Should().BeApproximately(50.0, 0.0001);
        high.WalkForwardEfficiency.Should().BeApproximately(0.5, 0.0001);

        var low = result.SegmentResults["low"];
        low.InSampleMeanFitness.Should().BeApproximately(1.0, 0.0001);
        low.OutOfSampleMeanFitness.Should().BeApproximately(0.9, 0.0001);
        low.DegradationPercent.Should().BeApproximately(10.0, 0.0001);
        low.WalkForwardEfficiency.Should().BeApproximately(0.9, 0.0001);
    }

    [Test]
    public void Build_WithLabeler_EmptyLabels_FoldExcludedFromSegments()
    {
        var result = new FoldBuilder()
            .WithDataPoints(DataPoints)
            .WithDataFrequency(Frequency)
            .Degrade()
            .WithTrainingWindow(TimeSpan.FromDays(30))
            .WithTestWindow(TimeSpan.FromDays(7))
            .BackwardLooking()
            .EvaluateInSample(_ => 1.0)
            .EvaluateOutOfSample(_ => 0.8)
            .WithLabeler(_ => Array.Empty<string>())
            .Build();

        result.SegmentResults.Should().BeEmpty(
            "empty label enumerables should result in no segments");

        // Overall result still has correct metrics
        result.FoldResults.Should().NotBeEmpty();
        result.InSampleMeanFitness.Should().BeApproximately(1.0, 0.0001);
        result.OutOfSampleMeanFitness.Should().BeApproximately(0.8, 0.0001);
    }
}
