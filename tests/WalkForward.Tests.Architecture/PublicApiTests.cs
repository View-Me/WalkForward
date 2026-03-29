using System.Reflection;
using FluentAssertions;
using NUnit.Framework;

namespace WalkForward.Tests.Architecture;

[TestFixture]
public class PublicApiTests
{
    private static readonly Assembly WalkForwardAssembly = typeof(Fold).Assembly;

    [Test]
    public void PublicApiSurfaceIsExact()
    {
        var expected = new HashSet<string>(StringComparer.Ordinal)
        {
            "FoldBuilder",
            "BackwardLookingBuilder",
            "ForwardLookingBuilder",
            "BackwardLookingOptions",
            "ForwardLookingOptions",
            "Fold",
            "FoldMode",
            "Consistency",
            "ConsistencyMetrics",
            "ClassifierConsistencyMetrics",
            "GridCellResult",
            "GridSearchBuilder",
            "GridSearchResult",
            "DegradationResult",
            "DegradationFoldResult",
            "LabeledFold",
            "ScoringWeights",
            "Smoothness",
            "CompositeScorer",
        };

        var actual = WalkForwardAssembly.GetExportedTypes()
            .Select(t => t.Name)
            .ToHashSet(StringComparer.Ordinal);

        actual.Should().BeEquivalentTo(
            expected,
            "the public API surface must not accidentally expose internal types");
    }

    [Test]
    public void InternalTypesAreNotExposed()
    {
        var internalTypeNames = new[]
        {
            "BackwardLookingFoldGenerator",
            "ForwardLookingFoldGenerator",
            "GridSearchEngine",
            "Validation",
        };

        var exportedNames = WalkForwardAssembly.GetExportedTypes()
            .Select(t => t.Name)
            .ToHashSet(StringComparer.Ordinal);

        foreach (var name in internalTypeNames)
        {
            exportedNames.Should().NotContain(
                name,
                $"'{name}' is an internal implementation detail and must not be public");
        }
    }

    [Test]
    public void AllPublicRecordsAreSealed()
    {
        var publicTypes = WalkForwardAssembly.GetExportedTypes();

        foreach (var type in publicTypes)
        {
            if (!IsRecord(type))
            {
                continue;
            }

            type.IsSealed.Should().BeTrue(
                $"public record '{type.Name}' should be sealed to prevent inheritance");
        }
    }

    [Test]
    public void AllPublicTypesHaveXmlDocumentation()
    {
        var xmlDocPath = Path.Combine(
            Path.GetDirectoryName(WalkForwardAssembly.Location)!,
            "WalkForward.xml");

        File.Exists(xmlDocPath).Should().BeTrue(
            $"XML documentation file should exist at '{xmlDocPath}'");

        var xmlContent = File.ReadAllText(xmlDocPath);

        var publicTypes = WalkForwardAssembly.GetExportedTypes();

        foreach (var type in publicTypes)
        {
            var memberName = type.IsNested
                ? $"T:{type.FullName!.Replace('+', '.')}"
                : $"T:{type.FullName}";

            xmlContent.Should().Contain(
                memberName,
                $"public type '{type.Name}' must have XML documentation");
        }
    }

    private static bool IsRecord(Type type)
    {
        return type.GetMethod("<Clone>$", BindingFlags.Public | BindingFlags.Instance) is not null;
    }
}
