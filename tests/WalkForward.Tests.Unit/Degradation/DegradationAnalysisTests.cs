using FluentAssertions;
using NUnit.Framework;
using WalkForward.Degradation;
using WalkForward.GridSearch;

namespace WalkForward.Tests.Unit.Degradation;

[TestFixture]
public class DegradationAnalysisTests
{
    private const int DataPoints = 10000;
    private static readonly TimeSpan Frequency = TimeSpan.FromMinutes(15);

    private static GridSearchResult BuildGrid()
    {
        return new FoldBuilder()
            .WithDataPoints(DataPoints)
            .WithDataFrequency(Frequency)
            .GridSearch()
            .WithTrainWindows(TimeSpan.FromDays(30), TimeSpan.FromDays(60), TimeSpan.FromDays(90))
            .WithTestWindows(TimeSpan.FromDays(7), TimeSpan.FromDays(14))
            .BackwardLooking()
            .WithMinimumFolds(1)
            .Evaluate(fold => fold.FoldIndex + 1.0)
            .Build();
    }

    // ── ForGrid: result count ──
    [Test]
    public void ForGrid_ReturnsOneResultPerCell()
    {
        var grid = BuildGrid();

        var results = DegradationAnalysis.ForGrid(
            grid,
            DataPoints,
            Frequency,
            FoldMode.BackwardLooking,
            fold => fold.FoldIndex + 1.0,
            fold => (fold.FoldIndex + 1.0) * 0.5);

        results.Should().HaveCount(grid.Cells.Count);
    }

    // ── ForGrid: cell reference matching ──
    [Test]
    public void ForGrid_EachResultHasMatchingCellReference()
    {
        var grid = BuildGrid();

        var results = DegradationAnalysis.ForGrid(
            grid,
            DataPoints,
            Frequency,
            FoldMode.BackwardLooking,
            fold => fold.FoldIndex + 1.0,
            fold => (fold.FoldIndex + 1.0) * 0.5);

        for (var i = 0; i < results.Count; i++)
        {
            results[i].Cell.TrainWindow.Should().Be(grid.Cells[i].TrainWindow);
            results[i].Cell.TestWindow.Should().Be(grid.Cells[i].TestWindow);
        }
    }

    // ── ForGrid: degradation per cell ──
    [Test]
    public void ForGrid_ComputesDegradationPerCell()
    {
        var grid = BuildGrid();

        var results = DegradationAnalysis.ForGrid(
            grid,
            DataPoints,
            Frequency,
            FoldMode.BackwardLooking,
            fold => fold.FoldIndex + 1.0,
            fold => (fold.FoldIndex + 1.0) * 0.5);

        foreach (var (_, degradation) in results)
        {
            degradation.FoldResults.Should().NotBeEmpty();
            degradation.InSampleMeanFitness.Should().BeGreaterThan(0);
            degradation.OutOfSampleMeanFitness.Should().BeGreaterThan(0);
            degradation.WalkForwardEfficiency.Should().BeApproximately(0.5, 0.01);
            degradation.DegradationPercent.Should().BeApproximately(50.0, 1.0);
        }
    }

    // ── ForGrid: empty grid ──
    [Test]
    public void ForGrid_EmptyGrid_ReturnsEmptyList()
    {
        var emptyGrid = new GridSearchResult { Cells = [] };

        var results = DegradationAnalysis.ForGrid(
            emptyGrid,
            DataPoints,
            Frequency,
            FoldMode.BackwardLooking,
            fold => fold.FoldIndex + 1.0,
            fold => (fold.FoldIndex + 1.0) * 0.5);

        results.Should().BeEmpty();
    }

    // ── ForGrid: cancellation (pre-cancelled) ──
    [Test]
    public void ForGrid_CancelledToken_Throws()
    {
        var grid = BuildGrid();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var act = () => DegradationAnalysis.ForGrid(
            grid,
            DataPoints,
            Frequency,
            FoldMode.BackwardLooking,
            fold => fold.FoldIndex + 1.0,
            fold => (fold.FoldIndex + 1.0) * 0.5,
            cancellationToken: cts.Token);

        act.Should().Throw<OperationCanceledException>();
    }

    // ── ForGrid: cancellation between cells ──
    [Test]
    public void ForGrid_CancelBetweenCells_Throws()
    {
        var grid = BuildGrid();
        grid.Cells.Count.Should().BeGreaterThan(1, "grid must have multiple cells for this test");

        using var cts = new CancellationTokenSource();
        var callCount = 0;

        var act = () => DegradationAnalysis.ForGrid(
            grid,
            DataPoints,
            Frequency,
            FoldMode.BackwardLooking,
            fold =>
            {
                callCount++;
                if (callCount > 1)
                {
                    cts.Cancel();
                }

                return fold.FoldIndex + 1.0;
            },
            fold => (fold.FoldIndex + 1.0) * 0.5,
            cancellationToken: cts.Token);

        act.Should().Throw<OperationCanceledException>();
    }
}
