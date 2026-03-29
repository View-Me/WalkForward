namespace WalkForward.Segments;

/// <summary>
/// A fold paired with an arbitrary string label assigned by a user-supplied labeler callback.
/// Labels are domain-agnostic (e.g., "high-volatility", "Q1-2024", "regime-A").
/// </summary>
/// <param name="Fold">The underlying fold.</param>
/// <param name="Label">The user-assigned label for this fold.</param>
public sealed record LabeledFold(Fold Fold, string Label);
