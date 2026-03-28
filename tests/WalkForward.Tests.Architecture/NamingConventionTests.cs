using System.Reflection;
using FluentAssertions;
using NUnit.Framework;

namespace WalkForward.Tests.Architecture;

[TestFixture]
public class NamingConventionTests
{
    private static readonly Assembly WalkForwardAssembly = typeof(Fold).Assembly;

    [Test]
    public void AllPublicTypesPascalCase()
    {
        var publicTypes = WalkForwardAssembly.GetExportedTypes();

        foreach (var type in publicTypes)
        {
            char.IsUpper(type.Name[0]).Should().BeTrue(
                $"public type '{type.Name}' should start with an uppercase letter (PascalCase)");
        }
    }

    [Test]
    public void AllPublicMethodsPascalCase()
    {
        var publicTypes = WalkForwardAssembly.GetExportedTypes();

        foreach (var type in publicTypes)
        {
            var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Where(m => !m.IsSpecialName)
                .Where(m => !m.Name.StartsWith('<'));

            foreach (var method in methods)
            {
                char.IsUpper(method.Name[0]).Should().BeTrue(
                    $"public method '{type.Name}.{method.Name}' should start with an uppercase letter (PascalCase)");
            }
        }
    }

    [Test]
    public void AllPublicPropertiesPascalCase()
    {
        var publicTypes = WalkForwardAssembly.GetExportedTypes();

        foreach (var type in publicTypes)
        {
            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

            foreach (var prop in properties)
            {
                char.IsUpper(prop.Name[0]).Should().BeTrue(
                    $"public property '{type.Name}.{prop.Name}' should start with an uppercase letter (PascalCase)");
            }
        }
    }

    [Test]
    public void EnumValuesArePascalCase()
    {
        var enumTypes = WalkForwardAssembly.GetExportedTypes()
            .Where(t => t.IsEnum);

        foreach (var enumType in enumTypes)
        {
            var values = Enum.GetNames(enumType);

            foreach (var value in values)
            {
                char.IsUpper(value[0]).Should().BeTrue(
                    $"enum value '{enumType.Name}.{value}' should start with an uppercase letter (PascalCase)");
            }
        }
    }

    [Test]
    public void NoPublicFields()
    {
        var publicTypes = WalkForwardAssembly.GetExportedTypes();

        foreach (var type in publicTypes)
        {
            if (type.IsEnum)
            {
                continue;
            }

            var publicFields = type.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

            publicFields.Should().BeEmpty(
                $"type '{type.Name}' should not have public fields (use properties instead)");
        }
    }
}
