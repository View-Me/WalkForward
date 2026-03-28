namespace WalkForward.Internal;

/// <summary>
/// Shared validation helpers for options and index conversion.
/// </summary>
internal static class Validation
{
    /// <summary>
    /// Ensures an integer value is strictly positive.
    /// </summary>
    internal static void EnsurePositive(int value, string paramName)
    {
        if (value <= 0)
        {
            throw new ArgumentOutOfRangeException(paramName, value, $"{paramName} must be positive.");
        }
    }

    /// <summary>
    /// Ensures a <see cref="TimeSpan"/> value is strictly positive.
    /// </summary>
    internal static void EnsurePositive(TimeSpan value, string paramName)
    {
        if (value <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(paramName, value, $"{paramName} must be positive.");
        }
    }

    /// <summary>
    /// Ensures an integer value is non-negative.
    /// </summary>
    internal static void EnsureNonNegative(int value, string paramName)
    {
        if (value < 0)
        {
            throw new ArgumentOutOfRangeException(paramName, value, $"{paramName} must be non-negative.");
        }
    }

    /// <summary>
    /// Ensures a <see cref="TimeSpan"/> value is non-negative.
    /// </summary>
    internal static void EnsureNonNegative(TimeSpan value, string paramName)
    {
        if (value < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(paramName, value, $"{paramName} must be non-negative.");
        }
    }

    /// <summary>
    /// Converts a time duration to an index count using floor division.
    /// Used for training and test window sizes.
    /// </summary>
    internal static int ToIndexCount(TimeSpan duration, TimeSpan frequency) =>
        (int)(duration / frequency);

    /// <summary>
    /// Converts an embargo duration to an index count using ceiling division.
    /// Always rounds up to prevent lookahead bias (Pitfall 3: fractional embargo).
    /// </summary>
    internal static int ToEmbargoIndexCount(TimeSpan embargo, TimeSpan frequency) =>
        (int)Math.Ceiling(embargo / frequency);
}
