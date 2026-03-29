namespace WalkForward.Pipeline;

/// <summary>
/// Reports progress of a pipeline execution, including per-stage and overall completion.
/// </summary>
/// <example>
/// <code>
/// var progress = new Progress&lt;PipelineProgress&gt;(p =>
///     Console.WriteLine($"[{p.StageName}] {p.OverallPercent:F0}% complete"));
/// var result = pipeline.Build(CancellationToken.None, progress);
/// </code>
/// </example>
public sealed record PipelineProgress
{
    /// <summary>Gets the name of the currently executing stage (e.g., "CoarseScan", "Score", "TopCandidates", "Validate").</summary>
    public required string StageName { get; init; }

    /// <summary>Gets the zero-based index of the currently executing stage.</summary>
    public required int StageIndex { get; init; }

    /// <summary>Gets the total number of configured stages in the pipeline.</summary>
    public required int TotalStages { get; init; }

    /// <summary>Gets the completion percentage of the current stage (0 to 100).</summary>
    public required double StagePercent { get; init; }

    /// <summary>Gets the overall completion percentage across all stages (0 to 100).</summary>
    public required double OverallPercent { get; init; }
}
