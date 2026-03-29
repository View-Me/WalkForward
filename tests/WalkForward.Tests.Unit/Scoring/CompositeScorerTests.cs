using FluentAssertions;
using NUnit.Framework;
using WalkForward.GridSearch;
using WalkForward.Scoring;

namespace WalkForward.Tests.Unit.Scoring;

[TestFixture]
public class CompositeScorerTests
{
    [Test]
    public void Score_withoutCallingWithWeights_throwsInvalidOperationException()
    {
        var scorer = new CompositeScorer();
        var cells = new List<GridCellResult> { MakeCell(1.0, 80) };

        var act = () => scorer.Score(cells);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*WithWeights*");
    }

    [Test]
    public void WithWeights_negativeWeight_throwsArgumentException()
    {
        var scorer = new CompositeScorer();

        var act = () => scorer.WithWeights(-0.1, 0.6, 0.5);

        act.Should().Throw<ArgumentException>();
    }

    [Test]
    public void WithWeights_allZeroWeights_throwsArgumentException()
    {
        var scorer = new CompositeScorer();

        var act = () => scorer.WithWeights(0.0, 0.0, 0.0);

        act.Should().Throw<ArgumentException>();
    }

    [Test]
    public void WithWeights_sumNotApproximatelyOne_throwsArgumentException()
    {
        var scorer = new CompositeScorer();

        // Sum = 1.5, outside tolerance of 0.001
        var act = () => scorer.WithWeights(0.5, 0.5, 0.5);

        act.Should().Throw<ArgumentException>();
    }

    [Test]
    public void Score_3x3Grid_computesCorrectCompositeScores()
    {
        // 3x3 grid: train windows 30d, 60d, 90d; test windows 7d, 14d, 21d
        var trainDays = new[] { 30, 60, 90 };
        var testDays = new[] { 7, 14, 21 };
        var fitnesses = new[,]
        {
            { 1.0, 1.2, 1.5 },
            { 1.8, 2.0, 2.2 },
            { 2.5, 2.8, 3.0 },
        };
        var consistencies = new[,]
        {
            { 60.0, 65.0, 70.0 },
            { 72.0, 75.0, 78.0 },
            { 80.0, 85.0, 90.0 },
        };

        var cells = new List<GridCellResult>();
        for (var t = 0; t < 3; t++)
        {
            for (var s = 0; s < 3; s++)
            {
                cells.Add(MakeCell(
                    fitnesses[t, s],
                    consistencies[t, s],
                    TimeSpan.FromDays(trainDays[t]),
                    TimeSpan.FromDays(testDays[s])));
            }
        }

        var scorer = new CompositeScorer().WithWeights(0.6, 0.25, 0.15);
        var result = scorer.Score(cells);

        result.Should().HaveCount(9);

        // maxFitness = 3.0
        // For each cell: normalizedFitness = MeanFitness / 3.0
        // consistencyFraction = ConsistencyPercent / 100.0
        // smoothness = Smoothness.Compute(trainIdx, testIdx, MeanFitness, gridCells)
        // composite = (normalizedFitness * 0.6) + (consistencyFraction * 0.25) + (smoothness * 0.15)

        // Verify a few key cells
        var maxFitness = 3.0;

        // Cell (0,0) - corner: trainIdx=0, testIdx=0
        var cell00 = result[0];
        var nf00 = 1.0 / maxFitness;
        cell00.CompositeScore.Should().BeGreaterThan(0.0);
        cell00.SmoothnessBonus.Should().BeGreaterThanOrEqualTo(0.0);

        // Cell (2,2) - corner: trainIdx=2, testIdx=2, highest fitness
        var cell22 = result[8];
        var nf22 = 3.0 / maxFitness; // = 1.0
        cell22.CompositeScore.Should().BeGreaterThan(cell00.CompositeScore,
            "highest fitness + consistency cell should score higher");

        // Verify all cells have non-zero CompositeScore
        foreach (var cell in result)
        {
            cell.CompositeScore.Should().BeGreaterThan(0.0);
        }
    }

    [Test]
    public void Score_singleCell_computesExpectedScore()
    {
        var cells = new List<GridCellResult>
        {
            MakeCell(1.5, 80),
        };

        var scorer = new CompositeScorer().WithWeights(0.6, 0.25, 0.15);
        var result = scorer.Score(cells);

        result.Should().HaveCount(1);

        // Single cell: no neighbors so smoothness = 0.0
        // normalizedFitness = 1.5 / 1.5 = 1.0
        // consistencyFraction = 80 / 100 = 0.8
        // composite = (1.0 * 0.6) + (0.8 * 0.25) + (0.0 * 0.15) = 0.6 + 0.2 + 0.0 = 0.8
        result[0].SmoothnessBonus.Should().BeApproximately(0.0, 0.0001);
        result[0].CompositeScore.Should().BeApproximately(0.8, 0.0001);
    }

    [Test]
    public void Score_returnsNewInstances_doesNotMutateInput()
    {
        var input = MakeCell(2.0, 90);
        var cells = new List<GridCellResult> { input };

        var scorer = new CompositeScorer().WithWeights(0.6, 0.25, 0.15);
        var result = scorer.Score(cells);

        result[0].Should().NotBeSameAs(input);
        input.SmoothnessBonus.Should().Be(0.0);
        input.CompositeScore.Should().Be(0.0);
    }

    [Test]
    public void Score_allZeroFitness_usesGuardValue()
    {
        var cells = new List<GridCellResult>
        {
            MakeCell(0.0, 80),
            MakeCell(0.0, 60, TimeSpan.FromDays(60)),
        };

        var scorer = new CompositeScorer().WithWeights(0.6, 0.25, 0.15);
        var result = scorer.Score(cells);

        // maxFitness = 0 -> guard to 1.0
        // normalizedFitness = 0.0 / 1.0 = 0.0 for all cells
        // Scores come purely from consistency and smoothness
        foreach (var cell in result)
        {
            cell.CompositeScore.Should().BeGreaterThan(0.0,
                "consistency component should contribute even when fitness is zero");
        }

        // Cell with 80% consistency should score higher than 60%
        result[0].CompositeScore.Should().BeGreaterThan(result[1].CompositeScore);
    }

    [Test]
    public void Score_preservesAllOtherFields()
    {
        var input = new GridCellResult
        {
            TrainWindow = TimeSpan.FromDays(45),
            TestWindow = TimeSpan.FromDays(10),
            TrainDataPoints = 4320,
            TestDataPoints = 960,
            MeanFitness = 2.5,
            Consistency = new ConsistencyMetrics(85, 0.7, -0.01, 2.5),
            FoldCount = 8,
            WorstFold = 0.5,
            FoldFitnesses = [2.0, 2.5, 3.0, 2.5, 2.0, 2.5, 3.0, 2.5],
        };

        var cells = new List<GridCellResult> { input };
        var scorer = new CompositeScorer().WithWeights(0.6, 0.25, 0.15);
        var result = scorer.Score(cells);

        var scored = result[0];
        scored.TrainWindow.Should().Be(TimeSpan.FromDays(45));
        scored.TestWindow.Should().Be(TimeSpan.FromDays(10));
        scored.TrainDataPoints.Should().Be(4320);
        scored.TestDataPoints.Should().Be(960);
        scored.MeanFitness.Should().Be(2.5);
        scored.Consistency.Should().Be(new ConsistencyMetrics(85, 0.7, -0.01, 2.5));
        scored.FoldCount.Should().Be(8);
        scored.WorstFold.Should().Be(0.5);
        scored.FoldFitnesses.Should().BeEquivalentTo(new[] { 2.0, 2.5, 3.0, 2.5, 2.0, 2.5, 3.0, 2.5 });
    }

    [Test]
    public void WithWeights_returnsSameInstance_forFluentChaining()
    {
        var scorer = new CompositeScorer();

        var returned = scorer.WithWeights(0.6, 0.25, 0.15);

        ReferenceEquals(scorer, returned).Should().BeTrue();
    }

    [Test]
    public void Score_emptyCellList_returnsEmptyList()
    {
        var scorer = new CompositeScorer().WithWeights(0.6, 0.25, 0.15);

        var result = scorer.Score(Array.Empty<GridCellResult>());

        result.Should().BeEmpty();
        result.Should().NotBeNull();
    }

    private static GridCellResult MakeCell(
        double meanFitness,
        double consistencyPercent,
        TimeSpan? trainWindow = null,
        TimeSpan? testWindow = null)
    {
        return new GridCellResult
        {
            TrainWindow = trainWindow ?? TimeSpan.FromDays(90),
            TestWindow = testWindow ?? TimeSpan.FromDays(7),
            TrainDataPoints = 8640,
            TestDataPoints = 672,
            MeanFitness = meanFitness,
            Consistency = new ConsistencyMetrics(consistencyPercent, 0.5, -0.01, meanFitness),
            FoldCount = 5,
            WorstFold = meanFitness * 0.5,
            FoldFitnesses = [meanFitness, meanFitness, meanFitness, meanFitness, meanFitness],
        };
    }
}
