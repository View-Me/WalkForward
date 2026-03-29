using FluentAssertions;
using NUnit.Framework;
using WalkForward.GridSearch;

namespace WalkForward.Tests.Unit.GridSearch;

[TestFixture]
public class GridSearchResultTests
{
    [Test]
    public void Top_ReturnsRequestedCount()
    {
        var result = MakeResult(6);

        var top = result.Top(3);

        top.Should().HaveCount(3);
    }

    [Test]
    public void Top_MoreThanAvailable_ReturnsAll()
    {
        var result = MakeResult(3);

        var top = result.Top(10);

        top.Should().HaveCount(3);
    }

    [Test]
    public void Top_Zero_ReturnsEmpty()
    {
        var result = MakeResult(5);

        var top = result.Top(0);

        top.Should().BeEmpty();
    }

    [Test]
    public void Cells_OrderedByMeanFitness_WhenUnscored()
    {
        // Build a grid search result through the engine, which sorts by MeanFitness descending.
        // Use different train windows that produce different fitness (larger train -> fewer folds -> different mean).
        var result = new FoldBuilder()
            .WithDataPoints(10000)
            .WithDataFrequency(TimeSpan.FromMinutes(15))
            .GridSearch()
            .WithTrainWindows(TimeSpan.FromDays(30), TimeSpan.FromDays(60))
            .WithTestWindows(TimeSpan.FromDays(7))
            .BackwardLooking()
            .Evaluate(fold => fold.FoldIndex + 1.0)
            .Build();

        // Engine should sort cells by MeanFitness descending
        for (var i = 0; i < result.Cells.Count - 1; i++)
        {
            result.Cells[i].MeanFitness.Should().BeGreaterThanOrEqualTo(
                result.Cells[i + 1].MeanFitness,
                "cells should be ordered by MeanFitness descending when unscored");
        }
    }

    private static GridSearchResult MakeResult(int cellCount)
    {
        var cells = new List<GridCellResult>();
        for (var i = 0; i < cellCount; i++)
        {
            cells.Add(MakeCell(cellCount - i, TimeSpan.FromDays(30 + (i * 30))));
        }

        return new GridSearchResult { Cells = cells };
    }

    private static GridCellResult MakeCell(double meanFitness, TimeSpan trainWindow)
    {
        return new GridCellResult
        {
            TrainWindow = trainWindow,
            TestWindow = TimeSpan.FromDays(7),
            TrainDataPoints = (int)(trainWindow / TimeSpan.FromMinutes(15)),
            TestDataPoints = 672,
            MeanFitness = meanFitness,
            Consistency = new ConsistencyMetrics(80, 0.5, 0.5, meanFitness),
            FoldCount = 5,
            WorstFold = meanFitness * 0.5,
            FoldFitnesses = [meanFitness, meanFitness, meanFitness, meanFitness, meanFitness],
        };
    }
}
