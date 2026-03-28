namespace WalkForward;

/// <summary>
/// Entry point for building walk-forward cross-validation folds.
/// Configure common parameters, then select a mode via <see cref="Anchored"/> or <see cref="Rolling"/>.
/// </summary>
/// <example>
/// <code>
/// var folds = new WalkForwardBuilder()
///     .WithDataPoints(10000)
///     .WithDataFrequency(TimeSpan.FromMinutes(15))
///     .Anchored()
///     .WithTrainingWindow(TimeSpan.FromDays(90))
///     .WithTestWindow(TimeSpan.FromDays(7))
///     .Build();
/// </code>
/// </example>
public sealed class WalkForwardBuilder
{
    private int _totalDataPoints;
    private TimeSpan _dataFrequency;

    /// <summary>
    /// Sets the total number of data points in the dataset.
    /// </summary>
    /// <param name="count">Total data points. Must be positive.</param>
    /// <returns>This builder for chaining.</returns>
    public WalkForwardBuilder WithDataPoints(int count)
    {
        _totalDataPoints = count;
        return this;
    }

    /// <summary>
    /// Sets the time interval between consecutive data points.
    /// </summary>
    /// <param name="frequency">Duration between data points (e.g., 15 minutes for 15-minute candles).</param>
    /// <returns>This builder for chaining.</returns>
    public WalkForwardBuilder WithDataFrequency(TimeSpan frequency)
    {
        _dataFrequency = frequency;
        return this;
    }

    /// <summary>
    /// Selects anchored (expanding window) walk-forward mode.
    /// The training window has a fixed size per fold, and folds walk backwards from the end of data,
    /// with the most recent data tested first.
    /// </summary>
    /// <returns>An <see cref="AnchoredBuilder"/> for configuring anchored-specific parameters.</returns>
    public AnchoredBuilder Anchored() => new(_totalDataPoints, _dataFrequency);

    /// <summary>
    /// Selects rolling (fixed window) walk-forward mode.
    /// Both windows slide forward through the data with a configurable stride.
    /// </summary>
    /// <returns>A <see cref="RollingBuilder"/> for configuring rolling-specific parameters.</returns>
    public RollingBuilder Rolling() => new(_totalDataPoints, _dataFrequency);
}
