using System.Text.Json;
using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>
/// TB-TMAR-LOCALIZATION-AMC-001 W4 — structure gate + CERTIFIED SoT/manifest lock.
/// </summary>
public sealed class LocalizationModuleAmcW4CertGuardTests
{
    [Fact]
    public void Localization_is_manifest_structure_certified_under_modules_localization()
    {
        var root = Repo();
        var slnx = File.ReadAllText(Path.Combine(root, "src/backend/Tooba.slnx"));
        Assert.Contains("/Modules/Localization/", slnx, StringComparison.Ordinal);
        Assert.Contains("Tooba.Localization.Endpoints", slnx, StringComparison.Ordinal);

        using var doc = JsonDocument.Parse(File.ReadAllText(Path.Combine(
            root, "docs/architecture/tmar-module-structure-manifests.json")));
        var modules = doc.RootElement.GetProperty("modules");
        var localization = modules.EnumerateArray()
            .Single(m => m.GetProperty("module").GetString() == "Localization");
        Assert.True(localization.GetProperty("structureCertified").GetBoolean());
        Assert.Equal("ARCH-COMPLETE-002", localization.GetProperty("lockVersion").GetString());

        var projectNames = localization.GetProperty("projects").EnumerateArray()
            .Select(p => p.GetProperty("projectName").GetString())
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(
            new[]
            {
                "Tooba.Localization.Application",
                "Tooba.Localization.Contracts",
                "Tooba.Localization.Domain",
                "Tooba.Localization.Endpoints",
                "Tooba.Localization.Infrastructure",
            },
            projectNames);
    }

    [Fact]
    public void Localization_sot_marks_complete_reference_pattern_and_structure_ready()
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(Path.Combine(
            Repo(), "docs/architecture/tmar-current-state.json")));
        var block = doc.RootElement.GetProperty("localizationAmc001");
        Assert.Equal("COMPLETE_REFERENCE_PATTERN", block.GetProperty("state").GetString());
        Assert.Equal("READY_FOR_CERTIFY", block.GetProperty("structureState").GetString());
        Assert.True(block.GetProperty("structureCertifiedUnderArchComplete002").GetBoolean());
        Assert.True(block.GetProperty("manifestCertified").GetBoolean());
        Assert.True(block.GetProperty("microserviceExtractable").GetBoolean());
        Assert.Equal("ZERO", block.GetProperty("foreignAppInfraDomainCoupling").GetString());
        Assert.Equal("MODULE_ENDPOINTS", block.GetProperty("endpointOwnership").GetString());
        Assert.Equal(4, block.GetProperty("endpointReachableRequests").GetInt32());
        Assert.Equal(4, block.GetProperty("validatorRequiredCount").GetInt32());
        Assert.Equal(0, block.GetProperty("noValidatorRequiredCount").GetInt32());

        var certified = doc.RootElement.GetProperty("structureLock").GetProperty("certifiedModules")
            .EnumerateArray()
            .Select(x => x.GetString())
            .ToArray();
        Assert.Equal(1, certified.Count(x => x == "Localization"));
        Assert.Contains("Media", certified);
        Assert.Contains("Content", certified);
    }

    [Fact]
    public void Localization_roots_and_capability_folders_match_manifest_allowlists()
    {
        var root = Repo();
        var app = Path.Combine(root, "src/backend/Modules/Localization/Tooba.Localization.Application");
        var infra = Path.Combine(root, "src/backend/Modules/Localization/Tooba.Localization.Infrastructure");
        var endpoints = Path.Combine(root, "src/backend/Modules/Localization/Tooba.Localization.Endpoints");
        var domain = Path.Combine(root, "src/backend/Modules/Localization/Tooba.Localization.Domain");
        var contracts = Path.Combine(root, "src/backend/Modules/Localization/Tooba.Localization.Contracts");

        Assert.Empty(Directory.EnumerateFiles(app, "*.cs", SearchOption.TopDirectoryOnly));
        Assert.Empty(Directory.EnumerateFiles(domain, "*.cs", SearchOption.TopDirectoryOnly));
        Assert.Empty(Directory.EnumerateFiles(contracts, "*.cs", SearchOption.TopDirectoryOnly));
        Assert.False(Directory.Exists(Path.Combine(app, "Commands")));
        Assert.False(Directory.Exists(Path.Combine(app, "Queries")));
        Assert.True(Directory.Exists(Path.Combine(app, "Languages", "Commands")));
        Assert.True(Directory.Exists(Path.Combine(app, "Languages", "Queries")));
        Assert.True(Directory.Exists(Path.Combine(app, "Languages", "Validators")));

        Assert.Equal(
            new[] { "LocalizationModule.cs" },
            Directory.EnumerateFiles(infra, "*.cs", SearchOption.TopDirectoryOnly)
                .Select(Path.GetFileName)
                .OrderBy(x => x, StringComparer.Ordinal)
                .ToArray());
        Assert.False(Directory.Exists(Path.Combine(infra, "Migrations")));
        Assert.True(Directory.Exists(Path.Combine(infra, "Persistence", "Migrations")));

        Assert.Equal(
            new[] { "LocalizationEndpointModule.cs" },
            Directory.EnumerateFiles(endpoints, "*.cs", SearchOption.TopDirectoryOnly)
                .Select(Path.GetFileName)
                .OrderBy(x => x, StringComparer.Ordinal)
                .ToArray());
        Assert.False(Directory.Exists(Path.Combine(endpoints, "Errors")));
        Assert.False(File.Exists(Path.Combine(endpoints, "LocaleAdminEndpoints.cs")));
        Assert.True(File.Exists(Path.Combine(endpoints, "Admin", "LocaleAdminEndpoints.cs")));
    }

    [Fact]
    public void Structure_and_certify_evidence_exist()
    {
        var root = Repo();
        Assert.True(File.Exists(Path.Combine(
            root, "docs/architecture/evidence/TB-TMAR-LOCALIZATION-AMC-001-W4/w4-structure-gate.md")));
        Assert.True(File.Exists(Path.Combine(
            root, "docs/architecture/evidence/TB-TMAR-LOCALIZATION-AMC-001-W4/w4-certification.md")));
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
