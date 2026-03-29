using WalkForward.Degradation;
using WalkForward.GridSearch;
using WalkForward.Pipeline;
using WalkForward.Scoring;

namespace WalkForward.Internal;

/// <summary>
/// Internal engine that orchestrates pipeline stages sequentially:
/// CoarseScan (always) -> Score (optional) -> TopCandidates (optional) -> Validate (optional).
/// </summary>
internal static class PipelineEngine
{
    /// <summary>
    /// Executes the pipeline stages and returns the aggregate result.
    /// </summary>
    internal static PipelineResult Execute(
        int totalDataPoints,
        TimeSpan dataFrequency,
        Action<GridSearchBuilder> coarseScanConfig,
        CompositeScorer? scorer,
        int? topN,
        Action<DegradationBuilder>? validateConfig,
        CancellationToken cancellationToken,
        IProgress<PipelineProgress>? progress)
    {
        var totalStages = CountStages(scorer, topN, validateConfig);
        var stageIndex = 0;

        // --- Stage: CoarseScan (always) ---
        cancellationToken.ThrowIfCancellationRequested();
        ReportProgress(progress, "CoarseScan", stageIndex, totalStages, 0, StageStartPercent(stageIndex, totalStages));

        var gridBuilder = new GridSearchBuilder(totalDataPoints, dataFrequency);
        coarseScanConfig(gridBuilder);
        var gridResult = gridBuilder.Build(cancellationToken);

        ReportProgress(progress, "CoarseScan", stageIndex, totalStages, 100, StageEndPercent(stageIndex, totalStages));
        stageIndex++;

        IReadOnlyList<GridCellResult> scoredCells = gridResult.Cells;

        // --- Stage: Score (optional) ---
        if (scorer is not null)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ReportProgress(progress, "Score", stageIndex, totalStages, 0, StageStartPercent(stageIndex, totalStages));

            scoredCells = scorer.Score(scoredCells);
            scoredCells = scoredCells.OrderByDescending(c => c.CompositeScore).ToList();

            ReportProgress(progress, "Score", stageIndex, totalStages, 100, StageEndPercent(stageIndex, totalStages));
            stageIndex++;
        }

        // --- Stage: TopCandidates (optional) ---
        var candidates = scoredCells;
        if (topN.HasValue)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ReportProgress(progress, "TopCandidates", stageIndex, totalStages, 0, StageStartPercent(stageIndex, totalStages));

            candidates = scoredCells.Take(topN.Value).ToList();

            ReportProgress(progress, "TopCandidates", stageIndex, totalStages, 100, StageEndPercent(stageIndex, totalStages));
            stageIndex++;
        }

        // --- Stage: Validate (optional) ---
        var validationResults = new Dictionary<GridCellResult, DegradationResult>();
        if (validateConfig is not null && candidates.Count > 0)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ReportProgress(progress, "Validate", stageIndex, totalStages, 0, StageStartPercent(stageIndex, totalStages));

            for (var i = 0; i < candidates.Count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var cell = candidates[i];

                var degBuilder = new DegradationBuilder(totalDataPoints, dataFrequency);
                validateConfig(degBuilder);
                degBuilder.WithTrainingWindow(cell.TrainWindow);
                degBuilder.WithTestWindow(cell.TestWindow);
                validationResults[cell] = degBuilder.Build(cancellationToken);

                var cellPercent = (i + 1.0) / candidates.Count * 100.0;
                var overallPercent = (stageIndex + ((i + 1.0) / candidates.Count)) / totalStages * 100.0;
                ReportProgress(progress, "Validate", stageIndex, totalStages, cellPercent, overallPercent);
            }
        }

        // --- Winner selection ---
        GridCellResult? winner;
        if (validationResults.Count > 0)
        {
            winner = validationResults
                .OrderByDescending(kvp => kvp.Value.WalkForwardEfficiency)
                .First().Key;
        }
        else if (candidates.Count > 0)
        {
            winner = candidates[0];
        }
        else
        {
            winner = null;
        }

        return new PipelineResult
        {
            CoarseScanResult = gridResult,
            ScoredCells = scoredCells,
            TopCandidates = candidates,
            ValidationResults = validationResults,
            Winner = winner,
        };
    }

    private static int CountStages(CompositeScorer? scorer, int? topN, Action<DegradationBuilder>? validateConfig)
    {
        var count = 1; // CoarseScan always

        if (scorer is not null)
        {
            count++;
        }

        if (topN.HasValue)
        {
            count++;
        }

        if (validateConfig is not null)
        {
            count++;
        }

        return count;
    }

    private static double StageStartPercent(int stageIndex, int totalStages) =>
        (double)stageIndex / totalStages * 100.0;

    private static double StageEndPercent(int stageIndex, int totalStages) =>
        (stageIndex + 1.0) / totalStages * 100.0;

    private static void ReportProgress(
        IProgress<PipelineProgress>? progress,
        string stageName,
        int stageIndex,
        int totalStages,
        double stagePercent,
        double overallPercent)
    {
        progress?.Report(new PipelineProgress
        {
            StageName = stageName,
            StageIndex = stageIndex,
            TotalStages = totalStages,
            StagePercent = stagePercent,
            OverallPercent = overallPercent,
        });
    }
}
