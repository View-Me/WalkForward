namespace WalkForward.Internal;

/// <summary>
/// Generates fold boundaries for anchored walk-forward validation.
/// Walks backwards from the end of data, producing folds with the most
/// recent test window first. Training windows have a fixed size for each fold.
/// </summary>
internal static class AnchoredFoldGenerator
{
    /// <summary>
    /// Generates fold boundaries using anchored walk-forward logic.
    /// </summary>
    /// <param name="options">Configuration for anchored fold generation.</param>
    /// <returns>An ordered list of folds, with fold 0 being the most recent.</returns>
    internal static IReadOnlyList<Fold> Generate(AnchoredOptions options)
    {
        options.Validate();

        var trainIndexCount = Validation.ToIndexCount(options.TrainingWindow, options.DataFrequency);
        var testIndexCount = Validation.ToIndexCount(options.TestWindow, options.DataFrequency);
        var embargoIndexCount = Validation.ToEmbargoIndexCount(options.Embargo, options.DataFrequency);

        var maxFolds = options.MaxFolds
            ?? Math.Max(0, (options.TotalDataPoints - trainIndexCount - embargoIndexCount) / testIndexCount);

        var folds = new List<Fold>();

        for (var i = 0; i < maxFolds; i++)
        {
            var testEnd = options.TotalDataPoints - (i * testIndexCount);
            var testStart = testEnd - testIndexCount;
            var embargoEnd = testStart;
            var embargoStart = embargoEnd - embargoIndexCount;
            var trainEnd = embargoStart;
            var trainStart = trainEnd - trainIndexCount;

            if (trainStart < options.WarmupPoints)
            {
                continue;
            }

            folds.Add(new Fold
            {
                FoldIndex = i,
                TrainStart = trainStart,
                TrainEnd = trainEnd,
                TestStart = testStart,
                TestEnd = testEnd,
                EmbargoStart = embargoStart,
                EmbargoEnd = embargoEnd,
            });
        }

        return folds;
    }
}
