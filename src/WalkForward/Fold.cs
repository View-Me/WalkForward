namespace WalkForward;

/// <summary>
/// Represents a single fold in a walk-forward validation, with index-based boundaries
/// for training, embargo, and test windows. Provides <see cref="Range"/> properties
/// for convenient array slicing (e.g., <c>data[fold.TrainRange]</c>).
/// </summary>
public sealed record Fold
{
    /// <summary>
    /// Gets the zero-based index of this fold in the sequence.
    /// </summary>
    public required int FoldIndex { get; init; }

    /// <summary>
    /// Gets the inclusive start index of the training window.
    /// </summary>
    public required int TrainStart { get; init; }

    /// <summary>
    /// Gets the exclusive end index of the training window.
    /// </summary>
    public required int TrainEnd { get; init; }

    /// <summary>
    /// Gets the inclusive start index of the test window.
    /// </summary>
    public required int TestStart { get; init; }

    /// <summary>
    /// Gets the exclusive end index of the test window.
    /// </summary>
    public required int TestEnd { get; init; }

    /// <summary>
    /// Gets the inclusive start index of the embargo gap between training and test.
    /// </summary>
    public required int EmbargoStart { get; init; }

    /// <summary>
    /// Gets the exclusive end index of the embargo gap between training and test.
    /// </summary>
    public required int EmbargoEnd { get; init; }

    /// <summary>
    /// Gets the training window as a <see cref="Range"/> for array slicing.
    /// </summary>
    public Range TrainRange => TrainStart..TrainEnd;

    /// <summary>
    /// Gets the test window as a <see cref="Range"/> for array slicing.
    /// </summary>
    public Range TestRange => TestStart..TestEnd;

    /// <summary>
    /// Gets the embargo gap as a <see cref="Range"/> for array slicing.
    /// </summary>
    public Range EmbargoRange => EmbargoStart..EmbargoEnd;

    /// <summary>
    /// Gets the number of data points in the training window.
    /// </summary>
    public int TrainLength => TrainEnd - TrainStart;

    /// <summary>
    /// Gets the number of data points in the test window.
    /// </summary>
    public int TestLength => TestEnd - TestStart;

    /// <summary>
    /// Gets the number of data points in the embargo gap.
    /// </summary>
    public int EmbargoLength => EmbargoEnd - EmbargoStart;
}
