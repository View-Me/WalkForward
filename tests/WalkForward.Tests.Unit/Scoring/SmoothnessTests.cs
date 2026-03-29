using FluentAssertions;
using NUnit.Framework;
using WalkForward.Scoring;

namespace WalkForward.Tests.Unit.Scoring;

[TestFixture]
public class SmoothnessTests
{
    [Test]
    public void Compute_centerCell_in3x3Grid_returns0_75()
    {
        // Center cell (1,1) with fitness 2.0, all neighbors have fitness 1.5
        var cells = Build3x3Grid(1.5);
        cells[4] = (1, 1, 2.0); // override center cell

        var result = Smoothness.Compute(1, 1, 2.0, cells);

        // neighborAvg = 1.5, Clamp(1.5 / Max(1.0, 2.0), 0, 1) = 0.75
        result.Should().BeApproximately(0.75, 0.0001);
    }

    [Test]
    public void Compute_singleCell_in1x1Grid_returns0()
    {
        var cells = new List<(int TrainIndex, int TestIndex, double Fitness)>
        {
            (0, 0, 2.0),
        };

        var result = Smoothness.Compute(0, 0, 2.0, cells);

        result.Should().Be(0.0);
    }

    [Test]
    public void Compute_cornerCell_in3x3Grid_uses3Neighbors()
    {
        // Corner cell (0,0) in 3x3 grid: neighbors are (0,1), (1,0), (1,1)
        var cells = Build3x3Grid(1.5);
        cells[0] = (0, 0, 2.0); // override corner cell

        var result = Smoothness.Compute(0, 0, 2.0, cells);

        // 3 neighbors all with fitness 1.5
        // neighborAvg = 1.5, Clamp(1.5 / Max(1.0, 2.0), 0, 1) = 0.75
        result.Should().BeApproximately(0.75, 0.0001);
    }

    [Test]
    public void Compute_edgeCell_in3x3Grid_uses5Neighbors()
    {
        // Edge cell (0,1) in 3x3 grid: neighbors are (0,0), (0,2), (1,0), (1,1), (1,2)
        var cells = Build3x3Grid(1.5);
        cells[1] = (0, 1, 2.0); // override edge cell

        var result = Smoothness.Compute(0, 1, 2.0, cells);

        // 5 neighbors all with fitness 1.5
        // neighborAvg = 1.5, Clamp(1.5 / Max(1.0, 2.0), 0, 1) = 0.75
        result.Should().BeApproximately(0.75, 0.0001);
    }

    [Test]
    public void Compute_zeroFitness_noDivisionError()
    {
        // Cell fitness is 0.0: Math.Max(1.0, Math.Abs(0.0)) = 1.0
        var cells = Build3x3Grid(1.5);
        cells[4] = (1, 1, 0.0); // override center cell

        var result = Smoothness.Compute(1, 1, 0.0, cells);

        // neighborAvg = 1.5, Clamp(1.5 / Max(1.0, 0.0), 0, 1) = Clamp(1.5, 0, 1) = 1.0
        result.Should().BeApproximately(1.0, 0.0001);
        double.IsNaN(result).Should().BeFalse();
        double.IsInfinity(result).Should().BeFalse();
    }

    [Test]
    public void Compute_allZeroFitness_returns0()
    {
        var cells = Build3x3Grid(0.0);

        var result = Smoothness.Compute(1, 1, 0.0, cells);

        // neighborAvg = 0.0, Clamp(0.0 / Max(1.0, 0.0), 0, 1) = 0.0
        result.Should().Be(0.0);
    }

    [Test]
    public void Compute_negativeFitness_usesAbsoluteValue()
    {
        // Negative center fitness: Math.Abs(-2.0) = 2.0
        var cells = Build3x3Grid(1.5);
        cells[4] = (1, 1, -2.0); // override center cell

        var result = Smoothness.Compute(1, 1, -2.0, cells);

        // neighborAvg = 1.5, Clamp(1.5 / Max(1.0, Abs(-2.0)), 0, 1) = Clamp(0.75, 0, 1) = 0.75
        result.Should().BeApproximately(0.75, 0.0001);
    }

    [Test]
    public void Compute_emptyCellsList_returns0()
    {
        var cells = new List<(int TrainIndex, int TestIndex, double Fitness)>();

        var result = Smoothness.Compute(0, 0, 1.0, cells);

        result.Should().Be(0.0);
    }

    private static List<(int TrainIndex, int TestIndex, double Fitness)> Build3x3Grid(double defaultFitness)
    {
        var cells = new List<(int, int, double)>();
        for (var t = 0; t < 3; t++)
        {
            for (var s = 0; s < 3; s++)
            {
                cells.Add((t, s, defaultFitness));
            }
        }

        return cells;
    }
}
