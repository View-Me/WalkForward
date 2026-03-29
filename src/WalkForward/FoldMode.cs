namespace WalkForward;

/// <summary>
/// Specifies the fold generation mode.
/// </summary>
public enum FoldMode
{
    /// <summary>
    /// Backward-looking mode walks backwards from the end of data.
    /// Each fold uses a training window that starts at a fixed distance before the test window,
    /// and test windows are laid out from most recent to oldest.
    /// </summary>
    BackwardLooking,

    /// <summary>
    /// Forward-looking mode walks forwards from the start of data.
    /// Each fold uses the same training window size, sliding forward through the data.
    /// </summary>
    ForwardLooking,
}
