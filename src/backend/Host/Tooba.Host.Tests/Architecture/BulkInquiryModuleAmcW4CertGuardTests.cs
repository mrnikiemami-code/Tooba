using System.Text.Json;
using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>
/// TB-TMAR-BULKINQUIRY-AMC-001 W4 — structure gate + CERTIFIED SoT/manifest lock.
/// </summary>
public sealed class BulkInquiryModuleAmcW4CertGuardTests
{
    [Fact]
    public void BulkInquiry_is_manifest_structure_certified_under_modules_bulkinquiry()
    {
        var root = Repo();
        var slnx = File.ReadAllText(Path.Combine(root, "src/backend/Tooba.slnx"));
        Assert.Contains("/Modules/BulkInquiry/", slnx, StringComparison.Ordinal);
        Assert.Contains("Tooba.BulkInquiry.Endpoints", slnx, StringComparison.Ordinal);
        Assert.Contains("Tooba.BulkInquiry.Contracts", slnx, StringComparison.Ordinal);

        using var doc = JsonDocument.Parse(File.ReadAllText(Path.Combine(
            root, "docs/architecture/tmar-module-structure-manifests.json")));
        var modules = doc.RootElement.GetProperty("modules");
        var bulkInquiry = modules.EnumerateArray()
            .Single(m => m.GetProperty("module").GetString() == "BulkInquiry");
        Assert.True(bulkInquiry.GetProperty("structureCertified").GetBoolean());
        Assert.Equal("ARCH-COMPLETE-002", bulkInquiry.GetProperty("lockVersion").GetString());

        var projectNames = bulkInquiry.GetProperty("projects").EnumerateArray()
            .Select(p => p.GetProperty("projectName").GetString())
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(
            new[]
            {
                "Tooba.BulkInquiry.Application",
                "Tooba.BulkInquiry.Contracts",
                "Tooba.BulkInquiry.Domain",
                "Tooba.BulkInquiry.Endpoints",
                "Tooba.BulkInquiry.Infrastructure",
            },
            projectNames);
    }

    [Fact]
    public void BulkInquiry_sot_marks_complete_reference_pattern_and_structure_ready()
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(Path.Combine(
            Repo(), "docs/architecture/tmar-current-state.json")));
        var block = doc.RootElement.GetProperty("bulkInquiryAmc001");
        Assert.Equal("COMPLETE_REFERENCE_PATTERN", block.GetProperty("state").GetString());
        Assert.Equal("READY_FOR_CERTIFY", block.GetProperty("structureState").GetString());
        Assert.True(block.GetProperty("structureCertifiedUnderArchComplete002").GetBoolean());
        Assert.True(block.GetProperty("manifestCertified").GetBoolean());
        Assert.True(block.GetProperty("microserviceExtractable").GetBoolean());
        Assert.Equal("ZERO", block.GetProperty("foreignAppInfraDomainCoupling").GetString());
        Assert.Equal("MODULE_ENDPOINTS", block.GetProperty("endpointOwnership").GetString());
        Assert.Equal(1, block.GetProperty("endpointReachableRequests").GetInt32());
        Assert.Equal(1, block.GetProperty("validatorRequiredCount").GetInt32());
        Assert.Equal(0, block.GetProperty("noValidatorRequiredCount").GetInt32());

        var certified = doc.RootElement.GetProperty("structureLock").GetProperty("certifiedModules")
            .EnumerateArray()
            .Select(x => x.GetString())
            .ToArray();
        Assert.Equal(1, certified.Count(x => x == "BulkInquiry"));
        Assert.Contains("PageComposition", certified);
        Assert.Contains("ProductQnA", certified);
    }

    [Fact]
    public void BulkInquiry_roots_and_capability_folders_match_manifest_allowlists()
    {
        var root = Repo();
        var app = Path.Combine(root, "src/backend/Modules/BulkInquiry/Tooba.BulkInquiry.Application");
        var infra = Path.Combine(root, "src/backend/Modules/BulkInquiry/Tooba.BulkInquiry.Infrastructure");
        var endpoints = Path.Combine(root, "src/backend/Modules/BulkInquiry/Tooba.BulkInquiry.Endpoints");
        var domain = Path.Combine(root, "src/backend/Modules/BulkInquiry/Tooba.BulkInquiry.Domain");
        var contracts = Path.Combine(root, "src/backend/Modules/BulkInquiry/Tooba.BulkInquiry.Contracts");

        Assert.Empty(Directory.EnumerateFiles(app, "*.cs", SearchOption.TopDirectoryOnly));
        Assert.Empty(Directory.EnumerateFiles(domain, "*.cs", SearchOption.TopDirectoryOnly));
        Assert.Empty(Directory.EnumerateFiles(contracts, "*.cs", SearchOption.TopDirectoryOnly));
        Assert.False(Directory.Exists(Path.Combine(app, "Commands")));
        Assert.True(Directory.Exists(Path.Combine(app, "Storefront", "Commands")));
        Assert.True(Directory.Exists(Path.Combine(app, "Validation")));
        Assert.False(Directory.Exists(Path.Combine(app, "Storefront", "Validators")));
        Assert.True(Directory.Exists(Path.Combine(app, "Composition")));

        Assert.Equal(
            new[] { "BulkInquiryModule.cs" },
            Directory.EnumerateFiles(infra, "*.cs", SearchOption.TopDirectoryOnly)
                .Select(Path.GetFileName)
                .OrderBy(x => x, StringComparer.Ordinal)
                .ToArray());
        Assert.False(Directory.Exists(Path.Combine(infra, "Migrations")));
        Assert.True(Directory.Exists(Path.Combine(infra, "Persistence", "Migrations")));
        Assert.True(Directory.Exists(Path.Combine(infra, "Directories")));

        Assert.Equal(
            new[] { "BulkInquiryEndpointModule.cs" },
            Directory.EnumerateFiles(endpoints, "*.cs", SearchOption.TopDirectoryOnly)
                .Select(Path.GetFileName)
                .OrderBy(x => x, StringComparer.Ordinal)
                .ToArray());
        Assert.False(Directory.Exists(Path.Combine(endpoints, "Errors")));
        Assert.False(Directory.Exists(Path.Combine(endpoints, "Resources")));
        Assert.True(File.Exists(Path.Combine(endpoints, "Storefront", "BulkInquiryStorefrontEndpoints.cs")));
        Assert.True(File.Exists(Path.Combine(contracts, "Errors", "BulkInquiryErrorCodes.cs")));
        Assert.True(File.Exists(Path.Combine(domain, "Aggregates", "BulkPurchaseInquiry.cs")));
        Assert.True(File.Exists(Path.Combine(domain, "Enums", "BulkInquiryStatus.cs")));
    }

    [Fact]
    public void Structure_and_certify_evidence_exist()
    {
        var root = Repo();
        Assert.True(File.Exists(Path.Combine(
            root, "docs/architecture/evidence/TB-TMAR-BULKINQUIRY-AMC-001-W4/w4-structure-gate.md")));
        Assert.True(File.Exists(Path.Combine(
            root, "docs/architecture/evidence/TB-TMAR-BULKINQUIRY-AMC-001-W4/w4-certification.md")));
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
