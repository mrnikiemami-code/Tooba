using System.Text.Json;
using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>
/// TB-TMAR-IDENTITY-AMC-001 W5/W6 — structure gate + SoT/manifest lock for the historical
/// AMC-001 lineage.
/// Reconciled in TB-TMAR-IDENTITY-AMSC-001-W3-R1: the current Identity certification authority is
/// the AMSC-001 four-wave lineage (<c>identityModuleAmsc001W0..W3</c>), so this guard now pins the
/// <b>historical</b> AMC-001 truth on <c>identityAmc001</c> (<c>structureState = READY_FOR_CERTIFY</c>,
/// 6 REQUIRED + 7 NO_VALIDATOR_REQUIRED) and asserts the current AMSC truth against
/// <c>identityModuleAmsc001W3</c>. The W3 wave had rewritten the historical record with current
/// AMSC values; R1 restored it. Every structural assertion is unchanged.
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

        // Historical AMC-001 record: truthful pre-W3 values (restored by TB-TMAR-IDENTITY-AMSC-001-W3-R1).
        var amc = doc.RootElement.GetProperty("identityAmc001");
        Assert.Equal("COMPLETE_REFERENCE_PATTERN", amc.GetProperty("state").GetString());
        Assert.Equal("READY_FOR_CERTIFY", amc.GetProperty("structureState").GetString());
        Assert.True(amc.GetProperty("structureCertifiedUnderArchComplete002").GetBoolean());
        Assert.True(amc.GetProperty("manifestCertified").GetBoolean());
        Assert.True(amc.GetProperty("microserviceExtractable").GetBoolean());
        Assert.Equal("ZERO", amc.GetProperty("foreignAppInfraDomainCoupling").GetString());
        Assert.Equal("MODULE_ENDPOINTS", amc.GetProperty("endpointOwnership").GetString());
        Assert.Equal(
            "COMPLETE_6_OF_6_REQUIRED_PRESENT_7_NO_VALIDATOR_REQUIRED",
            amc.GetProperty("validatorCoverage").GetString());
        Assert.Equal(6, amc.GetProperty("validatorRequiredCount").GetInt32());
        Assert.Equal(7, amc.GetProperty("noValidatorRequiredCount").GetInt32());

        // Current AMSC-001 certification authority: the W3 record.
        var w3 = doc.RootElement.GetProperty("identityModuleAmsc001W3");
        Assert.Equal("IDENTITY_AMSC_001_CERTIFIED", w3.GetProperty("state").GetString());
        Assert.Equal("CERTIFIED", w3.GetProperty("structureState").GetString());
        Assert.Equal(
            "EXHAUSTIVE_9_REQUIRED_PRESENT_4_NO_VALIDATOR_REQUIRED",
            w3.GetProperty("validatorCoverageState").GetString());
        Assert.Equal(13, w3.GetProperty("endpointReachableRequests").GetInt32());
        Assert.Equal("ZERO", w3.GetProperty("foreignAppInfraDomainCoupling").GetString());
        Assert.Equal("ZERO", w3.GetProperty("blockingResidualDebt").GetString());
        Assert.True(w3.GetProperty("microserviceExtractable").GetBoolean());
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
