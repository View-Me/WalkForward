using System.Reflection;
using FluentAssertions;
using NUnit.Framework;

namespace WalkForward.Tests.Unit.Builder;

[TestFixture]
public class ForwardLookingBuilderTests
{
    [Test]
    public void Full_rolling_chain_returns_folds()
    {
        var folds = new FoldBuilder()
            .WithDataPoints(10000)
            .WithDataFrequency(TimeSpan.FromMinutes(15))
            .ForwardLooking()
            .WithTrainingWindow(TimeSpan.FromDays(30))
            .WithTestWindow(TimeSpan.FromDays(7))
            .WithStride(TimeSpan.FromDays(3))
            .Build();

        folds.Should().NotBeEmpty();
        folds.Should().AllSatisfy(f =>
        {
            f.TrainStart.Should().BeGreaterThanOrEqualTo(0);
            f.TestEnd.Should().BeLessThanOrEqualTo(10000);
        });
    }

    [Test]
    public void ForwardLookingBuilder_does_expose_WithStride()
    {
        var methods = typeof(ForwardLookingBuilder).GetMethods(BindingFlags.Public | BindingFlags.Instance);

        methods.Should().Contain(
            m => m.Name == "WithStride",
            "forward-looking mode should expose stride configuration");
    }

    [Test]
    public void ForwardLooking_without_WithStride_defaults_stride_to_test_window_size()
    {
        var folds = new FoldBuilder()
            .WithDataPoints(10000)
            .WithDataFrequency(TimeSpan.FromMinutes(15))
            .ForwardLooking()
            .WithTrainingWindow(TimeSpan.FromDays(30))
            .WithTestWindow(TimeSpan.FromDays(7))
            .Build();

        folds.Should().NotBeEmpty();

        // When stride equals test window, consecutive folds' test windows should be adjacent
        if (folds.Count >= 2)
        {
            folds[1].TestStart.Should().Be(
                folds[0].TestEnd,
                "without explicit stride, stride defaults to test window so test windows are adjacent");
        }
    }

    [Test]
    public void WithEmbargo_creates_gap_between_train_and_test()
    {
        var folds = new FoldBuilder()
            .WithDataPoints(10000)
            .WithDataFrequency(TimeSpan.FromMinutes(15))
            .ForwardLooking()
            .WithTrainingWindow(TimeSpan.FromDays(30))
            .WithTestWindow(TimeSpan.FromDays(7))
            .WithEmbargo(TimeSpan.FromHours(4))
            .Build();

        folds.Should().NotBeEmpty();
        folds.Should().AllSatisfy(f =>
        {
            f.EmbargoLength.Should().BeGreaterThan(0, "embargo was configured to 4 hours");
            f.EmbargoStart.Should().Be(f.TrainEnd, "embargo starts where training ends");
            f.TestStart.Should().Be(f.EmbargoEnd, "test starts where embargo ends");
        });
    }
}
