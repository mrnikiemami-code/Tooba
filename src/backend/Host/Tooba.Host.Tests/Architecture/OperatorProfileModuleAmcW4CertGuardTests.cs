using System.Text.Json;
using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>
/// TB-TMAR-OPERATORPROFILE-AMC-001 W4 — structure gate + CERTIFIED SoT/manifest lock.
/// </summary>
public sealed class OperatorProfileModuleAmcW4CertGuardTests
{
    [Fact]
    public void OperatorProfile_is_manifest_structure_certified_under_modules_operatorprofile()
    {
        var root = Repo();
        var slnx = File.ReadAllText(Path.Combine(root, "src/backend/Tooba.slnx"));
        Assert.Contains("/Modules/OperatorProfile/", slnx, StringComparison.Ordinal);
        Assert.Contains("Tooba.OperatorProfile.Endpoints", slnx, StringComparison.Ordinal);

        using var doc = JsonDocument.Parse(File.ReadAllText(Path.Combine(
            root, "docs/architecture/tmar-module-structure-manifests.json")));
        var localization = doc.RootElement.GetProperty("modules").EnumerateArray()
            .Single(m => m.GetProperty("module").GetString() == "OperatorProfile");
        Assert.True(localization.GetProperty("structureCertified").GetBoolean());
        Assert.Equal("ARCH-COMPLETE-002", localization.GetProperty("lockVersion").GetString());

        var projectNames = localization.GetProperty("projects").EnumerateArray()
            .Select(p => p.GetProperty("projectName").GetString())
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(
            new[]
            {
                "Tooba.OperatorProfile.Application",
                "Tooba.OperatorProfile.Contracts",
                "Tooba.OperatorProfile.Domain",
                "Tooba.OperatorProfile.Endpoints",
                "Tooba.OperatorProfile.Infrastructure",
            },
            projectNames);
    }

    [Fact]
    public void OperatorProfile_sot_marks_complete_reference_pattern_and_structure_ready()
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(Path.Combine(
            Repo(), "docs/architecture/tmar-current-state.json")));
        var block = doc.RootElement.GetProperty("operatorProfileAmc001");
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
        Assert.Equal(1, certified.Count(x => x == "OperatorProfile"));
        Assert.Contains("Localization", certified);
        Assert.Contains("Media", certified);
    }

    [Fact]
    public void OperatorProfile_roots_and_capability_folders_match_manifest_allowlists()
    {
        var root = Repo();
        var app = Path.Combine(root, "src/backend/Modules/OperatorProfile/Tooba.OperatorProfile.Application");
        var infra = Path.Combine(root, "src/backend/Modules/OperatorProfile/Tooba.OperatorProfile.Infrastructure");
        var endpoints = Path.Combine(root, "src/backend/Modules/OperatorProfile/Tooba.OperatorProfile.Endpoints");
        var domain = Path.Combine(root, "src/backend/Modules/OperatorProfile/Tooba.OperatorProfile.Domain");
        var contracts = Path.Combine(root, "src/backend/Modules/OperatorProfile/Tooba.OperatorProfile.Contracts");

        Assert.Empty(Directory.EnumerateFiles(app, "*.cs", SearchOption.TopDirectoryOnly));
        Assert.Empty(Directory.EnumerateFiles(domain, "*.cs", SearchOption.TopDirectoryOnly));
        Assert.Empty(Directory.EnumerateFiles(contracts, "*.cs", SearchOption.TopDirectoryOnly));
        Assert.False(Directory.Exists(Path.Combine(app, "Commands")));
        Assert.False(Directory.Exists(Path.Combine(app, "Queries")));
        Assert.True(Directory.Exists(Path.Combine(app, "Admin", "Commands")));
        Assert.True(Directory.Exists(Path.Combine(app, "Admin", "Queries")));
        Assert.True(Directory.Exists(Path.Combine(app, "Admin", "Validators")));

        Assert.Equal(
            new[] { "OperatorProfileModule.cs" },
            Directory.EnumerateFiles(infra, "*.cs", SearchOption.TopDirectoryOnly)
                .Select(Path.GetFileName)
                .OrderBy(x => x, StringComparer.Ordinal)
                .ToArray());
        Assert.False(Directory.Exists(Path.Combine(infra, "Migrations")));
        Assert.True(Directory.Exists(Path.Combine(infra, "Persistence", "Migrations")));

        Assert.Equal(
            new[] { "OperatorProfileEndpointModule.cs" },
            Directory.EnumerateFiles(endpoints, "*.cs", SearchOption.TopDirectoryOnly)
                .Select(Path.GetFileName)
                .OrderBy(x => x, StringComparer.Ordinal)
                .ToArray());
        Assert.False(Directory.Exists(Path.Combine(endpoints, "Errors")));
        Assert.True(File.Exists(Path.Combine(endpoints, "Admin", "OperatorProfileAdminEndpoints.cs")));
    }

    [Fact]
    public void Structure_and_certify_evidence_exist()
    {
        var root = Repo();
        Assert.True(File.Exists(Path.Combine(
            root, "docs/architecture/evidence/TB-TMAR-OPERATORPROFILE-AMC-001-W4/w4-structure-gate.md")));
        Assert.True(File.Exists(Path.Combine(
            root, "docs/architecture/evidence/TB-TMAR-OPERATORPROFILE-AMC-001-W4/w4-certification.md")));
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
