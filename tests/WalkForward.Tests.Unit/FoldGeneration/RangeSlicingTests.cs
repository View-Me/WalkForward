using FluentAssertions;
using NUnit.Framework;
using WalkForward.Internal;

namespace WalkForward.Tests.Unit.FoldGeneration;

[TestFixture]
public class RangeSlicingTests
{
    private static readonly TimeSpan FifteenMinutes = TimeSpan.FromMinutes(15);

    [Test]
    public void TrainRange_SlicesArrayCorrectly()
    {
        var data = Enumerable.Range(0, 1000).ToArray();

        var options = new ForwardLookingOptions
        {
            TotalDataPoints = 1000,
            DataFrequency = TimeSpan.FromHours(1),
            TrainingWindow = TimeSpan.FromDays(10),
            TestWindow = TimeSpan.FromDays(3),
        };

        var folds = ForwardLookingFoldGenerator.Generate(options);
        folds.Should().HaveCountGreaterThan(0);

        var firstFold = folds[0];
        var trainSlice = data[firstFold.TrainRange];

        trainSlice.Should().HaveCount(firstFold.TrainLength);
        trainSlice[0].Should().Be(firstFold.TrainStart);
        trainSlice[^1].Should().Be(firstFold.TrainEnd - 1);
    }

    [Test]
    public void TestRange_SlicesArrayCorrectly()
    {
        var data = Enumerable.Range(0, 1000).ToArray();

        var options = new ForwardLookingOptions
        {
            TotalDataPoints = 1000,
            DataFrequency = TimeSpan.FromHours(1),
            TrainingWindow = TimeSpan.FromDays(10),
            TestWindow = TimeSpan.FromDays(3),
        };

        var folds = ForwardLookingFoldGenerator.Generate(options);
        folds.Should().HaveCountGreaterThan(0);

        var firstFold = folds[0];
        var testSlice = data[firstFold.TestRange];

        testSlice.Should().HaveCount(firstFold.TestLength);
        testSlice[0].Should().Be(firstFold.TestStart);
        testSlice[^1].Should().Be(firstFold.TestEnd - 1);
    }

    [Test]
    public void EmbargoRange_ContainsCorrectIndices()
    {
        var data = Enumerable.Range(0, 10000).ToArray();

        var options = new ForwardLookingOptions
        {
            TotalDataPoints = 10000,
            DataFrequency = FifteenMinutes,
            TrainingWindow = TimeSpan.FromDays(30),
            TestWindow = TimeSpan.FromDays(7),
            Embargo = TimeSpan.FromHours(4),
        };

        var folds = ForwardLookingFoldGenerator.Generate(options);
        folds.Should().HaveCountGreaterThan(0);

        var firstFold = folds[0];
        var embargoSlice = data[firstFold.EmbargoRange];

        embargoSlice.Should().HaveCount(16);
        embargoSlice[0].Should().Be(firstFold.EmbargoStart);
        embargoSlice[^1].Should().Be(firstFold.EmbargoEnd - 1);
    }
}
