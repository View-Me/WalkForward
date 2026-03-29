using WalkForward.GridSearch;

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
    /// <param name="cancellationToken">Token checked between cell iterations.</param>
    /// <returns>A <see cref="GridSearchResult"/> with cells sorted by mean fitness descending.</returns>
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
        CancellationToken cancellationToken)
    {
        var cells = new List<GridCellResult>();

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
                    foldFitnesses[i] = fitnessCallback(folds[i]);
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
            .OrderByDescending(c => c.MeanFitness)
            .ToList();

        return new GridSearchResult { Cells = filtered };
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
}
