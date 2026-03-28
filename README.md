# WalkForward.Net

<!-- NuGet badge placeholder: [![NuGet](https://img.shields.io/nuget/v/WalkForward.Net)](https://www.nuget.org/packages/WalkForward.Net) -->
[![License](https://img.shields.io/badge/license-Apache%202.0-blue.svg)](LICENSE)

Time-series cross-validation without lookahead bias for .NET.

## The Problem

Random k-fold cross-validation is **broken for time series**. It shuffles data, so your "test" fold may contain Tuesday's data while "training" on Wednesday's. The model learns the future to predict the past -- results look great, production performance is terrible. This is called **lookahead bias**.

WalkForward.Net generates temporally-correct train/test splits that respect the arrow of time, with configurable embargo gaps to prevent subtle information leakage.

## Installation

```bash
dotnet add package WalkForward.Net
```

Targets **.NET 8.0** and **.NET 9.0**. Zero runtime dependencies.

## Quick Start

### Anchored Mode (expanding window)

```csharp
using WalkForward;

// Generate folds from 10,000 data points at 15-minute frequency
var folds = new WalkForwardBuilder()
    .WithDataPoints(10_000)
    .WithDataFrequency(TimeSpan.FromMinutes(15))
    .Anchored()
    .WithTrainingWindow(TimeSpan.FromDays(90))
    .WithTestWindow(TimeSpan.FromDays(7))
    .WithEmbargo(TimeSpan.FromHours(4))
    .Build();

// Use folds with your own model/strategy
var returns = new double[folds.Count];
for (var i = 0; i < folds.Count; i++)
{
    var trainData = allData[folds[i].TrainRange];
    var testData  = allData[folds[i].TestRange];

    returns[i] = MyModel.Train(trainData).Evaluate(testData);
}

// Compute aggregate consistency metrics
var metrics = Consistency.Compute(returns);
// metrics.ConsistencyPercent   -> 60.0  (3 of 5 folds profitable)
// metrics.MagnitudeConsistency -> 0.72  (Sortino-like, [0,1])
// metrics.WorstFold            -> -0.02
// metrics.AverageReturn        -> 0.022
```

### Rolling Mode (fixed window)

```csharp
var folds = new WalkForwardBuilder()
    .WithDataPoints(50_000)
    .WithDataFrequency(TimeSpan.FromMinutes(15))
    .Rolling()
    .WithTrainingWindow(TimeSpan.FromDays(30))
    .WithTestWindow(TimeSpan.FromDays(7))
    .WithStride(TimeSpan.FromDays(3))
    .WithEmbargo(TimeSpan.FromHours(4))
    .Build();
```

## API Reference

| Type | Description |
|------|-------------|
| `WalkForwardBuilder` | Entry point. Configure data points and frequency, then select a mode. |
| `AnchoredBuilder` | Anchored mode configuration: training/test windows, embargo, warmup, max folds. |
| `RollingBuilder` | Rolling mode configuration: adds stride to anchored options. |
| `Fold` | Single fold with int boundaries and `Range` properties for array slicing. |
| `AnchoredOptions` | Anchored configuration record (used internally by `AnchoredBuilder`). |
| `RollingOptions` | Rolling configuration record (used internally by `RollingBuilder`). |
| `WalkForwardMode` | Enum: `Anchored`, `Rolling`. |
| `Consistency` | Static methods: `Compute()` for returns, `ForClassifier()` for ML accuracy. |
| `ConsistencyMetrics` | Returns-based metrics: consistency %, magnitude consistency, worst fold, average return. |
| `ClassifierConsistencyMetrics` | Classifier metrics: average accuracy, log-loss, consistency above baseline. |

## Modes

### Anchored (expanding window)

Folds walk backwards from the end of data. The training window is fixed-size per fold.

```
Data: [========================================]

Fold 1:            [=====TRAIN=====][//][==TEST==]
Fold 2:     [=====TRAIN=====][//][==TEST==]
Fold 3: [=====TRAIN=====][//][==TEST==]
                          ^^
                        embargo
```

### Rolling (fixed window)

Both windows slide forward through the data with a configurable stride.

```
Data: [========================================]

Fold 1: [=====TRAIN=====][//][==TEST==]
Fold 2:       [=====TRAIN=====][//][==TEST==]
Fold 3:             [=====TRAIN=====][//][==TEST==]
                               ^^
                             embargo
```

## Embargo

The embargo gap between training and test windows prevents information leakage from autocorrelated features. If your model predicts 4 hours ahead, the last training sample's label was computed using data that overlaps with the test window. The embargo excludes this zone.

```
Without embargo:  [====TRAIN====][==TEST==]
                              ^-- label leakage

With embargo:     [====TRAIN====][///GAP///][==TEST==]
                                 ^-- excluded zone
```

Embargo is specified as a `TimeSpan` and automatically converted to the correct number of data points based on your data frequency.

## Classifier Consistency

For ML classification models, use `Consistency.ForClassifier()` with per-fold accuracy and log-loss arrays:

```csharp
var metrics = Consistency.ForClassifier(
    foldAccuracies: new[] { 0.61, 0.58, 0.63, 0.60, 0.55 },
    foldLogLosses: new[] { 0.65, 0.68, 0.62, 0.64, 0.70 },
    baselineAccuracy: 0.50);

// metrics.AverageAccuracy          -> 0.594
// metrics.AverageLogLoss           -> 0.658
// metrics.ConsistencyAboveBaseline -> 100.0  (all folds above 0.50)
// metrics.FoldCount                -> 5
```

## Requirements

- .NET 8.0+ or .NET 9.0+
- Zero runtime dependencies
- AOT and trimming compatible

## License

Apache 2.0
