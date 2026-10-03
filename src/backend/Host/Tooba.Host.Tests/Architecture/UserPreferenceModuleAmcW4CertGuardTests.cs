using System.Text.Json;
using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>
/// TB-TMAR-USERPREFERENCE-AMC-001 W4 — structure gate + CERTIFIED SoT/manifest lock.
/// </summary>
public sealed class UserPreferenceModuleAmcW4CertGuardTests
{
    [Fact]
    public void UserPreference_is_manifest_structure_certified_under_modules_userpreference()
    {
        var root = Repo();
        var slnx = File.ReadAllText(Path.Combine(root, "src/backend/Tooba.slnx"));
        Assert.Contains("/Modules/UserPreference/", slnx, StringComparison.Ordinal);
        Assert.Contains("Tooba.UserPreference.Endpoints", slnx, StringComparison.Ordinal);
        Assert.Contains("Tooba.UserPreference.Contracts", slnx, StringComparison.Ordinal);

        using var doc = JsonDocument.Parse(File.ReadAllText(Path.Combine(
            root, "docs/architecture/tmar-module-structure-manifests.json")));
        var modules = doc.RootElement.GetProperty("modules");
        var userPreference = modules.EnumerateArray()
            .Single(m => m.GetProperty("module").GetString() == "UserPreference");
        Assert.True(userPreference.GetProperty("structureCertified").GetBoolean());
        Assert.Equal("ARCH-COMPLETE-002", userPreference.GetProperty("lockVersion").GetString());

        var projectNames = userPreference.GetProperty("projects").EnumerateArray()
            .Select(p => p.GetProperty("projectName").GetString())
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(
            new[]
            {
                "Tooba.UserPreference.Application",
                "Tooba.UserPreference.Contracts",
                "Tooba.UserPreference.Domain",
                "Tooba.UserPreference.Endpoints",
                "Tooba.UserPreference.Infrastructure",
            },
            projectNames);
    }

    [Fact]
    public void UserPreference_sot_marks_complete_reference_pattern_and_structure_ready()
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(Path.Combine(
            Repo(), "docs/architecture/tmar-current-state.json")));
        var block = doc.RootElement.GetProperty("userPreferenceAmc001");
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
        Assert.Equal(1, certified.Count(x => x == "UserPreference"));
        Assert.Contains("Wishlist", certified);
        Assert.Contains("BulkInquiry", certified);
    }

    [Fact]
    public void UserPreference_roots_and_capability_folders_match_manifest_allowlists()
    {
        var root = Repo();
        var app = Path.Combine(root, "src/backend/Modules/UserPreference/Tooba.UserPreference.Application");
        var infra = Path.Combine(root, "src/backend/Modules/UserPreference/Tooba.UserPreference.Infrastructure");
        var endpoints = Path.Combine(root, "src/backend/Modules/UserPreference/Tooba.UserPreference.Endpoints");
        var domain = Path.Combine(root, "src/backend/Modules/UserPreference/Tooba.UserPreference.Domain");
        var contracts = Path.Combine(root, "src/backend/Modules/UserPreference/Tooba.UserPreference.Contracts");

        Assert.Empty(Directory.EnumerateFiles(app, "*.cs", SearchOption.TopDirectoryOnly));
        Assert.Empty(Directory.EnumerateFiles(domain, "*.cs", SearchOption.TopDirectoryOnly));
        Assert.Empty(Directory.EnumerateFiles(contracts, "*.cs", SearchOption.TopDirectoryOnly));
        Assert.False(Directory.Exists(Path.Combine(app, "Commands")));
        Assert.False(Directory.Exists(Path.Combine(app, "Presentation")));
        Assert.True(Directory.Exists(Path.Combine(app, "LocalePreferences", "Commands")));
        Assert.True(Directory.Exists(Path.Combine(app, "LocalePreferences", "Queries")));
        Assert.True(Directory.Exists(Path.Combine(app, "LocalePreferences", "Validators")));
        Assert.True(Directory.Exists(Path.Combine(app, "UiPreferences", "Commands")));
        Assert.True(Directory.Exists(Path.Combine(app, "UiPreferences", "Queries")));
        Assert.True(Directory.Exists(Path.Combine(app, "UiPreferences", "Validators")));
        Assert.True(Directory.Exists(Path.Combine(app, "Composition")));

        Assert.Equal(
            new[] { "UserPreferenceModule.cs" },
            Directory.EnumerateFiles(infra, "*.cs", SearchOption.TopDirectoryOnly)
                .Select(Path.GetFileName)
                .OrderBy(x => x, StringComparer.Ordinal)
                .ToArray());
        Assert.False(Directory.Exists(Path.Combine(infra, "Migrations")));
        Assert.True(Directory.Exists(Path.Combine(infra, "Persistence", "Migrations")));
        Assert.True(File.Exists(Path.Combine(infra, "Persistence", "UserPreferenceOutboxRegistration.cs")));
        Assert.True(Directory.Exists(Path.Combine(infra, "Directories")));

        Assert.Equal(
            new[] { "UserPreferenceEndpointModule.cs" },
            Directory.EnumerateFiles(endpoints, "*.cs", SearchOption.TopDirectoryOnly)
                .Select(Path.GetFileName)
                .OrderBy(x => x, StringComparer.Ordinal)
                .ToArray());
        Assert.False(Directory.Exists(Path.Combine(endpoints, "Errors")));
        Assert.False(Directory.Exists(Path.Combine(endpoints, "Resources")));
        Assert.True(File.Exists(Path.Combine(endpoints, "Customer", "UserPreferenceCustomerEndpoints.cs")));
        Assert.True(File.Exists(Path.Combine(endpoints, "Admin", "UiPreferenceAdminEndpoints.cs")));
        Assert.True(File.Exists(Path.Combine(contracts, "Errors", "UserPreferenceErrorCodes.cs")));
        Assert.True(File.Exists(Path.Combine(domain, "Aggregates", "UserPreference.cs")));
        Assert.True(File.Exists(Path.Combine(domain, "Aggregates", "UiPreference.cs")));
    }

    [Fact]
    public void Structure_and_certify_evidence_exist()
    {
        var root = Repo();
        Assert.True(File.Exists(Path.Combine(
            root, "docs/architecture/evidence/TB-TMAR-USERPREFERENCE-AMC-001-W4/w4-structure-gate.md")));
        Assert.True(File.Exists(Path.Combine(
            root, "docs/architecture/evidence/TB-TMAR-USERPREFERENCE-AMC-001-W4/w4-certification.md")));
    }

    [Fact]
    public void UserPreference_foreign_app_infra_domain_coupling_is_zero()
    {
        var root = Repo();
        var projects = new[]
        {
            "Tooba.UserPreference.Application/Tooba.UserPreference.Application.csproj",
            "Tooba.UserPreference.Domain/Tooba.UserPreference.Domain.csproj",
            "Tooba.UserPreference.Infrastructure/Tooba.UserPreference.Infrastructure.csproj",
            "Tooba.UserPreference.Endpoints/Tooba.UserPreference.Endpoints.csproj",
            "Tooba.UserPreference.Contracts/Tooba.UserPreference.Contracts.csproj",
        };

        foreach (var relative in projects)
        {
            var csproj = File.ReadAllText(Path.Combine(root, "src/backend/Modules/UserPreference", relative));
            Assert.DoesNotContain("Tooba.Catalog.", csproj, StringComparison.Ordinal);
            Assert.DoesNotContain("Tooba.Cart.", csproj, StringComparison.Ordinal);
            Assert.DoesNotContain("Tooba.Wishlist.", csproj, StringComparison.Ordinal);
            Assert.DoesNotContain("Tooba.Identity.", csproj, StringComparison.Ordinal);
            Assert.DoesNotContain("Tooba.Party.", csproj, StringComparison.Ordinal);
            Assert.DoesNotContain("Tooba.OperatorProfile.", csproj, StringComparison.Ordinal);
            Assert.DoesNotContain("Tooba.Order.Application", csproj, StringComparison.Ordinal);
            Assert.DoesNotContain("Tooba.Order.Domain", csproj, StringComparison.Ordinal);
            Assert.DoesNotContain("Tooba.Order.Infrastructure", csproj, StringComparison.Ordinal);
        }

        var endpointsCsproj = File.ReadAllText(Path.Combine(
            root,
            "src/backend/Modules/UserPreference/Tooba.UserPreference.Endpoints/Tooba.UserPreference.Endpoints.csproj"));
        Assert.Contains("Tooba.Order.Contracts", endpointsCsproj, StringComparison.Ordinal);
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
