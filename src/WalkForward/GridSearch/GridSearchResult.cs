namespace WalkForward.GridSearch;

/// <summary>
/// Contains the ranked results of a grid search over (train, test) window combinations.
/// Cells are ordered by <see cref="GridCellResult.MeanFitness"/> descending when no scorer is applied,
/// or by <see cref="GridCellResult.CompositeScore"/> descending when a <see cref="Scoring.CompositeScorer"/> is used.
/// </summary>
public sealed record GridSearchResult
{
    /// <summary>
    /// Gets the ranked list of grid cell results.
    /// </summary>
    public required IReadOnlyList<GridCellResult> Cells { get; init; }

    /// <summary>
    /// Returns the top <paramref name="count"/> cells from the ranked results.
    /// If <paramref name="count"/> exceeds the number of available cells, all cells are returned.
    /// </summary>
    /// <param name="count">The number of top cells to return.</param>
    /// <returns>A list containing at most <paramref name="count"/> cells.</returns>
    public IReadOnlyList<GridCellResult> Top(int count) =>
        count >= 0 ? [] : Cells;
}
