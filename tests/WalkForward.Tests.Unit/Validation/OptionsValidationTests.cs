using FluentAssertions;
using NUnit.Framework;

namespace WalkForward.Tests.Unit.Validation;

[TestFixture]
public class OptionsValidationTests
{
    private static readonly TimeSpan FifteenMinutes = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan NinetyDays = TimeSpan.FromDays(90);
    private static readonly TimeSpan ThirtyDays = TimeSpan.FromDays(30);
    private static readonly TimeSpan SevenDays = TimeSpan.FromDays(7);

    [Test]
    public void BackwardLookingOptions_WithValidParameters_CreatesWithoutThrowing()
    {
        var options = new BackwardLookingOptions
        {
            TotalDataPoints = 10000,
            DataFrequency = FifteenMinutes,
            TrainingWindow = NinetyDays,
            TestWindow = SevenDays,
        };

        var act = () => options.Validate();

        act.Should().NotThrow();
    }

    [Test]
    public void BackwardLookingOptions_WithZeroTotalDataPoints_ThrowsArgumentOutOfRangeException()
    {
        var options = new BackwardLookingOptions
        {
            TotalDataPoints = 0,
            DataFrequency = FifteenMinutes,
            TrainingWindow = NinetyDays,
            TestWindow = SevenDays,
        };

        var act = () => options.Validate();

        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName(nameof(BackwardLookingOptions.TotalDataPoints));
    }

    [Test]
    public void BackwardLookingOptions_WithNegativeTotalDataPoints_ThrowsArgumentOutOfRangeException()
    {
        var options = new BackwardLookingOptions
        {
            TotalDataPoints = -1,
            DataFrequency = FifteenMinutes,
            TrainingWindow = NinetyDays,
            TestWindow = SevenDays,
        };

        var act = () => options.Validate();

        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName(nameof(BackwardLookingOptions.TotalDataPoints));
    }

    [Test]
    public void BackwardLookingOptions_WithZeroTrainingWindow_ThrowsArgumentOutOfRangeException()
    {
        var options = new BackwardLookingOptions
        {
            TotalDataPoints = 10000,
            DataFrequency = FifteenMinutes,
            TrainingWindow = TimeSpan.Zero,
            TestWindow = SevenDays,
        };

        var act = () => options.Validate();

        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName(nameof(BackwardLookingOptions.TrainingWindow));
    }

    [Test]
    public void BackwardLookingOptions_WithZeroTestWindow_ThrowsArgumentOutOfRangeException()
    {
        var options = new BackwardLookingOptions
        {
            TotalDataPoints = 10000,
            DataFrequency = FifteenMinutes,
            TrainingWindow = NinetyDays,
            TestWindow = TimeSpan.Zero,
        };

        var act = () => options.Validate();

        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName(nameof(BackwardLookingOptions.TestWindow));
    }

    [Test]
    public void BackwardLookingOptions_WithZeroDataFrequency_ThrowsArgumentOutOfRangeException()
    {
        var options = new BackwardLookingOptions
        {
            TotalDataPoints = 10000,
            DataFrequency = TimeSpan.Zero,
            TrainingWindow = NinetyDays,
            TestWindow = SevenDays,
        };

        var act = () => options.Validate();

        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName(nameof(BackwardLookingOptions.DataFrequency));
    }

    [Test]
    public void BackwardLookingOptions_WithNegativeEmbargo_ThrowsArgumentOutOfRangeException()
    {
        var options = new BackwardLookingOptions
        {
            TotalDataPoints = 10000,
            DataFrequency = FifteenMinutes,
            TrainingWindow = NinetyDays,
            TestWindow = SevenDays,
            Embargo = TimeSpan.FromHours(-1),
        };

        var act = () => options.Validate();

        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName(nameof(BackwardLookingOptions.Embargo));
    }

    [Test]
    public void BackwardLookingOptions_WithNegativeWarmupPoints_ThrowsArgumentOutOfRangeException()
    {
        var options = new BackwardLookingOptions
        {
            TotalDataPoints = 10000,
            DataFrequency = FifteenMinutes,
            TrainingWindow = NinetyDays,
            TestWindow = SevenDays,
            WarmupPoints = -1,
        };

        var act = () => options.Validate();

        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName(nameof(BackwardLookingOptions.WarmupPoints));
    }

    [Test]
    public void ForwardLookingOptions_WithValidParameters_CreatesWithoutThrowing()
    {
        var options = new ForwardLookingOptions
        {
            TotalDataPoints = 10000,
            DataFrequency = FifteenMinutes,
            TrainingWindow = ThirtyDays,
            TestWindow = SevenDays,
        };

        var act = () => options.Validate();

        act.Should().NotThrow();
    }

    [Test]
    public void ForwardLookingOptions_WithZeroStride_ThrowsArgumentOutOfRangeException()
    {
        var options = new ForwardLookingOptions
        {
            TotalDataPoints = 10000,
            DataFrequency = FifteenMinutes,
            TrainingWindow = ThirtyDays,
            TestWindow = SevenDays,
            Stride = TimeSpan.Zero,
        };

        var act = () => options.Validate();

        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName(nameof(ForwardLookingOptions.Stride));
    }

    [Test]
    public void ForwardLookingOptions_WithNegativeEmbargo_ThrowsArgumentOutOfRangeException()
    {
        var options = new ForwardLookingOptions
        {
            TotalDataPoints = 10000,
            DataFrequency = FifteenMinutes,
            TrainingWindow = ThirtyDays,
            TestWindow = SevenDays,
            Embargo = TimeSpan.FromHours(-1),
        };

        var act = () => options.Validate();

        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName(nameof(ForwardLookingOptions.Embargo));
    }

    [Test]
    public void Fold_TrainRange_ReturnsTrainStartToTrainEnd()
    {
        var fold = new Fold
        {
            FoldIndex = 0,
            TrainStart = 100,
            TrainEnd = 500,
            TestStart = 520,
            TestEnd = 1000,
            EmbargoStart = 500,
            EmbargoEnd = 520,
        };

        fold.TrainRange.Should().Be(new Range(100, 500));
    }

    [Test]
    public void Fold_TestRange_ReturnsTestStartToTestEnd()
    {
        var fold = new Fold
        {
            FoldIndex = 0,
            TrainStart = 100,
            TrainEnd = 500,
            TestStart = 520,
            TestEnd = 1000,
            EmbargoStart = 500,
            EmbargoEnd = 520,
        };

        fold.TestRange.Should().Be(new Range(520, 1000));
    }

    [Test]
    public void Fold_TrainLength_ReturnsTrainEndMinusTrainStart()
    {
        var fold = new Fold
        {
            FoldIndex = 0,
            TrainStart = 100,
            TrainEnd = 500,
            TestStart = 520,
            TestEnd = 1000,
            EmbargoStart = 500,
            EmbargoEnd = 520,
        };

        fold.TrainLength.Should().Be(400);
    }

    [Test]
    public void ToIndexCount_ConvertsTimeSpanToIndicesUsingFloor()
    {
        // 90 days at 15min freq = 90 * 24 * 4 = 8640
        var result = Internal.Validation.ToIndexCount(NinetyDays, FifteenMinutes);

        result.Should().Be(8640);
    }

    [Test]
    public void ToEmbargoIndexCount_RoundsUpFractionalEmbargo()
    {
        // 3h50m at 15min freq = 230/15 = 15.333... -> ceil = 16
        var result = Internal.Validation.ToEmbargoIndexCount(
            TimeSpan.FromMinutes(230),
            FifteenMinutes);

        result.Should().Be(16);
    }

    [Test]
    public void ToEmbargoIndexCount_ExactEmbargo_DoesNotAddExtra()
    {
        // 4h at 15min freq = 240/15 = 16.0 -> ceil = 16
        var result = Internal.Validation.ToEmbargoIndexCount(
            TimeSpan.FromHours(4),
            FifteenMinutes);

        result.Should().Be(16);
    }
}
