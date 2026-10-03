using System.Text.Json;
using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>
/// TB-TMAR-WISHLIST-AMC-001 W4 — structure gate + CERTIFIED SoT/manifest lock.
/// </summary>
public sealed class WishlistModuleAmcW4CertGuardTests
{
    [Fact]
    public void Wishlist_is_manifest_structure_certified_under_modules_wishlist()
    {
        var root = Repo();
        var slnx = File.ReadAllText(Path.Combine(root, "src/backend/Tooba.slnx"));
        Assert.Contains("/Modules/Wishlist/", slnx, StringComparison.Ordinal);
        Assert.Contains("Tooba.Wishlist.Endpoints", slnx, StringComparison.Ordinal);
        Assert.Contains("Tooba.Wishlist.Contracts", slnx, StringComparison.Ordinal);

        using var doc = JsonDocument.Parse(File.ReadAllText(Path.Combine(
            root, "docs/architecture/tmar-module-structure-manifests.json")));
        var modules = doc.RootElement.GetProperty("modules");
        var wishlist = modules.EnumerateArray()
            .Single(m => m.GetProperty("module").GetString() == "Wishlist");
        Assert.True(wishlist.GetProperty("structureCertified").GetBoolean());
        Assert.Equal("ARCH-COMPLETE-002", wishlist.GetProperty("lockVersion").GetString());

        var projectNames = wishlist.GetProperty("projects").EnumerateArray()
            .Select(p => p.GetProperty("projectName").GetString())
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(
            new[]
            {
                "Tooba.Wishlist.Application",
                "Tooba.Wishlist.Contracts",
                "Tooba.Wishlist.Domain",
                "Tooba.Wishlist.Endpoints",
                "Tooba.Wishlist.Infrastructure",
            },
            projectNames);
    }

    [Fact]
    public void Wishlist_sot_marks_complete_reference_pattern_and_structure_ready()
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(Path.Combine(
            Repo(), "docs/architecture/tmar-current-state.json")));
        var block = doc.RootElement.GetProperty("wishlistAmc001");
        Assert.Equal("COMPLETE_REFERENCE_PATTERN", block.GetProperty("state").GetString());
        Assert.Equal("READY_FOR_CERTIFY", block.GetProperty("structureState").GetString());
        Assert.True(block.GetProperty("structureCertifiedUnderArchComplete002").GetBoolean());
        Assert.True(block.GetProperty("manifestCertified").GetBoolean());
        Assert.True(block.GetProperty("microserviceExtractable").GetBoolean());
        Assert.Equal("ZERO", block.GetProperty("foreignAppInfraDomainCoupling").GetString());
        Assert.Equal("MODULE_ENDPOINTS", block.GetProperty("endpointOwnership").GetString());
        Assert.Equal(4, block.GetProperty("endpointReachableRequests").GetInt32());
        Assert.Equal(3, block.GetProperty("validatorRequiredCount").GetInt32());
        Assert.Equal(1, block.GetProperty("noValidatorRequiredCount").GetInt32());

        var certified = doc.RootElement.GetProperty("structureLock").GetProperty("certifiedModules")
            .EnumerateArray()
            .Select(x => x.GetString())
            .ToArray();
        Assert.Equal(1, certified.Count(x => x == "Wishlist"));
        Assert.Contains("BulkInquiry", certified);
        Assert.Contains("PageComposition", certified);
    }

    [Fact]
    public void Wishlist_roots_and_capability_folders_match_manifest_allowlists()
    {
        var root = Repo();
        var app = Path.Combine(root, "src/backend/Modules/Wishlist/Tooba.Wishlist.Application");
        var infra = Path.Combine(root, "src/backend/Modules/Wishlist/Tooba.Wishlist.Infrastructure");
        var endpoints = Path.Combine(root, "src/backend/Modules/Wishlist/Tooba.Wishlist.Endpoints");
        var domain = Path.Combine(root, "src/backend/Modules/Wishlist/Tooba.Wishlist.Domain");
        var contracts = Path.Combine(root, "src/backend/Modules/Wishlist/Tooba.Wishlist.Contracts");

        Assert.Empty(Directory.EnumerateFiles(app, "*.cs", SearchOption.TopDirectoryOnly));
        Assert.Empty(Directory.EnumerateFiles(domain, "*.cs", SearchOption.TopDirectoryOnly));
        Assert.Empty(Directory.EnumerateFiles(contracts, "*.cs", SearchOption.TopDirectoryOnly));
        Assert.False(Directory.Exists(Path.Combine(app, "Commands")));
        Assert.False(Directory.Exists(Path.Combine(app, "Presentation")));
        Assert.True(Directory.Exists(Path.Combine(app, "Customer", "Commands")));
        Assert.True(Directory.Exists(Path.Combine(app, "Customer", "Queries")));
        Assert.True(Directory.Exists(Path.Combine(app, "Customer", "Validators")));
        Assert.True(Directory.Exists(Path.Combine(app, "Composition")));

        Assert.Equal(
            new[] { "WishlistModule.cs" },
            Directory.EnumerateFiles(infra, "*.cs", SearchOption.TopDirectoryOnly)
                .Select(Path.GetFileName)
                .OrderBy(x => x, StringComparer.Ordinal)
                .ToArray());
        Assert.False(Directory.Exists(Path.Combine(infra, "Migrations")));
        Assert.True(Directory.Exists(Path.Combine(infra, "Persistence", "Migrations")));
        Assert.True(File.Exists(Path.Combine(infra, "Persistence", "WishlistOutboxRegistration.cs")));
        Assert.True(Directory.Exists(Path.Combine(infra, "Directories")));

        Assert.Equal(
            new[] { "WishlistEndpointModule.cs" },
            Directory.EnumerateFiles(endpoints, "*.cs", SearchOption.TopDirectoryOnly)
                .Select(Path.GetFileName)
                .OrderBy(x => x, StringComparer.Ordinal)
                .ToArray());
        Assert.False(Directory.Exists(Path.Combine(endpoints, "Errors")));
        Assert.False(Directory.Exists(Path.Combine(endpoints, "Resources")));
        Assert.True(File.Exists(Path.Combine(endpoints, "Customer", "WishlistCustomerEndpoints.cs")));
        Assert.True(File.Exists(Path.Combine(contracts, "Errors", "WishlistErrorCodes.cs")));
        Assert.True(File.Exists(Path.Combine(contracts, "Ports", "IWishlistCountPort.cs")));
        Assert.True(File.Exists(Path.Combine(domain, "Aggregates", "WishlistItem.cs")));
    }

    [Fact]
    public void Structure_and_certify_evidence_exist()
    {
        var root = Repo();
        Assert.True(File.Exists(Path.Combine(
            root, "docs/architecture/evidence/TB-TMAR-WISHLIST-AMC-001-W4/w4-structure-gate.md")));
        Assert.True(File.Exists(Path.Combine(
            root, "docs/architecture/evidence/TB-TMAR-WISHLIST-AMC-001-W4/w4-certification.md")));
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
