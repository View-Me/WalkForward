namespace WalkForward.Internal;

/// <summary>
/// Generates fold boundaries for forward-looking walk-forward validation.
/// Walks forwards from the start of data, producing folds with fixed-size
/// training windows that slide through the dataset.
/// </summary>
internal static class ForwardLookingFoldGenerator
{
    /// <summary>
    /// Generates fold boundaries using forward-looking walk-forward logic.
    /// </summary>
    /// <param name="options">Configuration for forward-looking fold generation.</param>
    /// <returns>An ordered list of folds, with fold 0 being the earliest.</returns>
    internal static IReadOnlyList<Fold> Generate(ForwardLookingOptions options)
    {
        options.Validate();

        var trainIndexCount = Validation.ToIndexCount(options.TrainingWindow, options.DataFrequency);
        var testIndexCount = Validation.ToIndexCount(options.TestWindow, options.DataFrequency);
        var embargoIndexCount = Validation.ToEmbargoIndexCount(options.Embargo, options.DataFrequency);
        var strideIndexCount = options.Stride.HasValue
            ? Validation.ToIndexCount(options.Stride.Value, options.DataFrequency)
            : testIndexCount;

        var maxFolds = options.MaxFolds
            ?? Math.Max(0, ((options.TotalDataPoints - trainIndexCount - embargoIndexCount - testIndexCount) / strideIndexCount) + 1);

        var folds = new List<Fold>();
        var foldIndex = 0;

        while (foldIndex < maxFolds)
        {
            var trainStart = foldIndex * strideIndexCount;
            var trainEnd = trainStart + trainIndexCount;
            var embargoStart = trainEnd;
            var embargoEnd = embargoStart + embargoIndexCount;
            var testStart = embargoEnd;
            var testEnd = testStart + testIndexCount;

            if (testEnd > options.TotalDataPoints)
            {
                break;
            }

            folds.Add(new Fold
            {
                FoldIndex = foldIndex,
                TrainStart = trainStart,
                TrainEnd = trainEnd,
                TestStart = testStart,
                TestEnd = testEnd,
                EmbargoStart = embargoStart,
                EmbargoEnd = embargoEnd,
            });

            foldIndex++;
        }

        return folds;
    }
}
