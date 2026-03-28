using System.Reflection;
using FluentAssertions;
using NUnit.Framework;

namespace WalkForward.Tests.Architecture;

[TestFixture]
public class DependencyTests
{
    private static readonly Assembly WalkForwardAssembly = typeof(Fold).Assembly;

    [Test]
    public void LibraryHasNoExternalDependencies()
    {
        var refs = WalkForwardAssembly.GetReferencedAssemblies();

        var externalRefs = refs.Where(r =>
            !r.Name!.StartsWith("System", StringComparison.Ordinal) &&
            !r.Name.StartsWith("netstandard", StringComparison.Ordinal) &&
            !r.Name.Equals("WalkForward", StringComparison.Ordinal));

        externalRefs.Should().BeEmpty(
            "library must have zero external dependencies (API-01)");
    }

    [Test]
    public void NoPublicTypeReferencesExternalNamespace()
    {
        var publicTypes = WalkForwardAssembly.GetExportedTypes();

        foreach (var type in publicTypes)
        {
            AssertTypeReferencesAreInternal(type);
        }
    }

    private static void AssertTypeReferencesAreInternal(Type type)
    {
        var publicMethods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .Where(m => !m.IsSpecialName);

        foreach (var method in publicMethods)
        {
            AssertNamespaceIsAllowed(method.ReturnType, $"{type.Name}.{method.Name} return type");

            foreach (var param in method.GetParameters())
            {
                AssertNamespaceIsAllowed(param.ParameterType, $"{type.Name}.{method.Name} parameter '{param.Name}'");
            }
        }

        var publicProperties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

        foreach (var prop in publicProperties)
        {
            AssertNamespaceIsAllowed(prop.PropertyType, $"{type.Name}.{prop.Name}");
        }
    }

    private static void AssertNamespaceIsAllowed(Type referencedType, string context)
    {
        var actualType = Nullable.GetUnderlyingType(referencedType) ?? referencedType;

        if (actualType.IsByRef)
        {
            actualType = actualType.GetElementType()!;
        }

        if (actualType.IsGenericType)
        {
            foreach (var arg in actualType.GetGenericArguments())
            {
                AssertNamespaceIsAllowed(arg, context);
            }
        }

        var ns = actualType.Namespace ?? string.Empty;

        var isAllowed = ns.StartsWith("System", StringComparison.Ordinal) ||
                        ns.StartsWith("WalkForward", StringComparison.Ordinal) ||
                        string.IsNullOrEmpty(ns);

        isAllowed.Should().BeTrue(
            $"{context} references type '{actualType.FullName}' from namespace '{ns}' which is outside allowed namespaces (System.*, WalkForward)");
    }
}
