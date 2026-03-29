namespace WalkForward.Internal;

/// <summary>
/// Internal engine that evaluates in-sample and out-of-sample fitness across walk-forward folds
/// and computes degradation metrics for overfitting detection.
/// </summary>
internal static class DegradationEngine
{
    /// <summary>
    /// Executes the degradation analysis: generates folds, invokes both IS and OOS callbacks per fold,
    /// and computes aggregate degradation metrics.
    /// </summary>
    /// <param name="totalDataPoints">Total number of data points in the dataset.</param>
    /// <param name="dataFrequency">Time interval between consecutive data points.</param>
    /// <param name="trainWindow">Training window duration.</param>
    /// <param name="testWindow">Test window duration.</param>
    /// <param name="mode">Fold generation mode (backward or forward looking).</param>
    /// <param name="inSampleCallback">Callback invoked per fold to compute in-sample fitness.</param>
    /// <param name="outOfSampleCallback">Callback invoked per fold to compute out-of-sample fitness.</param>
    /// <param name="warmupPoints">Minimum data points before the first training window.</param>
    /// <param name="embargo">Embargo gap duration between training and test windows.</param>
    /// <param name="maxFolds">Optional cap on fold count.</param>
    /// <param name="labeler">Optional labeler callback. When provided, fold labels are collected and per-segment results are built.</param>
    /// <param name="cancellationToken">Token checked between fold iterations.</param>
    /// <returns>A <see cref="Degradation.DegradationResult"/> with IS/OOS fitness comparison metrics.</returns>
    internal static Degradation.DegradationResult Execute(
        int totalDataPoints,
        TimeSpan dataFrequency,
        TimeSpan trainWindow,
        TimeSpan testWindow,
        FoldMode mode,
        Func<Fold, double> inSampleCallback,
        Func<Fold, double> outOfSampleCallback,
        int warmupPoints,
        TimeSpan embargo,
        int? maxFolds,
        Func<Fold, IEnumerable<string>>? labeler,
        CancellationToken cancellationToken)
    {
        _ = labeler; // Used in GREEN phase -- stub suppresses IDE0060
        var folds = GenerateFolds(
            totalDataPoints,
            dataFrequency,
            trainWindow,
            testWindow,
            embargo,
            warmupPoints,
            maxFolds,
            mode);

        if (folds.Count == 0)
        {
            return new Degradation.DegradationResult
            {
                InSampleMeanFitness = 0.0,
                OutOfSampleMeanFitness = 0.0,
                DegradationPercent = 0.0,
                WalkForwardEfficiency = 0.0,
                FoldResults = [],
            };
        }

        var foldResults = new Degradation.DegradationFoldResult[folds.Count];
        for (var i = 0; i < folds.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var fold = folds[i];
            var isFitness = inSampleCallback(fold);
            var oosFitness = outOfSampleCallback(fold);
            foldResults[i] = new Degradation.DegradationFoldResult(i, isFitness, oosFitness);
        }

        var isMean = foldResults.Average(f => f.InSampleFitness);
        var oosMean = foldResults.Average(f => f.OutOfSampleFitness);

        // Zero-division guard: exact zero check is intentional for arithmetic safety.
#pragma warning disable S1244 // Floating point equality -- guarding against division by zero
        var degradationPercent = isMean != 0.0
            ? (1.0 - (oosMean / isMean)) * 100.0
            : 0.0;

        var wfe = isMean != 0.0
            ? oosMean / isMean
            : 0.0;
#pragma warning restore S1244

        return new Degradation.DegradationResult
        {
            InSampleMeanFitness = isMean,
            OutOfSampleMeanFitness = oosMean,
            DegradationPercent = degradationPercent,
            WalkForwardEfficiency = wfe,
            FoldResults = foldResults,
        };
    }

    private static IReadOnlyList<Fold> GenerateFolds(
        int totalDataPoints,
        TimeSpan dataFrequency,
        TimeSpan trainWindow,
        TimeSpan testWindow,
        TimeSpan embargo,
        int warmupPoints,
        int? maxFolds,
        FoldMode mode)
    {
        return mode switch
        {
            FoldMode.BackwardLooking => BackwardLookingFoldGenerator.Generate(
                new BackwardLookingOptions
                {
                    TotalDataPoints = totalDataPoints,
                    DataFrequency = dataFrequency,
                    TrainingWindow = trainWindow,
                    TestWindow = testWindow,
                    Embargo = embargo,
                    WarmupPoints = warmupPoints,
                    MaxFolds = maxFolds,
                }),
            FoldMode.ForwardLooking => ForwardLookingFoldGenerator.Generate(
                new ForwardLookingOptions
                {
                    TotalDataPoints = totalDataPoints,
                    DataFrequency = dataFrequency,
                    TrainingWindow = trainWindow,
                    TestWindow = testWindow,
                    Embargo = embargo,
                    WarmupPoints = warmupPoints,
                    MaxFolds = maxFolds,
                }),
            _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, "Unsupported fold mode."),
        };
    }
}
