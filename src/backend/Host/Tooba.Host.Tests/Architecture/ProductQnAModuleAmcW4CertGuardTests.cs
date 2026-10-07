using System.Text.Json;
using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>
/// TB-TMAR-PRODUCTQNA-AMC-001 W4 — structure gate + CERTIFIED SoT/manifest lock.
/// </summary>
public sealed class ProductQnAModuleAmcW4CertGuardTests
{
    [Fact]
    public void ProductQnA_is_manifest_structure_certified_under_modules_productqna()
    {
        var root = Repo();
        var slnx = File.ReadAllText(Path.Combine(root, "src/backend/Tooba.slnx"));
        Assert.Contains("/Modules/ProductQnA/", slnx, StringComparison.Ordinal);
        Assert.Contains("Tooba.ProductQnA.Endpoints", slnx, StringComparison.Ordinal);

        using var doc = JsonDocument.Parse(File.ReadAllText(Path.Combine(
            root, "docs/architecture/tmar-module-structure-manifests.json")));
        var modules = doc.RootElement.GetProperty("modules");
        var productQnA = modules.EnumerateArray()
            .Single(m => m.GetProperty("module").GetString() == "ProductQnA");
        Assert.True(productQnA.GetProperty("structureCertified").GetBoolean());
        Assert.Equal("ARCH-COMPLETE-002", productQnA.GetProperty("lockVersion").GetString());

        var projectNames = productQnA.GetProperty("projects").EnumerateArray()
            .Select(p => p.GetProperty("projectName").GetString())
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(
            new[]
            {
                "Tooba.ProductQnA.Application",
                "Tooba.ProductQnA.Contracts",
                "Tooba.ProductQnA.Domain",
                "Tooba.ProductQnA.Endpoints",
                "Tooba.ProductQnA.Infrastructure",
            },
            projectNames);
    }

    [Fact]
    public void ProductQnA_sot_marks_complete_reference_pattern_and_structure_ready()
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(Path.Combine(
            Repo(), "docs/architecture/tmar-current-state.json")));
        var block = doc.RootElement.GetProperty("productQnAAmc001");
        Assert.Equal("COMPLETE_REFERENCE_PATTERN", block.GetProperty("state").GetString());
        Assert.Equal("READY_FOR_CERTIFY", block.GetProperty("structureState").GetString());
        Assert.True(block.GetProperty("structureCertifiedUnderArchComplete002").GetBoolean());
        Assert.True(block.GetProperty("manifestCertified").GetBoolean());
        Assert.True(block.GetProperty("microserviceExtractable").GetBoolean());
        Assert.Equal("ZERO", block.GetProperty("foreignAppInfraDomainCoupling").GetString());
        Assert.Equal("MODULE_ENDPOINTS", block.GetProperty("endpointOwnership").GetString());
        Assert.Equal(2, block.GetProperty("endpointReachableRequests").GetInt32());
        Assert.Equal(2, block.GetProperty("validatorRequiredCount").GetInt32());
        Assert.Equal(0, block.GetProperty("noValidatorRequiredCount").GetInt32());

        var certified = doc.RootElement.GetProperty("structureLock").GetProperty("certifiedModules")
            .EnumerateArray()
            .Select(x => x.GetString())
            .ToArray();
        Assert.Equal(1, certified.Count(x => x == "ProductQnA"));
        Assert.Contains("Party", certified);
        Assert.Contains("OperatorProfile", certified);
    }

    [Fact]
    public void ProductQnA_roots_and_capability_folders_match_manifest_allowlists()
    {
        var root = Repo();
        var app = Path.Combine(root, "src/backend/Modules/ProductQnA/Tooba.ProductQnA.Application");
        var infra = Path.Combine(root, "src/backend/Modules/ProductQnA/Tooba.ProductQnA.Infrastructure");
        var endpoints = Path.Combine(root, "src/backend/Modules/ProductQnA/Tooba.ProductQnA.Endpoints");
        var domain = Path.Combine(root, "src/backend/Modules/ProductQnA/Tooba.ProductQnA.Domain");
        var contracts = Path.Combine(root, "src/backend/Modules/ProductQnA/Tooba.ProductQnA.Contracts");

        Assert.Empty(Directory.EnumerateFiles(app, "*.cs", SearchOption.TopDirectoryOnly));
        Assert.Empty(Directory.EnumerateFiles(domain, "*.cs", SearchOption.TopDirectoryOnly));
        Assert.Empty(Directory.EnumerateFiles(contracts, "*.cs", SearchOption.TopDirectoryOnly));
        Assert.False(Directory.Exists(Path.Combine(app, "Commands")));
        Assert.False(Directory.Exists(Path.Combine(app, "Queries")));
        Assert.True(Directory.Exists(Path.Combine(app, "Customer", "Commands")));
        Assert.True(Directory.Exists(Path.Combine(app, "Validation")));
        Assert.True(Directory.Exists(Path.Combine(app, "Storefront", "Queries")));
        Assert.True(Directory.Exists(Path.Combine(app, "Composition")));
        Assert.False(Directory.Exists(Path.Combine(app, "Customer", "Validators")));
        Assert.False(Directory.Exists(Path.Combine(app, "Storefront", "Validators")));

        Assert.Equal(
            new[] { "ProductQnAModule.cs" },
            Directory.EnumerateFiles(infra, "*.cs", SearchOption.TopDirectoryOnly)
                .Select(Path.GetFileName)
                .OrderBy(x => x, StringComparer.Ordinal)
                .ToArray());
        Assert.False(Directory.Exists(Path.Combine(infra, "Migrations")));
        Assert.True(Directory.Exists(Path.Combine(infra, "Persistence", "Migrations")));
        Assert.True(Directory.Exists(Path.Combine(infra, "Directories")));

        Assert.Equal(
            new[] { "ProductQnAEndpointModule.cs" },
            Directory.EnumerateFiles(endpoints, "*.cs", SearchOption.TopDirectoryOnly)
                .Select(Path.GetFileName)
                .OrderBy(x => x, StringComparer.Ordinal)
                .ToArray());
        Assert.False(Directory.Exists(Path.Combine(endpoints, "Errors")));
        Assert.False(Directory.Exists(Path.Combine(endpoints, "Resources")));
        Assert.True(File.Exists(Path.Combine(endpoints, "Customer", "ProductQnACustomerEndpoints.cs")));
        Assert.True(File.Exists(Path.Combine(endpoints, "Storefront", "ProductQnAStorefrontEndpoints.cs")));
        Assert.True(File.Exists(Path.Combine(contracts, "Errors", "ProductQnAErrorCodes.cs")));
    }

    [Fact]
    public void Structure_and_certify_evidence_exist()
    {
        var root = Repo();
        Assert.True(File.Exists(Path.Combine(
            root, "docs/architecture/evidence/TB-TMAR-PRODUCTQNA-AMC-001-W4/w4-structure-gate.md")));
        Assert.True(File.Exists(Path.Combine(
            root, "docs/architecture/evidence/TB-TMAR-PRODUCTQNA-AMC-001-W4/w4-certification.md")));
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
