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
    /// Gets per-segment ranked cell results. Keys are segment labels assigned by the labeler callback.
    /// Empty dictionary when no labeler is configured via
    /// <see cref="GridSearchBuilder.WithLabeler(System.Func{Fold, System.Collections.Generic.IEnumerable{string}})"/>.
    /// </summary>
    public IReadOnlyDictionary<string, IReadOnlyList<GridCellResult>> SegmentResults { get; init; }
        = new Dictionary<string, IReadOnlyList<GridCellResult>>(StringComparer.Ordinal);

    /// <summary>
    /// Gets the best-scoring cell per segment label. The best cell is the first entry in
    /// the corresponding <see cref="SegmentResults"/> list.
    /// Empty dictionary when no labeler is configured.
    /// </summary>
    public IReadOnlyDictionary<string, GridCellResult> BestPerSegment { get; init; }
        = new Dictionary<string, GridCellResult>(StringComparer.Ordinal);

    /// <summary>
    /// Returns the top <paramref name="count"/> cells from the ranked results.
    /// If <paramref name="count"/> exceeds the number of available cells, all cells are returned.
    /// </summary>
    /// <param name="count">The number of top cells to return.</param>
    /// <returns>A list containing at most <paramref name="count"/> cells.</returns>
    public IReadOnlyList<GridCellResult> Top(int count) =>
        Cells.Take(count).ToList();
}
