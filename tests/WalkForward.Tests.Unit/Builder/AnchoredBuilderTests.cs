using System.Reflection;
using FluentAssertions;
using NUnit.Framework;

namespace WalkForward.Tests.Unit.Builder;

[TestFixture]
public class AnchoredBuilderTests
{
    [Test]
    public void Full_anchored_chain_returns_non_empty_folds()
    {
        var folds = new WalkForwardBuilder()
            .WithDataPoints(10000)
            .WithDataFrequency(TimeSpan.FromMinutes(15))
            .Anchored()
            .WithTrainingWindow(TimeSpan.FromDays(90))
            .WithTestWindow(TimeSpan.FromDays(7))
            .WithEmbargo(TimeSpan.FromHours(4))
            .WithWarmup(500)
            .Build();

        folds.Should().NotBeEmpty();
        folds.Should().AllSatisfy(f =>
        {
            f.TrainStart.Should().BeGreaterThanOrEqualTo(0);
            f.TestEnd.Should().BeLessThanOrEqualTo(10000);
        });
    }

    [Test]
    public void AnchoredBuilder_does_not_expose_WithStride()
    {
        var methods = typeof(AnchoredBuilder).GetMethods(BindingFlags.Public | BindingFlags.Instance);

        methods.Should().NotContain(
            m => m.Name == "WithStride",
            "anchored mode should not expose stride -- it steps by test window size");
    }

    [Test]
    public void WithMaxFolds_limits_returned_folds_to_at_most_3()
    {
        var folds = new WalkForwardBuilder()
            .WithDataPoints(100000)
            .WithDataFrequency(TimeSpan.FromMinutes(15))
            .Anchored()
            .WithTrainingWindow(TimeSpan.FromDays(30))
            .WithTestWindow(TimeSpan.FromDays(7))
            .WithMaxFolds(3)
            .Build();

        folds.Should().HaveCountLessThanOrEqualTo(3);
    }

    [Test]
    public void Without_WithMaxFolds_auto_computes_fold_count()
    {
        var folds = new WalkForwardBuilder()
            .WithDataPoints(100000)
            .WithDataFrequency(TimeSpan.FromMinutes(15))
            .Anchored()
            .WithTrainingWindow(TimeSpan.FromDays(30))
            .WithTestWindow(TimeSpan.FromDays(7))
            .Build();

        folds.Should().HaveCountGreaterThan(
            3,
            "with 100000 points at 15-min frequency, auto-compute should produce more than 3 folds");
    }
}
