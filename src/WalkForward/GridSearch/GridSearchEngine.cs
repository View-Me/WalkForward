using WalkForward.GridSearch;
using WalkForward.Scoring;

namespace WalkForward.Internal;

/// <summary>
/// Internal engine that iterates over all (train, test) window combinations in a grid search,
/// invokes the fitness callback per fold, and aggregates results per cell.
/// </summary>
internal static class GridSearchEngine
{
    /// <summary>
    /// Executes the grid search over all (train, test) window combinations.
    /// </summary>
    /// <param name="totalDataPoints">Total number of data points in the dataset.</param>
    /// <param name="dataFrequency">Time interval between consecutive data points.</param>
    /// <param name="trainWindows">Training window durations to evaluate.</param>
    /// <param name="testWindows">Test window durations to evaluate.</param>
    /// <param name="mode">Fold generation mode (backward or forward looking).</param>
    /// <param name="fitnessCallback">Callback invoked per fold to compute fitness.</param>
    /// <param name="warmupPoints">Minimum data points before the first training window.</param>
    /// <param name="embargo">Embargo gap duration between training and test windows.</param>
    /// <param name="minimumFolds">Minimum fold count for a cell to be included in results.</param>
    /// <param name="maxFoldsPerCell">Optional cap on folds per cell.</param>
    /// <param name="scorer">Optional composite scorer. When provided, cells are scored and sorted by composite score descending.</param>
    /// <param name="labeler">Optional labeler callback. When provided, fold labels are collected and per-segment results are built.</param>
    /// <param name="innerFolds">Number of inner K-fold temporal splits within each training window. Values less than 2 disable inner cross-validation.</param>
    /// <param name="cancellationToken">Token checked between cell iterations.</param>
    /// <returns>A <see cref="GridSearchResult"/> with cells sorted by composite score (if scored) or mean fitness (if unscored) descending.</returns>
    internal static GridSearchResult Execute(
        int totalDataPoints,
        TimeSpan dataFrequency,
        TimeSpan[] trainWindows,
        TimeSpan[] testWindows,
        FoldMode mode,
        Func<Fold, double> fitnessCallback,
        int warmupPoints,
        TimeSpan embargo,
        int minimumFolds,
        int? maxFoldsPerCell,
        CompositeScorer? scorer,
        Func<Fold, IEnumerable<string>>? labeler,
        int innerFolds,
        CancellationToken cancellationToken)
    {
        var cells = new List<GridCellResult>();

        // Per-segment, per-cell fitness accumulator: segment -> (trainWindow, testWindow) -> List<double>
        Dictionary<string, Dictionary<(TimeSpan Train, TimeSpan Test), List<double>>>? segmentAccumulator =
            labeler is not null ? new(StringComparer.Ordinal) : null;

        foreach (var trainWindow in trainWindows)
        {
            foreach (var testWindow in testWindows)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var folds = GenerateFolds(
                    totalDataPoints,
                    dataFrequency,
                    trainWindow,
                    testWindow,
                    embargo,
                    warmupPoints,
                    maxFoldsPerCell,
                    mode);

                if (folds.Count == 0)
                {
                    continue;
                }

                var foldFitnesses = new double[folds.Count];
                for (var i = 0; i < folds.Count; i++)
                {
                    foldFitnesses[i] = EvaluateFold(folds[i], fitnessCallback, innerFolds);

                    if (segmentAccumulator is not null)
                    {
                        foreach (var label in labeler!(folds[i]))
                        {
                            if (!segmentAccumulator.TryGetValue(label, out var cellMap))
                            {
                                cellMap = [];
                                segmentAccumulator[label] = cellMap;
                            }

                            var key = (trainWindow, testWindow);
                            if (!cellMap.TryGetValue(key, out var fitnessList))
                            {
                                fitnessList = [];
                                cellMap[key] = fitnessList;
                            }

                            fitnessList.Add(foldFitnesses[i]);
                        }
                    }
                }

                var consistency = Consistency.Compute(foldFitnesses);
                var meanFitness = foldFitnesses.Average();
                var worstFold = foldFitnesses.Min();

                cells.Add(new GridCellResult
                {
                    TrainWindow = trainWindow,
                    TestWindow = testWindow,
                    TrainDataPoints = Validation.ToIndexCount(trainWindow, dataFrequency),
                    TestDataPoints = Validation.ToIndexCount(testWindow, dataFrequency),
                    MeanFitness = meanFitness,
                    Consistency = consistency,
                    FoldCount = folds.Count,
                    WorstFold = worstFold,
                    FoldFitnesses = foldFitnesses,
                });
            }
        }

        var filtered = cells
            .Where(c => c.FoldCount >= minimumFolds)
            .ToList();

        if (scorer is not null)
        {
            filtered = scorer.Score(filtered)
                .OrderByDescending(c => c.CompositeScore)
                .ToList();
        }
        else
        {
            filtered = filtered
                .OrderByDescending(c => c.MeanFitness)
                .ToList();
        }

        // Build per-segment results
        var segmentResults = new Dictionary<string, IReadOnlyList<GridCellResult>>(StringComparer.Ordinal);
        var bestPerSegment = new Dictionary<string, GridCellResult>(StringComparer.Ordinal);

        if (segmentAccumulator is not null)
        {
            foreach (var (segment, cellMap) in segmentAccumulator)
            {
                var segmentCells = new List<GridCellResult>();
                foreach (var ((train, test), fitnesses) in cellMap)
                {
                    if (fitnesses.Count < minimumFolds)
                    {
                        continue;
                    }

                    var arr = fitnesses.ToArray();
                    segmentCells.Add(new GridCellResult
                    {
                        TrainWindow = train,
                        TestWindow = test,
                        TrainDataPoints = Validation.ToIndexCount(train, dataFrequency),
                        TestDataPoints = Validation.ToIndexCount(test, dataFrequency),
                        MeanFitness = arr.Average(),
                        Consistency = Consistency.Compute(arr),
                        FoldCount = arr.Length,
                        WorstFold = arr.Min(),
                        FoldFitnesses = arr,
                    });
                }

                if (segmentCells.Count == 0)
                {
                    continue;
                }

                // Per-segment scoring: scorer normalizes within segment independently
                if (scorer is not null)
                {
                    segmentCells = scorer.Score(segmentCells)
                        .OrderByDescending(c => c.CompositeScore)
                        .ToList();
                }
                else
                {
                    segmentCells = segmentCells
                        .OrderByDescending(c => c.MeanFitness)
                        .ToList();
                }

                segmentResults[segment] = segmentCells;
                bestPerSegment[segment] = segmentCells[0];
            }
        }

        return new GridSearchResult
        {
            Cells = filtered,
            SegmentResults = segmentResults,
            BestPerSegment = bestPerSegment,
        };
    }

    private static IReadOnlyList<Fold> GenerateFolds(
        int totalDataPoints,
        TimeSpan dataFrequency,
        TimeSpan trainWindow,
        TimeSpan testWindow,
        TimeSpan embargo,
        int warmupPoints,
        int? maxFolds,
        FoldMode mode)
    {
        return mode switch
        {
            FoldMode.BackwardLooking => BackwardLookingFoldGenerator.Generate(
                new BackwardLookingOptions
                {
                    TotalDataPoints = totalDataPoints,
                    DataFrequency = dataFrequency,
                    TrainingWindow = trainWindow,
                    TestWindow = testWindow,
                    Embargo = embargo,
                    WarmupPoints = warmupPoints,
                    MaxFolds = maxFolds,
                }),
            FoldMode.ForwardLooking => ForwardLookingFoldGenerator.Generate(
                new ForwardLookingOptions
                {
                    TotalDataPoints = totalDataPoints,
                    DataFrequency = dataFrequency,
                    TrainingWindow = trainWindow,
                    TestWindow = testWindow,
                    Embargo = embargo,
                    WarmupPoints = warmupPoints,
                    MaxFolds = maxFolds,
                }),
            _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, "Unsupported fold mode."),
        };
    }

    private static double EvaluateFold(
        Fold outerFold,
        Func<Fold, double> fitnessCallback,
        int innerFolds)
    {
        if (innerFolds < 2)
        {
            return fitnessCallback(outerFold);
        }

        var trainLength = outerFold.TrainEnd - outerFold.TrainStart;
        var subFoldSize = trainLength / innerFolds;

        if (subFoldSize <= 0)
        {
            return fitnessCallback(outerFold);
        }

        var weightedSum = 0.0;
        var totalWeight = 0;

        for (var k = 0; k < innerFolds; k++)
        {
            var holdOutStart = outerFold.TrainStart + (k * subFoldSize);
            var holdOutEnd = k == innerFolds - 1
                ? outerFold.TrainEnd
                : holdOutStart + subFoldSize;

            var innerFold = new Fold
            {
                FoldIndex = k,
                TrainStart = outerFold.TrainStart,
                TrainEnd = outerFold.TrainEnd,
                TestStart = holdOutStart,
                TestEnd = holdOutEnd,
                EmbargoStart = holdOutEnd,
                EmbargoEnd = holdOutEnd,
            };

            var subFitness = fitnessCallback(innerFold);
            var weight = holdOutEnd - holdOutStart;
            weightedSum += subFitness * weight;
            totalWeight += weight;
        }

        return weightedSum / totalWeight;
    }
}
