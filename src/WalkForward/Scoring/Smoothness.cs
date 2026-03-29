namespace WalkForward.Scoring;

/// <summary>
/// Computes smoothness bonus by comparing a cell's fitness to its grid neighbors.
/// </summary>
/// <remarks>
/// Assumes grid cells are indexed on regular axes. For irregular spacing,
/// neighbor distances may not reflect actual proximity.
/// </remarks>
public static class Smoothness
{
    /// <summary>
    /// Computes the smoothness bonus for a grid cell at the given coordinates.
    /// Neighbors are cells within Manhattan distance 1 on each axis (up to 8 neighbors).
    /// </summary>
    /// <param name="trainIndex">Zero-based train axis index of the target cell.</param>
    /// <param name="testIndex">Zero-based test axis index of the target cell.</param>
    /// <param name="cellFitness">Fitness value of the target cell.</param>
    /// <param name="allCells">All grid cells with their coordinates and fitness values.</param>
    /// <returns>Smoothness bonus clamped to [0.0, 1.0]. Returns 0.0 when no neighbors exist.</returns>
    public static double Compute(
        int trainIndex,
        int testIndex,
        double cellFitness,
        IReadOnlyList<(int TrainIndex, int TestIndex, double Fitness)> allCells)
    {
        var neighborSum = 0.0;
        var neighborCount = 0;

        for (var i = 0; i < allCells.Count; i++)
        {
            var (nTrain, nTest, nFitness) = allCells[i];

            if (nTrain == trainIndex && nTest == testIndex)
            {
                continue;
            }

            if (Math.Abs(nTrain - trainIndex) <= 1 && Math.Abs(nTest - testIndex) <= 1)
            {
                neighborSum += nFitness;
                neighborCount++;
            }
        }

        if (neighborCount == 0)
        {
            return 0.0;
        }

        var neighborAvg = neighborSum / neighborCount;
        return Math.Clamp(neighborAvg / Math.Max(1.0, Math.Abs(cellFitness)), 0.0, 1.0);
    }
}
