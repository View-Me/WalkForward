using WalkForward.GridSearch;
using WalkForward.Internal;

namespace WalkForward.Degradation;

/// <summary>
/// Static convenience methods for running degradation analysis across grid search results.
/// </summary>
public static class DegradationAnalysis
{
    /// <summary>
    /// Runs degradation analysis for each cell in a grid search result.
    /// Regenerates folds per cell using the provided data parameters and evaluates
    /// both in-sample and out-of-sample callbacks per fold.
    /// </summary>
    /// <param name="gridResult">The grid search result containing cells to analyze.</param>
    /// <param name="totalDataPoints">Total number of data points in the dataset.</param>
    /// <param name="dataFrequency">Time interval between consecutive data points.</param>
    /// <param name="mode">Fold generation mode (backward or forward looking).</param>
    /// <param name="inSampleCallback">Callback invoked per fold to compute in-sample fitness.</param>
    /// <param name="outOfSampleCallback">Callback invoked per fold to compute out-of-sample fitness.</param>
    /// <param name="warmupPoints">Minimum data points before the first training window. Defaults to 0.</param>
    /// <param name="embargo">Embargo gap duration between training and test windows. Defaults to zero.</param>
    /// <param name="maxFolds">Optional cap on folds per cell.</param>
    /// <param name="cancellationToken">Token to cancel the analysis between cell iterations.</param>
    /// <returns>A list of tuples pairing each grid cell with its degradation result.</returns>
    public static IReadOnlyList<(GridCellResult Cell, DegradationResult Degradation)> ForGrid(
        GridSearchResult gridResult,
        int totalDataPoints,
        TimeSpan dataFrequency,
        FoldMode mode,
        Func<Fold, double> inSampleCallback,
        Func<Fold, double> outOfSampleCallback,
        int warmupPoints = 0,
        TimeSpan embargo = default,
        int? maxFolds = null,
        CancellationToken cancellationToken = default)
    {
        var results = new List<(GridCellResult Cell, DegradationResult Degradation)>();

        foreach (var cell in gridResult.Cells)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var degradation = DegradationEngine.Execute(
                totalDataPoints,
                dataFrequency,
                cell.TrainWindow,
                cell.TestWindow,
                mode,
                inSampleCallback,
                outOfSampleCallback,
                warmupPoints,
                embargo,
                maxFolds,
                null,
                cancellationToken);

            results.Add((cell, degradation));
        }

        return results;
    }
}
