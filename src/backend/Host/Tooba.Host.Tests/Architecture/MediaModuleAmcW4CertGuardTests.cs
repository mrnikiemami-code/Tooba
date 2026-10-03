using System.Text.Json;
using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>
/// TB-TMAR-MEDIA-AMC-001 W4 — structure gate + CERTIFIED SoT/manifest lock.
/// </summary>
public sealed class MediaModuleAmcW4CertGuardTests
{
    [Fact]
    public void Media_is_manifest_structure_certified_under_modules_media()
    {
        var root = Repo();
        var slnx = File.ReadAllText(Path.Combine(root, "src/backend/Tooba.slnx"));
        Assert.Contains("/Modules/Media/", slnx, StringComparison.Ordinal);
        Assert.Contains("Tooba.Media.Endpoints", slnx, StringComparison.Ordinal);

        using var doc = JsonDocument.Parse(File.ReadAllText(Path.Combine(
            root, "docs/architecture/tmar-module-structure-manifests.json")));
        var modules = doc.RootElement.GetProperty("modules");
        var media = modules.EnumerateArray()
            .Single(m => m.GetProperty("module").GetString() == "Media");
        Assert.True(media.GetProperty("structureCertified").GetBoolean());
        Assert.Equal("ARCH-COMPLETE-002", media.GetProperty("lockVersion").GetString());

        var projectNames = media.GetProperty("projects").EnumerateArray()
            .Select(p => p.GetProperty("projectName").GetString())
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(
            new[]
            {
                "Tooba.Media.Application",
                "Tooba.Media.Contracts",
                "Tooba.Media.Domain",
                "Tooba.Media.Endpoints",
                "Tooba.Media.Infrastructure",
            },
            projectNames);
    }

    [Fact]
    public void Media_sot_marks_complete_reference_pattern_and_structure_ready()
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(Path.Combine(
            Repo(), "docs/architecture/tmar-current-state.json")));
        var block = doc.RootElement.GetProperty("mediaAmc001");
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
        Assert.Equal(1, certified.Count(x => x == "Media"));
        Assert.Contains("Identity", certified);
        Assert.Contains("Content", certified);
    }

    [Fact]
    public void Media_roots_and_capability_folders_match_manifest_allowlists()
    {
        var root = Repo();
        var app = Path.Combine(root, "src/backend/Modules/Media/Tooba.Media.Application");
        var infra = Path.Combine(root, "src/backend/Modules/Media/Tooba.Media.Infrastructure");
        var endpoints = Path.Combine(root, "src/backend/Modules/Media/Tooba.Media.Endpoints");
        var domain = Path.Combine(root, "src/backend/Modules/Media/Tooba.Media.Domain");
        var contracts = Path.Combine(root, "src/backend/Modules/Media/Tooba.Media.Contracts");

        Assert.Empty(Directory.EnumerateFiles(app, "*.cs", SearchOption.TopDirectoryOnly));
        Assert.Empty(Directory.EnumerateFiles(domain, "*.cs", SearchOption.TopDirectoryOnly));
        Assert.Empty(Directory.EnumerateFiles(contracts, "*.cs", SearchOption.TopDirectoryOnly));
        Assert.False(Directory.Exists(Path.Combine(app, "Commands")));
        Assert.False(Directory.Exists(Path.Combine(app, "Queries")));
        Assert.True(Directory.Exists(Path.Combine(app, "Assets", "Commands")));
        Assert.True(Directory.Exists(Path.Combine(app, "Assets", "Queries")));
        Assert.True(Directory.Exists(Path.Combine(app, "Assets", "Validators")));

        Assert.Equal(
            new[] { "MediaModule.cs" },
            Directory.EnumerateFiles(infra, "*.cs", SearchOption.TopDirectoryOnly)
                .Select(Path.GetFileName)
                .OrderBy(x => x, StringComparer.Ordinal)
                .ToArray());
        Assert.False(Directory.Exists(Path.Combine(infra, "Migrations")));
        Assert.True(Directory.Exists(Path.Combine(infra, "Persistence", "Migrations")));

        Assert.Equal(
            new[] { "MediaEndpointModule.cs" },
            Directory.EnumerateFiles(endpoints, "*.cs", SearchOption.TopDirectoryOnly)
                .Select(Path.GetFileName)
                .OrderBy(x => x, StringComparer.Ordinal)
                .ToArray());
        Assert.False(File.Exists(Path.Combine(endpoints, "MediaAdminEndpoints.cs")));
        Assert.True(File.Exists(Path.Combine(endpoints, "Admin", "MediaAdminEndpoints.cs")));
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
