using FluentAssertions;
using NUnit.Framework;

namespace WalkForward.Tests.Unit.Metrics;

[TestFixture]
public class ClassifierConsistencyTests
{
    [Test]
    public void ForClassifier_AverageAccuracy_is_correct()
    {
        double[] accuracies = [0.61, 0.58, 0.63, 0.60, 0.55];
        double[] logLosses = [0.5, 0.52, 0.48, 0.51, 0.54];

        var result = Consistency.ForClassifier(accuracies, logLosses, baselineAccuracy: 0.5);

        // mean = (0.61 + 0.58 + 0.63 + 0.60 + 0.55) / 5 = 2.97 / 5 = 0.594
        result.AverageAccuracy.Should().BeApproximately(0.594, 0.001);
    }

    [Test]
    public void ForClassifier_ConsistencyAboveBaseline_all_above()
    {
        double[] accuracies = [0.61, 0.58, 0.63, 0.60, 0.55];
        double[] logLosses = [0.5, 0.52, 0.48, 0.51, 0.54];

        var result = Consistency.ForClassifier(accuracies, logLosses, baselineAccuracy: 0.5);

        // All 5 are above 0.5, so 100%
        result.ConsistencyAboveBaseline.Should().BeApproximately(100.0, 0.001);
    }

    [Test]
    public void ForClassifier_ConsistencyAboveBaseline_partial()
    {
        double[] accuracies = [0.48, 0.52, 0.55];
        double[] logLosses = [0.6, 0.5, 0.48];

        var result = Consistency.ForClassifier(accuracies, logLosses, baselineAccuracy: 0.5);

        // 2 of 3 above 0.5 = 66.67%
        result.ConsistencyAboveBaseline.Should().BeApproximately(66.67, 0.01);
    }

    [Test]
    public void ForClassifier_AverageLogLoss_is_correctly_averaged()
    {
        double[] accuracies = [0.61, 0.58, 0.63];
        double[] logLosses = [0.50, 0.55, 0.45];

        var result = Consistency.ForClassifier(accuracies, logLosses, baselineAccuracy: 0.5);

        // mean log loss = (0.50 + 0.55 + 0.45) / 3 = 1.50 / 3 = 0.5
        result.AverageLogLoss.Should().BeApproximately(0.5, 0.001);
    }

    [Test]
    public void ForClassifier_FoldCount_matches_input_length()
    {
        double[] accuracies = [0.61, 0.58, 0.63, 0.60, 0.55];
        double[] logLosses = [0.5, 0.52, 0.48, 0.51, 0.54];

        var result = Consistency.ForClassifier(accuracies, logLosses, baselineAccuracy: 0.5);

        result.FoldCount.Should().Be(5);
    }

    [Test]
    public void ForClassifier_mismatched_lengths_throws_ArgumentException()
    {
        double[] accuracies = [0.61, 0.58];
        double[] logLosses = [0.5, 0.52, 0.48];

        var act = () => Consistency.ForClassifier(accuracies, logLosses, baselineAccuracy: 0.5);

        act.Should().Throw<ArgumentException>();
    }
}
