namespace WalkForward.Degradation;

/// <summary>
/// In-sample and out-of-sample fitness values for a single fold in a degradation analysis.
/// </summary>
/// <param name="FoldIndex">The zero-based index of this fold.</param>
/// <param name="InSampleFitness">The fitness computed on the training (in-sample) window.</param>
/// <param name="OutOfSampleFitness">The fitness computed on the test (out-of-sample) window.</param>
public sealed record DegradationFoldResult(
    int FoldIndex,
    double InSampleFitness,
    double OutOfSampleFitness);
