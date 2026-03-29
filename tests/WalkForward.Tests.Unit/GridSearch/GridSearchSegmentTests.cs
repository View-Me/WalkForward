using FluentAssertions;
using NUnit.Framework;
using WalkForward.Scoring;

namespace WalkForward.Tests.Unit.GridSearch;

[TestFixture]
public class GridSearchSegmentTests
{
    private const int DataPoints = 10000;
    private static readonly TimeSpan Frequency = TimeSpan.FromMinutes(15);
    private static readonly string[] SingleLabelA = ["A"];
    private static readonly string[] DualLabelAb = ["A", "B"];
    private static readonly string[] SingleLabelX = ["X"];

    // --- SEGM-01: Labeler callback invocation ---
    [Test]
    public void Build_WithLabeler_InvokesLabelerPerFold()
    {
        var labeledFolds = new List<Fold>();

        var result = new FoldBuilder()
            .WithDataPoints(DataPoints)
            .WithDataFrequency(Frequency)
            .GridSearch()
            .WithTrainWindows(TimeSpan.FromDays(30), TimeSpan.FromDays(60))
            .WithTestWindows(TimeSpan.FromDays(7), TimeSpan.FromDays(14))
            .BackwardLooking()
            .Evaluate(_ => 1.0)
            .WithLabeler(fold =>
            {
                labeledFolds.Add(fold);
                return SingleLabelA;
            })
            .Build();

        var totalFolds = result.Cells.Sum(c => c.FoldCount);
        labeledFolds.Should().HaveCount(
            totalFolds,
            "labeler should be invoked once for every fold across all cells");
    }

    [Test]
    public void Build_WithLabeler_MultiLabel_FoldContributesToMultipleSegments()
    {
        var result = new FoldBuilder()
            .WithDataPoints(DataPoints)
            .WithDataFrequency(Frequency)
            .GridSearch()
            .WithTrainWindows(TimeSpan.FromDays(30), TimeSpan.FromDays(60))
            .WithTestWindows(TimeSpan.FromDays(7), TimeSpan.FromDays(14))
            .BackwardLooking()
            .Evaluate(_ => 1.0)
            .WithLabeler(_ => DualLabelAb)
            .Build();

        result.SegmentResults.Should().ContainKey("A");
        result.SegmentResults.Should().ContainKey("B");
        result.SegmentResults["A"].Should().HaveCount(
            result.SegmentResults["B"].Count,
            "each segment should have the same number of cells when all folds get both labels");
    }

    [Test]
    public void Build_WithLabeler_EmptyLabels_FoldExcludedFromSegments()
    {
        var result = new FoldBuilder()
            .WithDataPoints(DataPoints)
            .WithDataFrequency(Frequency)
            .GridSearch()
            .WithTrainWindows(TimeSpan.FromDays(30), TimeSpan.FromDays(60))
            .WithTestWindows(TimeSpan.FromDays(7), TimeSpan.FromDays(14))
            .BackwardLooking()
            .Evaluate(_ => 1.0)
            .WithLabeler(_ => Array.Empty<string>())
            .Build();

        result.SegmentResults.Should().BeEmpty(
            "empty label enumerables should result in no segments");
        result.Cells.Should().NotBeEmpty(
            "overall cells should still be populated");
    }

    [Test]
    public void Build_WithoutLabeler_SegmentResultsEmpty()
    {
        var result = new FoldBuilder()
            .WithDataPoints(DataPoints)
            .WithDataFrequency(Frequency)
            .GridSearch()
            .WithTrainWindows(TimeSpan.FromDays(30), TimeSpan.FromDays(60))
            .WithTestWindows(TimeSpan.FromDays(7), TimeSpan.FromDays(14))
            .BackwardLooking()
            .Evaluate(_ => 1.0)
            .Build();

        result.SegmentResults.Should().BeEmpty(
            "no labeler set should produce empty SegmentResults");
        result.BestPerSegment.Should().BeEmpty(
            "no labeler set should produce empty BestPerSegment");
        result.Cells.Should().NotBeEmpty(
            "overall cells should still be populated");
    }

    // --- SEGM-02: Per-segment grouping and selection ---
    [Test]
    public void Build_WithLabeler_SegmentResultsContainsRankedCells()
    {
        // Use fitness that varies by train window size to get different MeanFitness per cell
        var result = new FoldBuilder()
            .WithDataPoints(DataPoints)
            .WithDataFrequency(Frequency)
            .GridSearch()
            .WithTrainWindows(TimeSpan.FromDays(30), TimeSpan.FromDays(60))
            .WithTestWindows(TimeSpan.FromDays(7), TimeSpan.FromDays(14))
            .BackwardLooking()
            .Evaluate(fold => fold.TrainRange.End.Value - fold.TrainRange.Start.Value)
            .WithLabeler(_ => SingleLabelX)
            .Build();

        result.SegmentResults.Should().ContainKey("X");
        var segmentCells = result.SegmentResults["X"];
        segmentCells.Should().NotBeEmpty();

        // Verify ordering by MeanFitness descending (no scorer set)
        for (var i = 0; i < segmentCells.Count - 1; i++)
        {
            segmentCells[i].MeanFitness.Should().BeGreaterThanOrEqualTo(
                segmentCells[i + 1].MeanFitness,
                "segment cells should be ranked by MeanFitness descending when unscored");
        }
    }

    [Test]
    public void Build_WithLabeler_BestPerSegment_ReturnsBestCell()
    {
        var result = new FoldBuilder()
            .WithDataPoints(DataPoints)
            .WithDataFrequency(Frequency)
            .GridSearch()
            .WithTrainWindows(TimeSpan.FromDays(30), TimeSpan.FromDays(60))
            .WithTestWindows(TimeSpan.FromDays(7), TimeSpan.FromDays(14))
            .BackwardLooking()
            .Evaluate(fold => fold.TrainRange.End.Value - fold.TrainRange.Start.Value)
            .WithLabeler(fold =>
            {
                // Assign "X" to all folds, "Y" only to folds with larger train windows
                var labels = new List<string> { "X" };
                if (fold.TrainLength > 3000)
                {
                    labels.Add("Y");
                }

                return labels;
            })
            .Build();

        result.BestPerSegment.Should().ContainKey("X");
        result.BestPerSegment.Should().ContainKey("Y");

        // BestPerSegment["X"] should be the first entry in SegmentResults["X"]
        result.BestPerSegment["X"].Should().Be(result.SegmentResults["X"][0]);
        result.BestPerSegment["Y"].Should().Be(result.SegmentResults["Y"][0]);
    }

    [Test]
    public void Build_WithLabeler_Scored_SegmentRankedByCompositeScore()
    {
        var scorer = new CompositeScorer().WithWeights(0.6, 0.25, 0.15);

        var result = new FoldBuilder()
            .WithDataPoints(DataPoints)
            .WithDataFrequency(Frequency)
            .GridSearch()
            .WithTrainWindows(TimeSpan.FromDays(30), TimeSpan.FromDays(60))
            .WithTestWindows(TimeSpan.FromDays(7), TimeSpan.FromDays(14))
            .BackwardLooking()
            .Evaluate(fold => fold.TrainRange.End.Value - fold.TrainRange.Start.Value)
            .WithLabeler(_ => SingleLabelX)
            .WithScoring(scorer)
            .Build();

        result.SegmentResults.Should().ContainKey("X");
        var segmentCells = result.SegmentResults["X"];

        // Verify ordering by CompositeScore descending (scorer set)
        for (var i = 0; i < segmentCells.Count - 1; i++)
        {
            segmentCells[i].CompositeScore.Should().BeGreaterThanOrEqualTo(
                segmentCells[i + 1].CompositeScore,
                "segment cells should be ranked by CompositeScore descending when scored");
        }

        // CompositeScore should be populated (non-zero)
        segmentCells.Should().Contain(
            c => c.CompositeScore > 0,
            "scored segment cells should have positive CompositeScore");
    }

    [Test]
    public void Build_WithLabeler_Unscored_SegmentRankedByMeanFitness()
    {
        var result = new FoldBuilder()
            .WithDataPoints(DataPoints)
            .WithDataFrequency(Frequency)
            .GridSearch()
            .WithTrainWindows(TimeSpan.FromDays(30), TimeSpan.FromDays(60))
            .WithTestWindows(TimeSpan.FromDays(7), TimeSpan.FromDays(14))
            .BackwardLooking()
            .Evaluate(fold => fold.TrainRange.End.Value - fold.TrainRange.Start.Value)
            .WithLabeler(_ => SingleLabelX)
            .Build();

        result.SegmentResults.Should().ContainKey("X");
        var segmentCells = result.SegmentResults["X"];

        // Verify ordering by MeanFitness descending (no scorer)
        for (var i = 0; i < segmentCells.Count - 1; i++)
        {
            segmentCells[i].MeanFitness.Should().BeGreaterThanOrEqualTo(
                segmentCells[i + 1].MeanFitness,
                "segment cells should be ranked by MeanFitness descending when unscored");
        }
    }

    [Test]
    public void Build_WithLabeler_MinimumFoldsFilter_AppliedPerSegment()
    {
        // Label "sparse" is only assigned to the first fold of each cell (fold index 0)
        // Label "full" is assigned to all folds
        // With minimumFolds=2, "sparse" cells should have 1 fold each -> excluded from segment
        // "full" cells should have all folds -> included
        var result = new FoldBuilder()
            .WithDataPoints(DataPoints)
            .WithDataFrequency(Frequency)
            .GridSearch()
            .WithTrainWindows(TimeSpan.FromDays(30), TimeSpan.FromDays(60))
            .WithTestWindows(TimeSpan.FromDays(7), TimeSpan.FromDays(14))
            .BackwardLooking()
            .WithMinimumFolds(2)
            .Evaluate(_ => 1.0)
            .WithLabeler(fold =>
            {
                var labels = new List<string> { "full" };
                if (fold.FoldIndex == 0)
                {
                    labels.Add("sparse");
                }

                return labels;
            })
            .Build();

        // "full" segment should have cells (all folds tagged)
        result.SegmentResults.Should().ContainKey("full");
        result.SegmentResults["full"].Should().NotBeEmpty();

        // "sparse" segment: each cell has exactly 1 fold tagged -> all excluded by minimumFolds=2
        result.SegmentResults.Should().NotContainKey(
            "sparse",
            "cells with only 1 fold in 'sparse' segment should be excluded by minimumFolds=2");

        // Overall cells should still be populated
        result.Cells.Should().NotBeEmpty();
    }
}
