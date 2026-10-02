using System.Text.Json;
using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>
/// TB-TMAR-IDENTITY-AMC-001 W5/W6 — structure gate + CERTIFIED SoT/manifest lock.
/// </summary>
public sealed class IdentityModuleAmcW5CertGuardTests
{
    [Fact]
    public void Identity_is_manifest_structure_certified_under_modules_identity()
    {
        var root = Repo();
        var slnx = File.ReadAllText(Path.Combine(root, "src/backend/Tooba.slnx"));
        Assert.Contains("/Modules/Identity/", slnx, StringComparison.Ordinal);
        Assert.Contains("Tooba.Identity.Endpoints", slnx, StringComparison.Ordinal);

        using var doc = JsonDocument.Parse(File.ReadAllText(Path.Combine(
            root, "docs/architecture/tmar-module-structure-manifests.json")));
        var modules = doc.RootElement.GetProperty("modules");
        var identity = modules.EnumerateArray()
            .Single(m => m.GetProperty("module").GetString() == "Identity");
        Assert.True(identity.GetProperty("structureCertified").GetBoolean());
        Assert.Equal("ARCH-COMPLETE-002", identity.GetProperty("lockVersion").GetString());

        var projectNames = identity.GetProperty("projects").EnumerateArray()
            .Select(p => p.GetProperty("projectName").GetString())
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(
            new[]
            {
                "Tooba.Identity.Application",
                "Tooba.Identity.Contracts",
                "Tooba.Identity.Domain",
                "Tooba.Identity.Endpoints",
                "Tooba.Identity.Infrastructure",
            },
            projectNames);
    }

    [Fact]
    public void Identity_sot_marks_complete_reference_pattern_and_structure_ready()
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(Path.Combine(
            Repo(), "docs/architecture/tmar-current-state.json")));
        var block = doc.RootElement.GetProperty("identityAmc001");
        Assert.Equal("COMPLETE_REFERENCE_PATTERN", block.GetProperty("state").GetString());
        Assert.Equal("READY_FOR_CERTIFY", block.GetProperty("structureState").GetString());
        Assert.True(block.GetProperty("structureCertifiedUnderArchComplete002").GetBoolean());
        Assert.True(block.GetProperty("manifestCertified").GetBoolean());
        Assert.True(block.GetProperty("microserviceExtractable").GetBoolean());
        Assert.Equal("ZERO", block.GetProperty("foreignAppInfraDomainCoupling").GetString());
        Assert.Equal("MODULE_ENDPOINTS", block.GetProperty("endpointOwnership").GetString());
    }

    [Fact]
    public void Contracts_auth_path_namespace_is_exact_and_infra_outbox_not_at_root()
    {
        var root = Repo();
        var authContract = File.ReadAllText(Path.Combine(
            root,
            "src/backend/Modules/Identity/Tooba.Identity.Contracts/Auth/IdentityAuthenticationContracts.cs"));
        Assert.Contains("namespace Tooba.Identity.Contracts.Auth", authContract, StringComparison.Ordinal);

        Assert.False(File.Exists(Path.Combine(
            root, "src/backend/Modules/Identity/Tooba.Identity.Infrastructure/IdentityOutboxRegistration.cs")));
        Assert.True(File.Exists(Path.Combine(
            root, "src/backend/Modules/Identity/Tooba.Identity.Infrastructure/Persistence/IdentityOutboxRegistration.cs")));
        Assert.False(File.Exists(Path.Combine(
            root, "src/backend/Host/Tooba.Host/Authentication/AuthenticationHttpBoundary.cs")));
    }

    private static string Repo()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "AGENTS.md")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("repo root not found");
    }
}
