using System.Reflection;
using StoryCoffee.Contracts;
using StoryCoffee.Infrastructure.Data;
using StoryCoffee.Domain;

namespace StoryCoffee.Tests;

public sealed class ArchitectureTests
{
    [Theory]
    [MemberData(nameof(ProjectReferences))]
    public void Projects_DoNotReferenceForbiddenLayers(Assembly assembly, string[] forbiddenAssemblyNames)
    {
        var referencedAssemblies = assembly
            .GetReferencedAssemblies()
            .Select(reference => reference.Name)
            .ToHashSet(StringComparer.Ordinal);

        foreach (var forbiddenAssemblyName in forbiddenAssemblyNames)
        {
            Assert.DoesNotContain(forbiddenAssemblyName, referencedAssemblies);
        }
    }

    [Theory]
    [MemberData(nameof(ExpectedNamespaceRoots))]
    public void Projects_UseExpectedNamespaceRoots(Assembly assembly, string expectedNamespaceRoot)
    {
        var invalidTypes = assembly
            .GetTypes()
            .Where(type => type.Namespace is not null && !type.Namespace.StartsWith(expectedNamespaceRoot, StringComparison.Ordinal))
            .Select(type => type.FullName)
            .ToArray();

        Assert.Empty(invalidTypes);
    }

    public static TheoryData<Assembly, string[]> ProjectReferences()
    {
        return new TheoryData<Assembly, string[]>
        {
            { typeof(Customer).Assembly, ["StoryCoffee.Application", "StoryCoffee.Infrastructure", "StoryCoffee.Api", "Microsoft.EntityFrameworkCore"] },
            { typeof(LoginRequest).Assembly, ["StoryCoffee.Application", "StoryCoffee.Infrastructure", "StoryCoffee.Api", "Microsoft.EntityFrameworkCore"] },
            { typeof(ProductCatalogUseCase).Assembly, ["StoryCoffee.Infrastructure", "StoryCoffee.Api", "Microsoft.EntityFrameworkCore", "Npgsql.EntityFrameworkCore.PostgreSQL"] },
            { typeof(AppDbContext).Assembly, ["StoryCoffee.Api"] },
            { typeof(Program).Assembly, [] }
        };
    }

    public static TheoryData<Assembly, string> ExpectedNamespaceRoots()
    {
        return new TheoryData<Assembly, string>
        {
            { typeof(Customer).Assembly, "StoryCoffee.Domain" },
            { typeof(LoginRequest).Assembly, "StoryCoffee.Contracts" },
            { typeof(ProductCatalogUseCase).Assembly, "StoryCoffee.Application" },
            { typeof(AppDbContext).Assembly, "StoryCoffee.Infrastructure" },
            { typeof(Program).Assembly, "StoryCoffee.Api" }
        };
    }
}
