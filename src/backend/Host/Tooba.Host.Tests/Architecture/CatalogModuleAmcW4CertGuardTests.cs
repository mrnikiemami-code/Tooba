using System.Text.Json;
using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>
/// TB-TMAR-CATALOG-AMC-001 W4 — structure gate + CERTIFIED SoT/manifest lock.
/// </summary>
public sealed class CatalogModuleAmcW4CertGuardTests
{
    [Fact]
    public void Catalog_is_manifest_structure_certified_under_modules_catalog()
    {
        var root = Repo();
        var slnx = File.ReadAllText(Path.Combine(root, "src/backend/Tooba.slnx"));
        Assert.Contains("/Modules/Catalog/", slnx, StringComparison.Ordinal);
        Assert.Contains("Tooba.Catalog.Endpoints", slnx, StringComparison.Ordinal);

        using var doc = JsonDocument.Parse(File.ReadAllText(Path.Combine(
            root, "docs/architecture/tmar-module-structure-manifests.json")));
        var modules = doc.RootElement.GetProperty("modules");
        var catalog = modules.EnumerateArray()
            .Single(m => m.GetProperty("module").GetString() == "Catalog");
        Assert.True(catalog.GetProperty("structureCertified").GetBoolean());
        Assert.Equal("ARCH-COMPLETE-002", catalog.GetProperty("lockVersion").GetString());

        var projectNames = catalog.GetProperty("projects").EnumerateArray()
            .Select(p => p.GetProperty("projectName").GetString())
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(
            new[]
            {
                "Tooba.Catalog.Application",
                "Tooba.Catalog.Contracts",
                "Tooba.Catalog.Domain",
                "Tooba.Catalog.Endpoints",
                "Tooba.Catalog.Infrastructure",
            },
            projectNames);
    }

    [Fact]
    public void Catalog_sot_marks_complete_reference_pattern_and_structure_ready()
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(Path.Combine(
            Repo(), "docs/architecture/tmar-current-state.json")));
        var block = doc.RootElement.GetProperty("catalogAmc001");
        Assert.Equal("COMPLETE_REFERENCE_PATTERN", block.GetProperty("state").GetString());
        Assert.Equal("READY_FOR_CERTIFY", block.GetProperty("structureState").GetString());
        Assert.True(block.GetProperty("structureCertifiedUnderArchComplete002").GetBoolean());
        Assert.True(block.GetProperty("manifestCertified").GetBoolean());
        Assert.True(block.GetProperty("microserviceExtractable").GetBoolean());
        Assert.Equal("ZERO", block.GetProperty("foreignAppInfraDomainCoupling").GetString());
        Assert.Equal("MODULE_ENDPOINTS", block.GetProperty("endpointOwnership").GetString());
        Assert.Equal(132, block.GetProperty("endpointReachableRequests").GetInt32());
        Assert.Equal(53, block.GetProperty("validatorRequiredCount").GetInt32());
        Assert.Equal(79, block.GetProperty("noValidatorRequiredCount").GetInt32());

        var certified = doc.RootElement.GetProperty("structureLock").GetProperty("certifiedModules")
            .EnumerateArray()
            .Select(x => x.GetString())
            .ToArray();
        Assert.Contains("Catalog", certified);
        Assert.Contains("Party", certified);
        Assert.Contains("UserPreference", certified);
    }

    [Fact]
    public void Catalog_roots_and_capability_folders_match_manifest_allowlists()
    {
        var root = Repo();
        var app = Path.Combine(root, "src/backend/Modules/Catalog/Tooba.Catalog.Application");
        var infra = Path.Combine(root, "src/backend/Modules/Catalog/Tooba.Catalog.Infrastructure");
        var endpoints = Path.Combine(root, "src/backend/Modules/Catalog/Tooba.Catalog.Endpoints");
        var domain = Path.Combine(root, "src/backend/Modules/Catalog/Tooba.Catalog.Domain");
        var contracts = Path.Combine(root, "src/backend/Modules/Catalog/Tooba.Catalog.Contracts");

        Assert.Equal(
            new[] { "GlobalUsings.cs" },
            Directory.EnumerateFiles(app, "*.cs", SearchOption.TopDirectoryOnly)
                .Select(Path.GetFileName)
                .OrderBy(x => x, StringComparer.Ordinal)
                .ToArray());
        Assert.Equal(
            new[] { "GlobalUsings.cs" },
            Directory.EnumerateFiles(domain, "*.cs", SearchOption.TopDirectoryOnly)
                .Select(Path.GetFileName)
                .OrderBy(x => x, StringComparer.Ordinal)
                .ToArray());
        Assert.Equal(
            new[] { "GlobalUsings.cs" },
            Directory.EnumerateFiles(contracts, "*.cs", SearchOption.TopDirectoryOnly)
                .Select(Path.GetFileName)
                .OrderBy(x => x, StringComparer.Ordinal)
                .ToArray());
        Assert.False(Directory.Exists(Path.Combine(app, "Commands")));
        Assert.False(Directory.Exists(Path.Combine(app, "Queries")));
        Assert.True(Directory.Exists(Path.Combine(app, "Categories")));
        Assert.True(Directory.Exists(Path.Combine(app, "Storefront")));
        Assert.True(Directory.Exists(Path.Combine(app, "Ports")));
        Assert.True(Directory.Exists(Path.Combine(app, "Models")));
        Assert.True(Directory.Exists(Path.Combine(domain, "Aggregates")));
        Assert.True(Directory.Exists(Path.Combine(domain, "Enums")));

        Assert.Equal(
            new[] { "CatalogModule.cs", "GlobalUsings.cs" },
            Directory.EnumerateFiles(infra, "*.cs", SearchOption.TopDirectoryOnly)
                .Select(Path.GetFileName)
                .OrderBy(x => x, StringComparer.Ordinal)
                .ToArray());
        Assert.False(Directory.Exists(Path.Combine(infra, "Migrations")));
        Assert.True(Directory.Exists(Path.Combine(infra, "Persistence", "Migrations")));
        Assert.True(Directory.Exists(Path.Combine(infra, "Directories")));
        Assert.True(File.Exists(Path.Combine(infra, "Directories", "CatalogDirectory.cs")));

        Assert.Equal(
            new[] { "CatalogEndpointModule.cs", "GlobalUsings.cs" },
            Directory.EnumerateFiles(endpoints, "*.cs", SearchOption.TopDirectoryOnly)
                .Select(Path.GetFileName)
                .OrderBy(x => x, StringComparer.Ordinal)
                .ToArray());
        Assert.False(Directory.Exists(Path.Combine(endpoints, "Errors")));
        Assert.False(Directory.Exists(Path.Combine(endpoints, "Resources")));
        Assert.True(Directory.Exists(Path.Combine(endpoints, "Admin")));
        Assert.True(Directory.Exists(Path.Combine(endpoints, "Seller")));
        Assert.True(Directory.Exists(Path.Combine(endpoints, "Storefront")));
        Assert.True(File.Exists(Path.Combine(contracts, "Errors", "CatalogErrorCodes.cs")));
        Assert.True(File.Exists(Path.Combine(contracts, "Errors", "CatalogErrorCatalogContributor.cs")));
        Assert.True(Directory.Exists(Path.Combine(contracts, "Ports")));
    }

    [Fact]
    public void Catalog_has_zero_foreign_app_infra_domain_project_references()
    {
        var root = Repo();
        foreach (var project in new[]
                 {
                     "Tooba.Catalog.Application.csproj",
                     "Tooba.Catalog.Infrastructure.csproj",
                     "Tooba.Catalog.Endpoints.csproj",
                     "Tooba.Catalog.Domain.csproj",
                     "Tooba.Catalog.Contracts.csproj",
                 })
        {
            var path = Directory.EnumerateFiles(
                    Path.Combine(root, "src/backend/Modules/Catalog"),
                    project,
                    SearchOption.AllDirectories)
                .Single();
            var text = File.ReadAllText(path);
            Assert.DoesNotMatch(
                @"ProjectReference Include=""[^""]*Tooba\.(?!Catalog\.)[A-Za-z]+\.(Application|Infrastructure|Domain)\\",
                text);
        }
    }

    private static string Repo()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "AGENTS.md")))
                return directory.FullName;
            directory = directory.Parent;
        }

        throw new InvalidOperationException("repo root not found");
    }
}
