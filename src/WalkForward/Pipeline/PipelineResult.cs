using WalkForward.Degradation;
using WalkForward.GridSearch;

namespace WalkForward.Pipeline;

/// <summary>
/// Aggregate result of a pipeline execution, containing the full journey from coarse scan
/// through scoring, candidate selection, and optional validation.
/// </summary>
public sealed record PipelineResult
{
    /// <summary>Gets the raw grid search result from the CoarseScan stage.</summary>
    public required GridSearchResult CoarseScanResult { get; init; }

    /// <summary>Gets the grid cells after scoring. Equals <see cref="CoarseScanResult"/> cells when no scorer is configured.</summary>
    public required IReadOnlyList<GridCellResult> ScoredCells { get; init; }

    /// <summary>Gets the top candidate cells after filtering. Equals <see cref="ScoredCells"/> when no TopCandidates stage is configured.</summary>
    public required IReadOnlyList<GridCellResult> TopCandidates { get; init; }

    /// <summary>Gets per-candidate degradation results from the Validate stage. Empty when no Validate stage is configured.</summary>
    public required IReadOnlyDictionary<GridCellResult, DegradationResult> ValidationResults { get; init; }

    /// <summary>Gets the winning candidate, or null when the pipeline produces no viable candidates.</summary>
    public GridCellResult? Winner { get; init; }
}
