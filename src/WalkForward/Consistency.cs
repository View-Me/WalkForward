namespace WalkForward;

/// <summary>
/// Computes aggregate consistency metrics from per-fold validation results.
/// </summary>
public static class Consistency
{
    /// <summary>
    /// Computes returns-based consistency metrics from per-fold returns.
    /// </summary>
    /// <param name="foldReturns">Per-fold return values. Positive values indicate profitable folds.</param>
    /// <returns>Aggregated consistency metrics including consistency percentage, magnitude consistency,
    /// worst fold return, and average return.</returns>
    public static ConsistencyMetrics Compute(ReadOnlySpan<double> foldReturns)
    {
        if (foldReturns.IsEmpty)
        {
            return new ConsistencyMetrics(0, 0, 0, 0);
        }

        var positiveCount = 0;
        var sum = 0.0;
        var worstFold = double.MaxValue;
        var negativeSquaredSum = 0.0;
        var negativeCount = 0;

        for (var i = 0; i < foldReturns.Length; i++)
        {
            var r = foldReturns[i];
            sum += r;

            if (r > 0)
            {
                positiveCount++;
            }

            if (r < worstFold)
            {
                worstFold = r;
            }

            if (r < 0)
            {
                negativeSquaredSum += r * r;
                negativeCount++;
            }
        }

        var consistencyPercent = ((double)positiveCount / foldReturns.Length) * 100.0;
        var averageReturn = sum / foldReturns.Length;

        var downsideDeviation = negativeCount > 0
            ? Math.Sqrt(negativeSquaredSum / negativeCount)
            : 1.0;

        var sortinoLike = downsideDeviation > 0
            ? averageReturn / downsideDeviation
            : 0.0;

        var magnitudeConsistency = Math.Clamp((sortinoLike + 2.0) / 4.0, 0.0, 1.0);

        return new ConsistencyMetrics(
            consistencyPercent,
            magnitudeConsistency,
            worstFold,
            averageReturn);
    }

    /// <summary>
    /// Computes classifier-specific consistency metrics from per-fold accuracy and log-loss values.
    /// </summary>
    /// <param name="foldAccuracies">Per-fold accuracy values.</param>
    /// <param name="foldLogLosses">Per-fold log-loss values. Must have the same length as <paramref name="foldAccuracies"/>.</param>
    /// <param name="baselineAccuracy">The baseline accuracy to compare against (e.g., 0.5 for binary classification).</param>
    /// <returns>Classifier consistency metrics including average accuracy, average log-loss,
    /// and percentage of folds above baseline.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="foldAccuracies"/> and <paramref name="foldLogLosses"/> have different lengths.
    /// </exception>
    public static ClassifierConsistencyMetrics ForClassifier(
        ReadOnlySpan<double> foldAccuracies,
        ReadOnlySpan<double> foldLogLosses,
        double baselineAccuracy)
    {
        if (foldAccuracies.Length != foldLogLosses.Length)
        {
            throw new ArgumentException(
                $"Accuracy count ({foldAccuracies.Length}) must equal log-loss count ({foldLogLosses.Length}).",
                nameof(foldLogLosses));
        }

        if (foldAccuracies.IsEmpty)
        {
            return new ClassifierConsistencyMetrics(0, 0, 0, 0);
        }

        var accuracySum = 0.0;
        var logLossSum = 0.0;
        var aboveBaselineCount = 0;

        for (var i = 0; i < foldAccuracies.Length; i++)
        {
            accuracySum += foldAccuracies[i];
            logLossSum += foldLogLosses[i];

            if (foldAccuracies[i] > baselineAccuracy)
            {
                aboveBaselineCount++;
            }
        }

        var averageAccuracy = accuracySum / foldAccuracies.Length;
        var averageLogLoss = logLossSum / foldLogLosses.Length;
        var consistencyAboveBaseline = ((double)aboveBaselineCount / foldAccuracies.Length) * 100.0;

        return new ClassifierConsistencyMetrics(
            averageAccuracy,
            averageLogLoss,
            consistencyAboveBaseline,
            foldAccuracies.Length);
    }
}
