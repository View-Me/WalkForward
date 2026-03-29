namespace WalkForward.Scoring;

/// <summary>
/// Immutable weight triple for composite scoring. All weights must be non-negative
/// and must sum to approximately 1.0 (tolerance 0.001).
/// </summary>
/// <param name="FitnessWeight">Weight for the normalized fitness component.</param>
/// <param name="ConsistencyWeight">Weight for the consistency percentage component.</param>
/// <param name="SmoothnessWeight">Weight for the smoothness bonus component.</param>
public sealed record ScoringWeights(
    double FitnessWeight,
    double ConsistencyWeight,
    double SmoothnessWeight);
