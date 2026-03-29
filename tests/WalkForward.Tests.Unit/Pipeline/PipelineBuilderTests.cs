using FluentAssertions;
using NUnit.Framework;
using WalkForward.GridSearch;
using WalkForward.Pipeline;
using WalkForward.Scoring;

namespace WalkForward.Tests.Unit.Pipeline;

[TestFixture]
public class PipelineBuilderTests
{
    private const int DataPoints = 10000;
    private static readonly TimeSpan Frequency = TimeSpan.FromMinutes(15);

    // --- Helpers ---
    private static PipelineBuilder CreateBuilder() =>
        new FoldBuilder()
            .WithDataPoints(DataPoints)
            .WithDataFrequency(Frequency)
            .Pipeline();

    private static void ConfigureCoarseScan(GridSearchBuilder grid) =>
        grid
            .WithTrainWindows(TimeSpan.FromDays(30), TimeSpan.FromDays(60))
            .WithTestWindows(TimeSpan.FromDays(7), TimeSpan.FromDays(14))
            .BackwardLooking()
            .Evaluate(fold => fold.TrainLength > 0 ? 0.8 : 0.5);

    // --- PIPE-01: Multi-stage pipeline ---
    [Test]
    public void Build_FullPipeline_ReturnsResultWithAllStages()
    {
        var scorer = new CompositeScorer().WithWeights(0.6, 0.25, 0.15);

        var result = CreateBuilder()
            .CoarseScan(ConfigureCoarseScan)
            .Score(scorer)
            .TopCandidates(3)
            .Validate(deg => deg
                .BackwardLooking()
                .EvaluateInSample(fold => 0.9)
                .EvaluateOutOfSample(fold => 0.7))
            .Build();

        result.CoarseScanResult.Should().NotBeNull();
        result.ScoredCells.Should().NotBeEmpty();
        result.TopCandidates.Count.Should().BeLessThanOrEqualTo(3);
        result.ValidationResults.Should().NotBeEmpty();
        result.Winner.Should().NotBeNull();
    }

    [Test]
    public void Build_NoCoarseScan_Throws()
    {
        var act = () => CreateBuilder().Build();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*CoarseScan*");
    }

    [Test]
    public void Build_ValidateStage_SetsPerCandidateWindows()
    {
        var result = CreateBuilder()
            .CoarseScan(ConfigureCoarseScan)
            .TopCandidates(2)
            .Validate(deg => deg
                .BackwardLooking()
                .EvaluateInSample(fold => 0.9)
                .EvaluateOutOfSample(fold => 0.7))
            .Build();

        foreach (var kvp in result.ValidationResults)
        {
            var candidate = kvp.Key;
            result.TopCandidates.Should().Contain(candidate);
        }

        // Each validation was done with the candidate's own window sizes
        result.ValidationResults.Keys.Select(c => c.TrainWindow)
            .Should().BeSubsetOf(result.TopCandidates.Select(c => c.TrainWindow));
    }

    // --- PIPE-02: Single-stage and partial pipelines ---
    [Test]
    public void Build_CoarseScanOnly_ReturnsGridResult()
    {
        var result = CreateBuilder()
            .CoarseScan(ConfigureCoarseScan)
            .Build();

        result.CoarseScanResult.Should().NotBeNull();
        result.CoarseScanResult.Cells.Should().NotBeEmpty();
        result.ScoredCells.Should().BeEquivalentTo(result.CoarseScanResult.Cells);
        result.TopCandidates.Should().BeEquivalentTo(result.ScoredCells);
        result.ValidationResults.Should().BeEmpty();
        result.Winner.Should().Be(result.ScoredCells[0]);
    }

    [Test]
    public void Build_CoarseScanAndScore_ReturnsScoredCells()
    {
        var scorer = new CompositeScorer().WithWeights(0.6, 0.25, 0.15);

        var result = CreateBuilder()
            .CoarseScan(ConfigureCoarseScan)
            .Score(scorer)
            .Build();

        result.ScoredCells.Should().NotBeEmpty();
        result.ScoredCells.Should().OnlyContain(c => c.CompositeScore > 0);
        result.TopCandidates.Should().BeEquivalentTo(result.ScoredCells);
    }

    [Test]
    public void Build_CoarseScanAndTopCandidates_FiltersCorrectly()
    {
        var result = CreateBuilder()
            .CoarseScan(ConfigureCoarseScan)
            .TopCandidates(2)
            .Build();

        result.TopCandidates.Count.Should().BeLessThanOrEqualTo(2);
    }

    // --- PIPE-03: Progress reporting ---
    [Test]
    public void Build_ReportsProgress_AcrossStages()
    {
        var reports = new List<PipelineProgress>();
        var progress = new SynchronousProgress<PipelineProgress>(r => reports.Add(r));
        var scorer = new CompositeScorer().WithWeights(0.6, 0.25, 0.15);

        CreateBuilder()
            .CoarseScan(ConfigureCoarseScan)
            .Score(scorer)
            .TopCandidates(3)
            .Validate(deg => deg
                .BackwardLooking()
                .EvaluateInSample(fold => 0.9)
                .EvaluateOutOfSample(fold => 0.7))
            .Build(CancellationToken.None, progress);

        reports.Should().NotBeEmpty();
        reports[0].StageName.Should().Be("CoarseScan");
        reports[0].StageIndex.Should().Be(0);
        reports.Last().OverallPercent.Should().BeApproximately(100.0, 0.1);
        reports.Should().OnlyContain(r => r.TotalStages == 4);
    }

    [Test]
    public void Build_NullProgress_DoesNotThrow()
    {
        var act = () => CreateBuilder()
            .CoarseScan(ConfigureCoarseScan)
            .Build(CancellationToken.None, null);

        act.Should().NotThrow();
    }

    // --- Edge cases ---
    [Test]
    public void Build_EmptyGrid_ReturnsEmptyResult()
    {
        // Use window sizes larger than the data to get 0 viable cells
        var result = CreateBuilder()
            .CoarseScan(grid => grid
                .WithTrainWindows(TimeSpan.FromDays(500))
                .WithTestWindows(TimeSpan.FromDays(500))
                .BackwardLooking()
                .Evaluate(fold => 0.8))
            .Build();

        result.Winner.Should().BeNull();
        result.TopCandidates.Should().BeEmpty();
        result.ValidationResults.Should().BeEmpty();
    }

    [Test]
    public void Build_CancellationRequested_Throws()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var act = () => CreateBuilder()
            .CoarseScan(ConfigureCoarseScan)
            .Build(cts.Token);

        act.Should().Throw<OperationCanceledException>();
    }

    [Test]
    public void Build_WinnerSelection_ByWalkForwardEfficiency()
    {
        // Use different fitness for different window sizes so WFE varies
        var result = CreateBuilder()
            .CoarseScan(grid => grid
                .WithTrainWindows(TimeSpan.FromDays(30), TimeSpan.FromDays(60))
                .WithTestWindows(TimeSpan.FromDays(7))
                .BackwardLooking()
                .Evaluate(fold => fold.TrainLength > 3000 ? 0.9 : 0.6))
            .TopCandidates(2)
            .Validate(deg => deg
                .BackwardLooking()
                .EvaluateInSample(fold => fold.TrainLength > 3000 ? 0.9 : 0.6)
                .EvaluateOutOfSample(fold => fold.TrainLength > 3000 ? 0.85 : 0.3))
            .Build();

        result.Winner.Should().NotBeNull();

        // Winner should be the candidate with highest WalkForwardEfficiency
        var bestWfe = result.ValidationResults
            .OrderByDescending(kvp => kvp.Value.WalkForwardEfficiency)
            .First().Key;
        result.Winner.Should().Be(bestWfe);
    }

    [Test]
    public void Pipeline_ReturnsPipelineBuilder()
    {
        var builder = new FoldBuilder()
            .WithDataPoints(DataPoints)
            .WithDataFrequency(Frequency)
            .Pipeline();

        builder.Should().NotBeNull();
        builder.Should().BeOfType<PipelineBuilder>();
    }

    // --- Helper: synchronous IProgress<T> to avoid SynchronizationContext issues ---
    private sealed class SynchronousProgress<T> : IProgress<T>
    {
        private readonly Action<T> _handler;

        public SynchronousProgress(Action<T> handler)
        {
            _handler = handler;
        }

        public void Report(T value) => _handler(value);
    }
}
