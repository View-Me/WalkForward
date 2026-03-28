using FluentAssertions;
using NUnit.Framework;

namespace WalkForward.Tests.Unit.Metrics;

[TestFixture]
public class ConsistencyTests
{
    private static readonly double[] MixedReturns = [0.05, -0.02, 0.08, 0.01, -0.01];

    [Test]
    public void Compute_mixed_returns_ConsistencyPercent_is_60()
    {
        var result = Consistency.Compute(MixedReturns);

        result.ConsistencyPercent.Should().BeApproximately(60.0, 0.001);
    }

    [Test]
    public void Compute_mixed_returns_AverageReturn_is_0_022()
    {
        var result = Consistency.Compute(MixedReturns);

        result.AverageReturn.Should().BeApproximately(0.022, 0.0001);
    }

    [Test]
    public void Compute_mixed_returns_WorstFold_is_minus_0_02()
    {
        var result = Consistency.Compute(MixedReturns);

        result.WorstFold.Should().BeApproximately(-0.02, 0.0001);
    }

    [Test]
    public void Compute_mixed_returns_MagnitudeConsistency_is_between_0_and_1()
    {
        var result = Consistency.Compute(MixedReturns);

        result.MagnitudeConsistency.Should().BeInRange(0.0, 1.0);
    }

    [Test]
    public void Compute_mixed_returns_MagnitudeConsistency_matches_expected_value()
    {
        // Pre-computed:
        // negativeFolds = [-0.02, -0.01]
        // downsideDeviation = sqrt(((0.02^2) + (0.01^2)) / 2) = sqrt(0.00025) = 0.015811...
        // sortinoLike = 0.022 / 0.015811 = 1.39139...
        // magnitudeConsistency = clamp((1.39139 + 2) / 4, 0, 1) = 0.84785...
        var result = Consistency.Compute(MixedReturns);

        result.MagnitudeConsistency.Should().BeApproximately(0.8479, 0.001);
    }

    [Test]
    public void Compute_all_positive_returns_ConsistencyPercent_is_100()
    {
        double[] returns = [0.1, 0.2, 0.3];

        var result = Consistency.Compute(returns);

        result.ConsistencyPercent.Should().BeApproximately(100.0, 0.001);
    }

    [Test]
    public void Compute_all_positive_returns_MagnitudeConsistency()
    {
        // downsideDeviation = 1.0 when no negative folds (matches CryptoBot source)
        // sortinoLike = 0.2 / 1.0 = 0.2
        // magnitudeConsistency = clamp((0.2 + 2) / 4, 0, 1) = 0.55
        double[] returns = [0.1, 0.2, 0.3];

        var result = Consistency.Compute(returns);

        result.MagnitudeConsistency.Should().BeApproximately(0.55, 0.001);
    }

    [Test]
    public void Compute_all_negative_returns_ConsistencyPercent_is_0()
    {
        double[] returns = [-0.1, -0.2, -0.3];

        var result = Consistency.Compute(returns);

        result.ConsistencyPercent.Should().BeApproximately(0.0, 0.001);
    }

    [Test]
    public void Compute_mirrors_CryptoBot_algorithm()
    {
        // Reference test using the CryptoBot AnchoredWalkForwardValidator.ComputeAggregateMetrics
        // algorithm adapted from decimal to double
        double[] returns = [0.10, -0.05, 0.15, 0.03, -0.08, 0.07, -0.02, 0.12];

        var result = Consistency.Compute(returns);

        // 5 positive out of 8 = 62.5%
        result.ConsistencyPercent.Should().BeApproximately(62.5, 0.001);

        // average = (0.10 - 0.05 + 0.15 + 0.03 - 0.08 + 0.07 - 0.02 + 0.12) / 8 = 0.32/8 = 0.04
        result.AverageReturn.Should().BeApproximately(0.04, 0.0001);

        // worst = -0.08
        result.WorstFold.Should().BeApproximately(-0.08, 0.0001);

        // magnitudeConsistency should be between 0 and 1
        result.MagnitudeConsistency.Should().BeInRange(0.0, 1.0);
    }
}
