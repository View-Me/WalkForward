using FluentAssertions;
using NUnit.Framework;

namespace WalkForward.Tests.Unit.Metrics;

[TestFixture]
public class EmptyInputTests
{
    [Test]
    public void Compute_empty_array_returns_defaults_without_throwing()
    {
        var result = Consistency.Compute(ReadOnlySpan<double>.Empty);

        result.ConsistencyPercent.Should().Be(0);
        result.MagnitudeConsistency.Should().Be(0);
        result.WorstFold.Should().Be(0);
        result.AverageReturn.Should().Be(0);
    }

    [Test]
    public void Compute_single_positive_element_ConsistencyPercent_is_100()
    {
        double[] returns = [0.05];

        var result = Consistency.Compute(returns);

        result.ConsistencyPercent.Should().BeApproximately(100.0, 0.001);
    }

    [Test]
    public void ForClassifier_empty_arrays_returns_defaults_without_throwing()
    {
        var result = Consistency.ForClassifier(
            ReadOnlySpan<double>.Empty,
            ReadOnlySpan<double>.Empty,
            baselineAccuracy: 0.5);

        result.AverageAccuracy.Should().Be(0);
        result.AverageLogLoss.Should().Be(0);
        result.ConsistencyAboveBaseline.Should().Be(0);
        result.FoldCount.Should().Be(0);
    }
}
