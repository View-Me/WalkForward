using FluentAssertions;
using NUnit.Framework;
using WalkForward.Degradation;

namespace WalkForward.Tests.Unit.Degradation;

[TestFixture]
public class DegradationBuilderTests
{
    private const int DataPoints = 10000;
    private static readonly TimeSpan Frequency = TimeSpan.FromMinutes(15);

    // ── Builder creation and fluent chaining ──
    [Test]
    public void Degrade_ReturnsDegradationBuilder()
    {
        var builder = new FoldBuilder()
            .WithDataPoints(DataPoints)
            .WithDataFrequency(Frequency)
            .Degrade();

        builder.Should().NotBeNull();
        builder.Should().BeOfType<DegradationBuilder>();
    }

    [Test]
    public void EvaluateInSample_ReturnsSameBuilder()
    {
        var builder = new FoldBuilder()
            .WithDataPoints(DataPoints)
            .WithDataFrequency(Frequency)
            .Degrade();

        var result = builder.EvaluateInSample(_ => 1.0);

        result.Should().BeSameAs(builder);
    }

    [Test]
    public void EvaluateOutOfSample_ReturnsSameBuilder()
    {
        var builder = new FoldBuilder()
            .WithDataPoints(DataPoints)
            .WithDataFrequency(Frequency)
            .Degrade();

        var result = builder.EvaluateOutOfSample(_ => 1.0);

        result.Should().BeSameAs(builder);
    }

    // ── Validation: missing required configuration ──
    [Test]
    public void Build_NoModeSet_Throws()
    {
        var builder = new FoldBuilder()
            .WithDataPoints(DataPoints)
            .WithDataFrequency(Frequency)
            .Degrade()
            .WithTrainingWindow(TimeSpan.FromDays(30))
            .WithTestWindow(TimeSpan.FromDays(7))
            .EvaluateInSample(_ => 1.0)
            .EvaluateOutOfSample(_ => 1.0);

        var act = () => builder.Build();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*BackwardLooking or ForwardLooking*");
    }

    [Test]
    public void Build_NoInSampleCallback_Throws()
    {
        var builder = new FoldBuilder()
            .WithDataPoints(DataPoints)
            .WithDataFrequency(Frequency)
            .Degrade()
            .WithTrainingWindow(TimeSpan.FromDays(30))
            .WithTestWindow(TimeSpan.FromDays(7))
            .BackwardLooking()
            .EvaluateOutOfSample(_ => 1.0);

        var act = () => builder.Build();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*EvaluateInSample*");
    }

    [Test]
    public void Build_NoOutOfSampleCallback_Throws()
    {
        var builder = new FoldBuilder()
            .WithDataPoints(DataPoints)
            .WithDataFrequency(Frequency)
            .Degrade()
            .WithTrainingWindow(TimeSpan.FromDays(30))
            .WithTestWindow(TimeSpan.FromDays(7))
            .BackwardLooking()
            .EvaluateInSample(_ => 1.0);

        var act = () => builder.Build();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*EvaluateOutOfSample*");
    }

    [Test]
    public void Build_NoTrainingWindow_Throws()
    {
        var builder = new FoldBuilder()
            .WithDataPoints(DataPoints)
            .WithDataFrequency(Frequency)
            .Degrade()
            .WithTestWindow(TimeSpan.FromDays(7))
            .BackwardLooking()
            .EvaluateInSample(_ => 1.0)
            .EvaluateOutOfSample(_ => 1.0);

        var act = () => builder.Build();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*WithTrainingWindow*");
    }

    [Test]
    public void Build_NoTestWindow_Throws()
    {
        var builder = new FoldBuilder()
            .WithDataPoints(DataPoints)
            .WithDataFrequency(Frequency)
            .Degrade()
            .WithTrainingWindow(TimeSpan.FromDays(30))
            .BackwardLooking()
            .EvaluateInSample(_ => 1.0)
            .EvaluateOutOfSample(_ => 1.0);

        var act = () => builder.Build();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*WithTestWindow*");
    }

    // ── Callback invocation ──
    [Test]
    public void Build_InvokesBothCallbacksPerFold()
    {
        var isCallCount = 0;
        var oosCallCount = 0;

        // 10000 points, 15-min, backward-looking
        // 30d train = 2880 points, 7d test = 672 points
        // maxFolds = floor((10000 - 2880) / 672) = 10
        var result = new FoldBuilder()
            .WithDataPoints(DataPoints)
            .WithDataFrequency(Frequency)
            .Degrade()
            .WithTrainingWindow(TimeSpan.FromDays(30))
            .WithTestWindow(TimeSpan.FromDays(7))
            .BackwardLooking()
            .EvaluateInSample(_ =>
            {
                isCallCount++;
                return 1.0;
            })
            .EvaluateOutOfSample(_ =>
            {
                oosCallCount++;
                return 0.5;
            })
            .Build();

        var foldCount = result.FoldResults.Count;
        foldCount.Should().BeGreaterThan(0);
        isCallCount.Should().Be(foldCount);
        oosCallCount.Should().Be(foldCount);
    }

    // ── Per-fold results ──
    [Test]
    public void Build_PerFoldResults_ContainCorrectValues()
    {
        // IS returns fold.FoldIndex + 1.0, OOS returns (fold.FoldIndex + 1.0) * 0.5
        var result = new FoldBuilder()
            .WithDataPoints(DataPoints)
            .WithDataFrequency(Frequency)
            .Degrade()
            .WithTrainingWindow(TimeSpan.FromDays(30))
            .WithTestWindow(TimeSpan.FromDays(7))
            .BackwardLooking()
            .EvaluateInSample(fold => fold.FoldIndex + 1.0)
            .EvaluateOutOfSample(fold => (fold.FoldIndex + 1.0) * 0.5)
            .Build();

        result.FoldResults.Should().NotBeEmpty();

        for (var i = 0; i < result.FoldResults.Count; i++)
        {
            var foldResult = result.FoldResults[i];
            foldResult.FoldIndex.Should().Be(i);
            foldResult.InSampleFitness.Should().BeApproximately(i + 1.0, 0.0001);
            foldResult.OutOfSampleFitness.Should().BeApproximately((i + 1.0) * 0.5, 0.0001);
        }
    }

    // ── Aggregate metrics ──
    [Test]
    public void Build_ComputesInSampleMeanFitness()
    {
        var result = new FoldBuilder()
            .WithDataPoints(DataPoints)
            .WithDataFrequency(Frequency)
            .Degrade()
            .WithTrainingWindow(TimeSpan.FromDays(30))
            .WithTestWindow(TimeSpan.FromDays(7))
            .BackwardLooking()
            .EvaluateInSample(fold => fold.FoldIndex + 1.0)
            .EvaluateOutOfSample(fold => (fold.FoldIndex + 1.0) * 0.5)
            .Build();

        var expectedMean = result.FoldResults.Average(f => f.InSampleFitness);
        result.InSampleMeanFitness.Should().BeApproximately(expectedMean, 0.0001);
    }

    [Test]
    public void Build_ComputesOutOfSampleMeanFitness()
    {
        var result = new FoldBuilder()
            .WithDataPoints(DataPoints)
            .WithDataFrequency(Frequency)
            .Degrade()
            .WithTrainingWindow(TimeSpan.FromDays(30))
            .WithTestWindow(TimeSpan.FromDays(7))
            .BackwardLooking()
            .EvaluateInSample(fold => fold.FoldIndex + 1.0)
            .EvaluateOutOfSample(fold => (fold.FoldIndex + 1.0) * 0.5)
            .Build();

        var expectedMean = result.FoldResults.Average(f => f.OutOfSampleFitness);
        result.OutOfSampleMeanFitness.Should().BeApproximately(expectedMean, 0.0001);
    }

    [Test]
    public void Build_DegradationPercent_NormalCase()
    {
        // All folds return IS=2.0, OOS=1.0 -> DegradationPercent = (1 - 1/2) * 100 = 50%
        var result = new FoldBuilder()
            .WithDataPoints(DataPoints)
            .WithDataFrequency(Frequency)
            .Degrade()
            .WithTrainingWindow(TimeSpan.FromDays(30))
            .WithTestWindow(TimeSpan.FromDays(7))
            .BackwardLooking()
            .EvaluateInSample(_ => 2.0)
            .EvaluateOutOfSample(_ => 1.0)
            .Build();

        result.DegradationPercent.Should().BeApproximately(50.0, 0.0001);
    }

    [Test]
    public void Build_WalkForwardEfficiency_NormalCase()
    {
        // All folds return IS=2.0, OOS=1.0 -> WFE = 1/2 = 0.5
        var result = new FoldBuilder()
            .WithDataPoints(DataPoints)
            .WithDataFrequency(Frequency)
            .Degrade()
            .WithTrainingWindow(TimeSpan.FromDays(30))
            .WithTestWindow(TimeSpan.FromDays(7))
            .BackwardLooking()
            .EvaluateInSample(_ => 2.0)
            .EvaluateOutOfSample(_ => 1.0)
            .Build();

        result.WalkForwardEfficiency.Should().BeApproximately(0.5, 0.0001);
    }

    // ── Edge cases ──
    [Test]
    public void Build_ZeroISMean_NoDivisionByZero()
    {
        // Both callbacks return 0.0 -> DegradationPercent=0.0, WFE=0.0
        var result = new FoldBuilder()
            .WithDataPoints(DataPoints)
            .WithDataFrequency(Frequency)
            .Degrade()
            .WithTrainingWindow(TimeSpan.FromDays(30))
            .WithTestWindow(TimeSpan.FromDays(7))
            .BackwardLooking()
            .EvaluateInSample(_ => 0.0)
            .EvaluateOutOfSample(_ => 0.0)
            .Build();

        result.DegradationPercent.Should().Be(0.0);
        result.WalkForwardEfficiency.Should().Be(0.0);
    }

    [Test]
    public void Build_PerfectMatch_NoDegradation()
    {
        // IS == OOS -> DegradationPercent=0.0, WFE=1.0
        var result = new FoldBuilder()
            .WithDataPoints(DataPoints)
            .WithDataFrequency(Frequency)
            .Degrade()
            .WithTrainingWindow(TimeSpan.FromDays(30))
            .WithTestWindow(TimeSpan.FromDays(7))
            .BackwardLooking()
            .EvaluateInSample(fold => fold.FoldIndex + 1.0)
            .EvaluateOutOfSample(fold => fold.FoldIndex + 1.0)
            .Build();

        result.DegradationPercent.Should().BeApproximately(0.0, 0.0001);
        result.WalkForwardEfficiency.Should().BeApproximately(1.0, 0.0001);
    }

    [Test]
    public void Build_OOSExceedsIS_NegativeDegradation()
    {
        // IS=1.0, OOS=2.0 -> DegradationPercent = (1 - 2/1) * 100 = -100%
        var result = new FoldBuilder()
            .WithDataPoints(DataPoints)
            .WithDataFrequency(Frequency)
            .Degrade()
            .WithTrainingWindow(TimeSpan.FromDays(30))
            .WithTestWindow(TimeSpan.FromDays(7))
            .BackwardLooking()
            .EvaluateInSample(_ => 1.0)
            .EvaluateOutOfSample(_ => 2.0)
            .Build();

        result.DegradationPercent.Should().BeLessThan(0);
        result.WalkForwardEfficiency.Should().BeGreaterThan(1.0);
    }

    // ── Fold modes ──
    [Test]
    public void Build_BackwardLooking_ProducesResults()
    {
        var result = new FoldBuilder()
            .WithDataPoints(DataPoints)
            .WithDataFrequency(Frequency)
            .Degrade()
            .WithTrainingWindow(TimeSpan.FromDays(30))
            .WithTestWindow(TimeSpan.FromDays(7))
            .BackwardLooking()
            .EvaluateInSample(_ => 1.0)
            .EvaluateOutOfSample(_ => 0.5)
            .Build();

        result.FoldResults.Should().NotBeEmpty();
    }

    [Test]
    public void Build_ForwardLooking_ProducesResults()
    {
        var result = new FoldBuilder()
            .WithDataPoints(DataPoints)
            .WithDataFrequency(Frequency)
            .Degrade()
            .WithTrainingWindow(TimeSpan.FromDays(30))
            .WithTestWindow(TimeSpan.FromDays(7))
            .ForwardLooking()
            .EvaluateInSample(_ => 1.0)
            .EvaluateOutOfSample(_ => 0.5)
            .Build();

        result.FoldResults.Should().NotBeEmpty();
    }

    // ── Zero folds scenario ──
    [Test]
    public void Build_InsufficientData_ZeroFolds()
    {
        // Use a very large training window: 100d = 9600 points
        // Plus 7d test = 672 points. Total = 10272 > 10000 -> 0 folds
        var result = new FoldBuilder()
            .WithDataPoints(DataPoints)
            .WithDataFrequency(Frequency)
            .Degrade()
            .WithTrainingWindow(TimeSpan.FromDays(100))
            .WithTestWindow(TimeSpan.FromDays(7))
            .BackwardLooking()
            .EvaluateInSample(_ => 1.0)
            .EvaluateOutOfSample(_ => 0.5)
            .Build();

        result.FoldResults.Should().BeEmpty();
        result.InSampleMeanFitness.Should().Be(0.0);
        result.OutOfSampleMeanFitness.Should().Be(0.0);
        result.DegradationPercent.Should().Be(0.0);
        result.WalkForwardEfficiency.Should().Be(0.0);
    }

    // ── Cancellation ──
    [Test]
    public void Build_CancelledToken_Throws()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var builder = new FoldBuilder()
            .WithDataPoints(DataPoints)
            .WithDataFrequency(Frequency)
            .Degrade()
            .WithTrainingWindow(TimeSpan.FromDays(30))
            .WithTestWindow(TimeSpan.FromDays(7))
            .BackwardLooking()
            .EvaluateInSample(_ => 1.0)
            .EvaluateOutOfSample(_ => 0.5);

        var act = () => builder.Build(cts.Token);

        act.Should().Throw<OperationCanceledException>();
    }

    // ── Optional configuration ──
    [Test]
    public void Build_WithWarmup_ReducesFolds()
    {
        var withWarmup = new FoldBuilder()
            .WithDataPoints(DataPoints)
            .WithDataFrequency(Frequency)
            .Degrade()
            .WithTrainingWindow(TimeSpan.FromDays(30))
            .WithTestWindow(TimeSpan.FromDays(7))
            .BackwardLooking()
            .WithWarmup(3000)
            .EvaluateInSample(_ => 1.0)
            .EvaluateOutOfSample(_ => 0.5)
            .Build();

        var withoutWarmup = new FoldBuilder()
            .WithDataPoints(DataPoints)
            .WithDataFrequency(Frequency)
            .Degrade()
            .WithTrainingWindow(TimeSpan.FromDays(30))
            .WithTestWindow(TimeSpan.FromDays(7))
            .BackwardLooking()
            .EvaluateInSample(_ => 1.0)
            .EvaluateOutOfSample(_ => 0.5)
            .Build();

        withWarmup.FoldResults.Count.Should().BeLessThan(withoutWarmup.FoldResults.Count);
    }

    [Test]
    public void Build_WithEmbargo_ReducesFolds()
    {
        var withEmbargo = new FoldBuilder()
            .WithDataPoints(DataPoints)
            .WithDataFrequency(Frequency)
            .Degrade()
            .WithTrainingWindow(TimeSpan.FromDays(30))
            .WithTestWindow(TimeSpan.FromDays(7))
            .BackwardLooking()
            .WithEmbargo(TimeSpan.FromDays(10))
            .EvaluateInSample(_ => 1.0)
            .EvaluateOutOfSample(_ => 0.5)
            .Build();

        var withoutEmbargo = new FoldBuilder()
            .WithDataPoints(DataPoints)
            .WithDataFrequency(Frequency)
            .Degrade()
            .WithTrainingWindow(TimeSpan.FromDays(30))
            .WithTestWindow(TimeSpan.FromDays(7))
            .BackwardLooking()
            .EvaluateInSample(_ => 1.0)
            .EvaluateOutOfSample(_ => 0.5)
            .Build();

        withEmbargo.FoldResults.Count.Should().BeLessThan(withoutEmbargo.FoldResults.Count);
    }

    [Test]
    public void Build_WithMaxFolds_LimitsFolds()
    {
        var result = new FoldBuilder()
            .WithDataPoints(DataPoints)
            .WithDataFrequency(Frequency)
            .Degrade()
            .WithTrainingWindow(TimeSpan.FromDays(30))
            .WithTestWindow(TimeSpan.FromDays(7))
            .BackwardLooking()
            .WithMaxFolds(3)
            .EvaluateInSample(_ => 1.0)
            .EvaluateOutOfSample(_ => 0.5)
            .Build();

        result.FoldResults.Count.Should().BeLessThanOrEqualTo(3);
    }
}
