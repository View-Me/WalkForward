using WalkForward.GridSearch;

namespace WalkForward.Scoring;

/// <summary>
/// Computes composite scores from fitness, consistency, and smoothness components.
/// Configure weights via <see cref="WithWeights"/>, then call <see cref="Score"/> to
/// produce ranked grid cells.
/// </summary>
/// <example>
/// <code>
/// var scored = new CompositeScorer()
///     .WithWeights(fitnessWeight: 0.6, consistencyWeight: 0.25, smoothnessWeight: 0.15)
///     .Score(cells);
/// var best = scored.OrderByDescending(c => c.CompositeScore).First();
/// </code>
/// </example>
public sealed class CompositeScorer
{
    private const double WeightTolerance = 0.001;

    private double _fitnessWeight;
    private double _consistencyWeight;
    private double _smoothnessWeight;
    private bool _weightsConfigured;

    /// <summary>
    /// Sets the scoring weights for each component. All weights must be non-negative
    /// and must sum to approximately 1.0 (tolerance 0.001).
    /// </summary>
    /// <param name="fitnessWeight">Weight for the normalized fitness component.</param>
    /// <param name="consistencyWeight">Weight for the consistency percentage component (0-1 scale).</param>
    /// <param name="smoothnessWeight">Weight for the smoothness bonus component.</param>
    /// <returns>This scorer for fluent chaining.</returns>
    /// <exception cref="ArgumentException">Thrown when any weight is negative or weights don't sum to ~1.0.</exception>
    public CompositeScorer WithWeights(
        double fitnessWeight,
        double consistencyWeight,
        double smoothnessWeight)
    {
        if (fitnessWeight < 0 || consistencyWeight < 0 || smoothnessWeight < 0)
        {
            throw new ArgumentException(
                "All weights must be non-negative.",
                nameof(fitnessWeight));
        }

        var sum = fitnessWeight + consistencyWeight + smoothnessWeight;
        if (sum < WeightTolerance)
        {
            throw new ArgumentException(
                "Weights must sum to a positive value.",
                nameof(fitnessWeight));
        }

        if (Math.Abs(sum - 1.0) > WeightTolerance)
        {
            throw new ArgumentException(
                $"Weights must sum to approximately 1.0 (got {sum:F4}). " +
                $"Normalize your weights: ({fitnessWeight / sum:F4}, {consistencyWeight / sum:F4}, {smoothnessWeight / sum:F4}).",
                nameof(fitnessWeight));
        }

        _fitnessWeight = fitnessWeight;
        _consistencyWeight = consistencyWeight;
        _smoothnessWeight = smoothnessWeight;
        _weightsConfigured = true;
        return this;
    }

    /// <summary>
    /// Scores a collection of grid cells by computing smoothness bonus and composite score
    /// for each cell. Fitness values are normalized internally by dividing by the maximum
    /// fitness across all cells.
    /// </summary>
    /// <param name="cells">Grid cells to score. Grid topology is inferred from distinct
    /// TrainWindow/TestWindow values.</param>
    /// <returns>New <see cref="GridCellResult"/> instances with
    /// <see cref="GridCellResult.SmoothnessBonus"/> and <see cref="GridCellResult.CompositeScore"/>
    /// populated. All other fields are preserved from the input cells.</returns>
    /// <exception cref="InvalidOperationException">Thrown when <see cref="WithWeights"/> has not been called.</exception>
    public IReadOnlyList<GridCellResult> Score(IReadOnlyList<GridCellResult> cells)
    {
        if (!_weightsConfigured)
        {
            throw new InvalidOperationException(
                "WithWeights must be called before Score. " +
                "Example: new CompositeScorer().WithWeights(0.6, 0.25, 0.15).Score(cells)");
        }

        if (cells.Count == 0)
        {
            return Array.Empty<GridCellResult>();
        }

        // Build grid index mapping from distinct sorted TrainWindow/TestWindow values
        var trainWindows = cells.Select(c => c.TrainWindow).Distinct().OrderBy(t => t).ToList();
        var testWindows = cells.Select(c => c.TestWindow).Distinct().OrderBy(t => t).ToList();

        // Build flat cell list with grid coordinates for Smoothness computation
        var gridCells = new List<(int TrainIndex, int TestIndex, double Fitness)>(cells.Count);
        var cellIndices = new (int TrainIndex, int TestIndex)[cells.Count];

        for (var i = 0; i < cells.Count; i++)
        {
            var trainIdx = trainWindows.IndexOf(cells[i].TrainWindow);
            var testIdx = testWindows.IndexOf(cells[i].TestWindow);
            gridCells.Add((trainIdx, testIdx, cells[i].MeanFitness));
            cellIndices[i] = (trainIdx, testIdx);
        }

        // Find max fitness for normalization (guard against all-zero)
        var maxFitness = 0.0;
        for (var i = 0; i < cells.Count; i++)
        {
            if (cells[i].MeanFitness > maxFitness)
            {
                maxFitness = cells[i].MeanFitness;
            }
        }

        if (maxFitness < double.Epsilon)
        {
            maxFitness = 1.0;
        }

        // Score each cell
        var result = new GridCellResult[cells.Count];
        for (var i = 0; i < cells.Count; i++)
        {
            var smoothness = Smoothness.Compute(
                cellIndices[i].TrainIndex,
                cellIndices[i].TestIndex,
                cells[i].MeanFitness,
                gridCells);

            var normalizedFitness = cells[i].MeanFitness / maxFitness;
            var consistencyFraction = cells[i].Consistency.ConsistencyPercent / 100.0;

            var composite =
                (normalizedFitness * _fitnessWeight) +
                (consistencyFraction * _consistencyWeight) +
                (smoothness * _smoothnessWeight);

            result[i] = cells[i] with
            {
                SmoothnessBonus = smoothness,
                CompositeScore = composite,
            };
        }

        return result;
    }
}
