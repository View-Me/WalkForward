using FluentAssertions;
using NUnit.Framework;
using WalkForward.GridSearch;

namespace WalkForward.Tests.Unit.GridSearch;

[TestFixture]
public class GridSearchBuilderTests
{
    private const int DataPoints = 10000;
    private static readonly TimeSpan Frequency = TimeSpan.FromMinutes(15);

    // --- GRID-01: Builder creation and window configuration ---
    [Test]
    public void GridSearch_ReturnsGridSearchBuilder()
    {
        var builder = new FoldBuilder()
            .WithDataPoints(DataPoints)
            .WithDataFrequency(Frequency)
            .GridSearch();

        builder.Should().NotBeNull();
        builder.Should().BeOfType<GridSearchBuilder>();
    }

    [Test]
    public void WithTrainWindows_TimeSpan_SetsWindows()
    {
        var builder = new FoldBuilder()
            .WithDataPoints(DataPoints)
            .WithDataFrequency(Frequency)
            .GridSearch();

        var result = builder.WithTrainWindows(TimeSpan.FromDays(30), TimeSpan.FromDays(60));

        result.Should().BeSameAs(builder);
    }

    [Test]
    public void WithTestWindows_TimeSpan_SetsWindows()
    {
        var builder = new FoldBuilder()
            .WithDataPoints(DataPoints)
            .WithDataFrequency(Frequency)
            .GridSearch();

        var result = builder.WithTestWindows(TimeSpan.FromDays(7), TimeSpan.FromDays(14));

        result.Should().BeSameAs(builder);
    }

    [Test]
    public void WithTrainWindows_Int_SetsWindows()
    {
        var builder = new FoldBuilder()
            .WithDataPoints(DataPoints)
            .WithDataFrequency(Frequency)
            .GridSearch();

        var result = builder.WithTrainWindows(2880, 5760);

        result.Should().BeSameAs(builder);
    }

    [Test]
    public void WithTestWindows_Int_SetsWindows()
    {
        var builder = new FoldBuilder()
            .WithDataPoints(DataPoints)
            .WithDataFrequency(Frequency)
            .GridSearch();

        var result = builder.WithTestWindows(672, 1344);

        result.Should().BeSameAs(builder);
    }

    [Test]
    public void WithTrainWindows_EmptyArray_Throws()
    {
        var builder = new FoldBuilder()
            .WithDataPoints(DataPoints)
            .WithDataFrequency(Frequency)
            .GridSearch();

        var act = () => builder.WithTrainWindows(Array.Empty<TimeSpan>());

        act.Should().Throw<ArgumentException>();
    }

    [Test]
    public void WithTestWindows_EmptyArray_Throws()
    {
        var builder = new FoldBuilder()
            .WithDataPoints(DataPoints)
            .WithDataFrequency(Frequency)
            .GridSearch();

        var act = () => builder.WithTestWindows(Array.Empty<TimeSpan>());

        act.Should().Throw<ArgumentException>();
    }

    // --- GRID-02: Grid evaluation ---
    [Test]
    public void Build_3x2Grid_Evaluates6Cells()
    {
        // 3 train windows x 2 test windows = 6 cells
        // 10000 points at 15-min frequency, backward-looking
        // Set minimum folds to 1 so all cells with >= 1 fold are included.
        // 90d/14d: floor((10000-8640)/1344)=1 fold, so all 6 cells pass min=1.
        var result = new FoldBuilder()
            .WithDataPoints(DataPoints)
            .WithDataFrequency(Frequency)
            .GridSearch()
            .WithTrainWindows(TimeSpan.FromDays(30), TimeSpan.FromDays(60), TimeSpan.FromDays(90))
            .WithTestWindows(TimeSpan.FromDays(7), TimeSpan.FromDays(14))
            .BackwardLooking()
            .WithMinimumFolds(1)
            .Evaluate(_ => 1.0)
            .Build();

        result.Cells.Should().HaveCount(6);
    }

    [Test]
    public void Build_InvokesFitnessPerFold()
    {
        var callCount = 0;

        // 10000 points, 15-min, backward-looking
        // 30d train = 2880 points, 7d test = 672 points
        // maxFolds = floor((10000 - 2880) / 672) = floor(7120 / 672) = 10
        var result = new FoldBuilder()
            .WithDataPoints(DataPoints)
            .WithDataFrequency(Frequency)
            .GridSearch()
            .WithTrainWindows(TimeSpan.FromDays(30))
            .WithTestWindows(TimeSpan.FromDays(7))
            .BackwardLooking()
            .Evaluate(_ =>
            {
                callCount++;
                return 1.0;
            })
            .Build();

        // Expected folds for 30d/7d backward-looking: 10
        callCount.Should().Be(result.Cells[0].FoldCount);
        callCount.Should().BeGreaterThan(0);
    }

    [Test]
    public void Build_ForwardLooking_ProducesCells()
    {
        var result = new FoldBuilder()
            .WithDataPoints(DataPoints)
            .WithDataFrequency(Frequency)
            .GridSearch()
            .WithTrainWindows(TimeSpan.FromDays(30))
            .WithTestWindows(TimeSpan.FromDays(7))
            .ForwardLooking()
            .Evaluate(_ => 1.0)
            .Build();

        result.Cells.Should().NotBeEmpty();
    }

    [Test]
    public void Build_NoModeSet_Throws()
    {
        var builder = new FoldBuilder()
            .WithDataPoints(DataPoints)
            .WithDataFrequency(Frequency)
            .GridSearch()
            .WithTrainWindows(TimeSpan.FromDays(30))
            .WithTestWindows(TimeSpan.FromDays(7))
            .Evaluate(_ => 1.0);

        var act = () => builder.Build();

        act.Should().Throw<InvalidOperationException>();
    }

    [Test]
    public void Build_NoTrainWindows_Throws()
    {
        var builder = new FoldBuilder()
            .WithDataPoints(DataPoints)
            .WithDataFrequency(Frequency)
            .GridSearch()
            .WithTestWindows(TimeSpan.FromDays(7))
            .BackwardLooking()
            .Evaluate(_ => 1.0);

        var act = () => builder.Build();

        act.Should().Throw<InvalidOperationException>();
    }

    [Test]
    public void Build_NoTestWindows_Throws()
    {
        var builder = new FoldBuilder()
            .WithDataPoints(DataPoints)
            .WithDataFrequency(Frequency)
            .GridSearch()
            .WithTrainWindows(TimeSpan.FromDays(30))
            .BackwardLooking()
            .Evaluate(_ => 1.0);

        var act = () => builder.Build();

        act.Should().Throw<InvalidOperationException>();
    }

    [Test]
    public void Build_NoEvaluate_Throws()
    {
        var builder = new FoldBuilder()
            .WithDataPoints(DataPoints)
            .WithDataFrequency(Frequency)
            .GridSearch()
            .WithTrainWindows(TimeSpan.FromDays(30))
            .WithTestWindows(TimeSpan.FromDays(7))
            .BackwardLooking();

        var act = () => builder.Build();

        act.Should().Throw<InvalidOperationException>();
    }

    // --- GRID-03: Per-cell aggregation ---
    [Test]
    public void Build_AggregatesPerCell_MeanFitness()
    {
        // Fitness returns fold index + 1.0 (1.0, 2.0, 3.0, ...)
        var result = new FoldBuilder()
            .WithDataPoints(DataPoints)
            .WithDataFrequency(Frequency)
            .GridSearch()
            .WithTrainWindows(TimeSpan.FromDays(30))
            .WithTestWindows(TimeSpan.FromDays(7))
            .BackwardLooking()
            .Evaluate(fold => fold.FoldIndex + 1.0)
            .Build();

        var cell = result.Cells[0];
        var expectedMean = cell.FoldFitnesses.Average();

        cell.MeanFitness.Should().BeApproximately(expectedMean, 0.0001);
    }

    [Test]
    public void Build_AggregatesPerCell_Consistency()
    {
        var result = new FoldBuilder()
            .WithDataPoints(DataPoints)
            .WithDataFrequency(Frequency)
            .GridSearch()
            .WithTrainWindows(TimeSpan.FromDays(30))
            .WithTestWindows(TimeSpan.FromDays(7))
            .BackwardLooking()
            .Evaluate(fold => fold.FoldIndex + 1.0)
            .Build();

        var cell = result.Cells[0];

        cell.Consistency.Should().NotBeNull();
        cell.Consistency.ConsistencyPercent.Should().BeGreaterThan(0);
    }

    [Test]
    public void Build_AggregatesPerCell_FoldCount()
    {
        // 30d train = 2880 points, 7d test = 672 points
        // Backward-looking max folds = floor((10000 - 2880) / 672) = 10
        var result = new FoldBuilder()
            .WithDataPoints(DataPoints)
            .WithDataFrequency(Frequency)
            .GridSearch()
            .WithTrainWindows(TimeSpan.FromDays(30))
            .WithTestWindows(TimeSpan.FromDays(7))
            .BackwardLooking()
            .Evaluate(_ => 1.0)
            .Build();

        var cell = result.Cells[0];

        cell.FoldCount.Should().Be(10);
    }

    [Test]
    public void Build_AggregatesPerCell_WorstFold()
    {
        // Fitness returns fold index + 1.0 (1.0, 2.0, 3.0, ...)
        // Worst fold = 1.0 (fold index 0)
        var result = new FoldBuilder()
            .WithDataPoints(DataPoints)
            .WithDataFrequency(Frequency)
            .GridSearch()
            .WithTrainWindows(TimeSpan.FromDays(30))
            .WithTestWindows(TimeSpan.FromDays(7))
            .BackwardLooking()
            .Evaluate(fold => fold.FoldIndex + 1.0)
            .Build();

        var cell = result.Cells[0];

        cell.WorstFold.Should().Be(1.0);
    }

    [Test]
    public void Build_AggregatesPerCell_FoldFitnesses()
    {
        var result = new FoldBuilder()
            .WithDataPoints(DataPoints)
            .WithDataFrequency(Frequency)
            .GridSearch()
            .WithTrainWindows(TimeSpan.FromDays(30))
            .WithTestWindows(TimeSpan.FromDays(7))
            .BackwardLooking()
            .Evaluate(fold => fold.FoldIndex + 1.0)
            .Build();

        var cell = result.Cells[0];

        cell.FoldFitnesses.Should().HaveCount(cell.FoldCount);

        // Values should be 1.0, 2.0, 3.0, ... foldCount
        for (var i = 0; i < cell.FoldCount; i++)
        {
            cell.FoldFitnesses[i].Should().BeApproximately(i + 1.0, 0.0001);
        }
    }

    [Test]
    public void Build_SkipsZeroFoldCells()
    {
        // Use a very large train window (100d = 9600 points) with small data (10000 points)
        // plus 90d train as a valid window
        // 100d: train=9600, test=672, total needed = 9600+672 = 10272 > 10000 -> 0 folds
        // Actually backward-looking: maxFolds = floor((10000-9600)/672) = floor(0.595) = 0
        var result = new FoldBuilder()
            .WithDataPoints(DataPoints)
            .WithDataFrequency(Frequency)
            .GridSearch()
            .WithTrainWindows(TimeSpan.FromDays(30), TimeSpan.FromDays(100))
            .WithTestWindows(TimeSpan.FromDays(7))
            .BackwardLooking()
            .WithMinimumFolds(1)
            .Evaluate(_ => 1.0)
            .Build();

        // Only the 30d train window should produce cells
        result.Cells.Should().HaveCount(1);
        result.Cells[0].TrainWindow.Should().Be(TimeSpan.FromDays(30));
    }

    // --- GRID-04: Minimum folds filtering ---
    [Test]
    public void Build_WithMinimumFolds_FiltersLowFoldCells()
    {
        // Create a grid where some cells produce few folds
        // 90d train = 8640 points. Backward: maxFolds = floor((10000-8640)/672) = floor(2.02) = 2
        // 30d train = 2880 points. Backward: maxFolds = floor((10000-2880)/672) = floor(10.59) = 10
        // WithMinimumFolds(3) should exclude the 90d cell (2 folds)
        var result = new FoldBuilder()
            .WithDataPoints(DataPoints)
            .WithDataFrequency(Frequency)
            .GridSearch()
            .WithTrainWindows(TimeSpan.FromDays(30), TimeSpan.FromDays(90))
            .WithTestWindows(TimeSpan.FromDays(7))
            .BackwardLooking()
            .WithMinimumFolds(3)
            .Evaluate(_ => 1.0)
            .Build();

        result.Cells.Should().HaveCount(1);
        result.Cells[0].TrainWindow.Should().Be(TimeSpan.FromDays(30));
    }

    [Test]
    public void Build_DefaultMinimumFolds_Is2()
    {
        // 95d train = 9120 points. Backward: maxFolds = floor((10000-9120)/672) = floor(1.31) = 1
        // Default min folds = 2, so this cell should be excluded
        // 30d train produces 10 folds -> included
        var result = new FoldBuilder()
            .WithDataPoints(DataPoints)
            .WithDataFrequency(Frequency)
            .GridSearch()
            .WithTrainWindows(TimeSpan.FromDays(30), TimeSpan.FromDays(95))
            .WithTestWindows(TimeSpan.FromDays(7))
            .BackwardLooking()
            .Evaluate(_ => 1.0)
            .Build();

        result.Cells.Should().HaveCount(1);
        result.Cells[0].TrainWindow.Should().Be(TimeSpan.FromDays(30));
    }

    // --- GRID-06: Cancellation ---
    [Test]
    public void Build_CancelledToken_ThrowsOperationCancelledException()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var builder = new FoldBuilder()
            .WithDataPoints(DataPoints)
            .WithDataFrequency(Frequency)
            .GridSearch()
            .WithTrainWindows(TimeSpan.FromDays(30))
            .WithTestWindows(TimeSpan.FromDays(7))
            .BackwardLooking()
            .Evaluate(_ => 1.0);

        var act = () => builder.Build(cts.Token);

        act.Should().Throw<OperationCanceledException>();
    }

    [Test]
    public void Build_CancelBetweenCells_ThrowsAfterFirstCell()
    {
        using var cts = new CancellationTokenSource();
        var cellCount = 0;

        var builder = new FoldBuilder()
            .WithDataPoints(DataPoints)
            .WithDataFrequency(Frequency)
            .GridSearch()
            .WithTrainWindows(TimeSpan.FromDays(30), TimeSpan.FromDays(60), TimeSpan.FromDays(90))
            .WithTestWindows(TimeSpan.FromDays(7))
            .BackwardLooking()
            .Evaluate(fold =>
            {
                // Cancel after the first cell completes all its fold evaluations
                if (fold.FoldIndex == 0)
                {
                    cellCount++;
                    if (cellCount > 1)
                    {
                        cts.Cancel();
                    }
                }

                return 1.0;
            });

        var act = () => builder.Build(cts.Token);

        act.Should().Throw<OperationCanceledException>();
    }

    // --- Additional builder tests ---
    [Test]
    public void WithWarmup_PassesThroughToFoldGeneration()
    {
        // Without warmup: 30d/7d backward gives 10 folds
        // With warmup=3000: trainStart must be >= 3000
        // trainStart = (10000 - i*672) - 672 - 2880 for backward
        // fold 0: trainStart = 10000 - 672 - 2880 = 6448 (>= 3000, included)
        // fold 7: trainStart = 10000 - 7*672 - 672 - 2880 = 10000 - 4704 - 672 - 2880 = 1744 (< 3000, excluded)
        var withWarmup = new FoldBuilder()
            .WithDataPoints(DataPoints)
            .WithDataFrequency(Frequency)
            .GridSearch()
            .WithTrainWindows(TimeSpan.FromDays(30))
            .WithTestWindows(TimeSpan.FromDays(7))
            .BackwardLooking()
            .WithWarmup(3000)
            .Evaluate(_ => 1.0)
            .Build();

        var withoutWarmup = new FoldBuilder()
            .WithDataPoints(DataPoints)
            .WithDataFrequency(Frequency)
            .GridSearch()
            .WithTrainWindows(TimeSpan.FromDays(30))
            .WithTestWindows(TimeSpan.FromDays(7))
            .BackwardLooking()
            .Evaluate(_ => 1.0)
            .Build();

        withWarmup.Cells[0].FoldCount.Should().BeLessThan(withoutWarmup.Cells[0].FoldCount);
    }

    [Test]
    public void WithEmbargo_PassesThroughToFoldGeneration()
    {
        // With embargo=4h: embargoIndexCount = ceil(4*60/15) = 16
        // Backward: maxFolds = floor((10000 - 2880 - 16) / 672) = floor(7104/672) = 10 still
        // But because embargo occupies space, folds whose train window starts < 0 are excluded
        // Actually the formula: maxFolds = floor((totalDataPoints - trainCount - embargoCount) / testCount)
        // Without embargo: floor((10000-2880)/672) = 10
        // With embargo=4h (16 points): floor((10000-2880-16)/672) = floor(7104/672) = 10
        // Need bigger embargo. Use 2 days = 192 points:
        // floor((10000-2880-192)/672) = floor(6928/672) = 10 still
        // Use 10 days = 960 points:
        // floor((10000-2880-960)/672) = floor(6160/672) = 9
        var withEmbargo = new FoldBuilder()
            .WithDataPoints(DataPoints)
            .WithDataFrequency(Frequency)
            .GridSearch()
            .WithTrainWindows(TimeSpan.FromDays(30))
            .WithTestWindows(TimeSpan.FromDays(7))
            .BackwardLooking()
            .WithEmbargo(TimeSpan.FromDays(10))
            .Evaluate(_ => 1.0)
            .Build();

        var withoutEmbargo = new FoldBuilder()
            .WithDataPoints(DataPoints)
            .WithDataFrequency(Frequency)
            .GridSearch()
            .WithTrainWindows(TimeSpan.FromDays(30))
            .WithTestWindows(TimeSpan.FromDays(7))
            .BackwardLooking()
            .Evaluate(_ => 1.0)
            .Build();

        withEmbargo.Cells[0].FoldCount.Should().BeLessThan(withoutEmbargo.Cells[0].FoldCount);
    }

    [Test]
    public void WithMaxFoldsPerCell_LimitsFolds()
    {
        var result = new FoldBuilder()
            .WithDataPoints(DataPoints)
            .WithDataFrequency(Frequency)
            .GridSearch()
            .WithTrainWindows(TimeSpan.FromDays(30))
            .WithTestWindows(TimeSpan.FromDays(7))
            .BackwardLooking()
            .WithMaxFoldsPerCell(2)
            .Evaluate(_ => 1.0)
            .Build();

        foreach (var cell in result.Cells)
        {
            cell.FoldCount.Should().BeLessThanOrEqualTo(2);
        }
    }
}
