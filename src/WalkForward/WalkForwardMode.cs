namespace WalkForward;

/// <summary>
/// Specifies the walk-forward fold generation mode.
/// </summary>
public enum WalkForwardMode
{
    /// <summary>
    /// Anchored mode walks backwards from the end of data with expanding training windows.
    /// Each fold uses a training window that starts at a fixed distance before the test window,
    /// and test windows are laid out from most recent to oldest.
    /// </summary>
    Anchored,

    /// <summary>
    /// Rolling mode walks forwards from the start of data with fixed-size training windows.
    /// Each fold uses the same training window size, sliding forward through the data.
    /// </summary>
    Rolling,
}
