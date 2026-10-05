using System.Text.Json;
using System.Text.RegularExpressions;
using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>
/// TB-TMAR-CUSTOMERPROFILE-AMSC-001-W3 — durable ARCH-COMPLETE-002 certification lock for CustomerProfile:
/// SoT/manifest records, canonical mechanisms, single stable-code owner, Host closure and evidence.
/// </summary>
public sealed class CustomerProfileModuleAmsc001W3CertGuardTests
{
    private const string ModuleRoot = "src/backend/Modules/CustomerProfile";

    [Fact]
    public void CustomerProfile_is_arch_complete_002_certified_in_sot_and_manifest()
    {
        var root = Repo();

        using var manifest = JsonDocument.Parse(File.ReadAllText(Path.Combine(
            root, "docs/architecture/tmar-module-structure-manifests.json")));
        var entries = manifest.RootElement.GetProperty("modules").EnumerateArray()
            .Where(m => m.GetProperty("module").GetString() == "CustomerProfile").ToArray();
        Assert.Single(entries);
        Assert.True(entries[0].GetProperty("structureCertified").GetBoolean());
        Assert.Equal("ARCH-COMPLETE-002", entries[0].GetProperty("lockVersion").GetString());
        Assert.Contains("TB-TMAR-CUSTOMERPROFILE-AMSC-001-W3", entries[0].GetProperty("certificationNote").GetString()!, StringComparison.Ordinal);
        Assert.Equal(5, entries[0].GetProperty("projects").GetArrayLength());
        Assert.DoesNotContain(
            manifest.RootElement.GetProperty("preCertModules").EnumerateArray(),
            m => m.GetProperty("module").GetString() == "CustomerProfile");

        using var sot = JsonDocument.Parse(File.ReadAllText(Path.Combine(
            root, "docs/architecture/tmar-current-state.json")));
        var w3 = sot.RootElement.GetProperty("customerProfileModuleAmsc001W3");
        Assert.Equal("COMPLETE_REFERENCE_PATTERN", w3.GetProperty("verdict").GetString());
        Assert.Equal("CERTIFIED", w3.GetProperty("structureState").GetString());
        Assert.Equal("ARCH-COMPLETE-002", w3.GetProperty("lockVersion").GetString());
        Assert.True(w3.GetProperty("structureCertified").GetBoolean());
        Assert.Equal("EXACT", w3.GetProperty("pathNamespaceState").GetString());
        Assert.Equal("ENFORCED", w3.GetProperty("rootAllowlistState").GetString());
        Assert.Equal("ZERO", w3.GetProperty("foreignAppInfraDomainCoupling").GetString());
        Assert.Equal("MODULE_OWNED", w3.GetProperty("endpointOwnershipState").GetString());
        Assert.Equal(3, w3.GetProperty("endpointReachableRequests").GetInt32());
        Assert.Equal("ZERO", w3.GetProperty("blockingResidualDebt").GetString());
        Assert.True(w3.GetProperty("microserviceExtractable").GetBoolean());
        Assert.Equal("NONE", w3.GetProperty("automaticNextImplementationTask").GetString());
        Assert.Equal("PRESERVED", w3.GetProperty("hostFinalClosureState").GetString());

        var certified = sot.RootElement.GetProperty("structureLock").GetProperty("certifiedModules")
            .EnumerateArray().Select(x => x.GetString()).ToArray();
        Assert.Equal(1, certified.Count(x => x == "CustomerProfile"));

        // Wave lineage is recorded with its own commit SHAs.
        Assert.Equal("39a5de09", sot.RootElement.GetProperty("customerProfileModuleAmsc001W0").GetProperty("commit").GetString());
        Assert.Equal("4599c97f", sot.RootElement.GetProperty("customerProfileModuleAmsc001W1").GetProperty("commit").GetString());
        Assert.Equal("4902fca6", sot.RootElement.GetProperty("customerProfileModuleAmsc001W2").GetProperty("commit").GetString());
    }

    [Fact]
    public void CustomerProfile_has_zero_hardcoded_fault_text_and_one_stable_code_owner()
    {
        var root = Repo();

        var production = Directory.GetFiles(Path.Combine(root, ModuleRoot), "*.cs", SearchOption.AllDirectories)
            .Where(p => !p.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                && !p.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                && !p.Contains($"{Path.DirectorySeparatorChar}Migrations{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .ToArray();

        foreach (var file in production)
        {
            var text = File.ReadAllText(file);
            // No raw string-literal fault payload; every fault carries a stable code.
            Assert.DoesNotMatch(new Regex("ContractOperationException\\(\"[^\"]+\"\\)"), text);
        }

        // The Domain consumes Contracts-owned codes but declares no code constant of its own.
        var domain = string.Join("\n", production
            .Where(p => p.Contains($"{Path.DirectorySeparatorChar}Tooba.CustomerProfile.Domain{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .Select(File.ReadAllText));
        Assert.DoesNotContain("public const string", domain, StringComparison.Ordinal);

        // Every declared stable code resolves to exactly one canonical descriptor owner.
        var codesFile = File.ReadAllText(Path.Combine(
            root, ModuleRoot, "Tooba.CustomerProfile.Contracts", "Errors", "CustomerProfileErrorCodes.cs"));
        var declared = Regex.Matches(codesFile, "public const string \\w+ = \"(?<c>[^\"]+)\"")
            .Select(m => m.Groups["c"].Value).OrderBy(x => x, StringComparer.Ordinal).ToArray();
        Assert.Equal(7, declared.Length);

        var contributor = File.ReadAllText(Path.Combine(
            root, ModuleRoot, "Tooba.CustomerProfile.Endpoints", "Errors", "CustomerProfileErrorCatalogContributor.cs"));
        var names = Regex.Matches(codesFile, "public const string (?<n>\\w+)")
            .Select(m => m.Groups["n"].Value).ToArray();
        var registered = Regex.Matches(contributor, "CustomerProfileErrorCodes\\.(?<n>\\w+)")
            .Select(m => m.Groups["n"].Value).ToHashSet(StringComparer.Ordinal);

        foreach (var name in names)
        {
            if (name == "SessionRequired")
            {
                // Foundation owns this shared cross-cutting descriptor; CustomerProfile consumes it.
                Assert.DoesNotContain(name, registered);
                continue;
            }

            Assert.Contains(name, registered);
        }

        // Every registered module code is localized in both resource sets.
        foreach (var culture in new[] { "CustomerProfileErrors.resx", "CustomerProfileErrors.fa.resx" })
        {
            var resx = File.ReadAllText(Path.Combine(
                root, ModuleRoot, "Tooba.CustomerProfile.Endpoints", "Resources", culture));
            foreach (var code in declared.Where(c => c.StartsWith("customer.profile.", StringComparison.Ordinal)))
            {
                Assert.Contains($"name=\"{code}\"", resx, StringComparison.Ordinal);
            }
        }
    }

    [Fact]
    public void CustomerProfile_endpoints_and_domain_boundaries_are_canonical()
    {
        var root = Repo();

        var endpoints = Path.Combine(root, ModuleRoot, "Tooba.CustomerProfile.Endpoints");
        var joined = string.Join("\n", Directory.GetFiles(endpoints, "*.cs", SearchOption.AllDirectories)
            .Where(p => !p.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .Select(File.ReadAllText));

        Assert.DoesNotContain("Tooba.CustomerProfile.Domain", joined, StringComparison.Ordinal);
        Assert.DoesNotContain("Results.Ok()", joined, StringComparison.Ordinal);
        Assert.DoesNotContain("Results.BadRequest(", joined, StringComparison.Ordinal);
        Assert.DoesNotContain("Results.Problem(", joined, StringComparison.Ordinal);
        Assert.Contains("ApiResponseFactory", joined, StringComparison.Ordinal);
        Assert.Contains("CustomerProfileErrorCodes.SessionRequired", joined, StringComparison.Ordinal);

        var csproj = File.ReadAllText(Path.Combine(endpoints, "Tooba.CustomerProfile.Endpoints.csproj"));
        Assert.DoesNotContain("Tooba.CustomerProfile.Infrastructure", csproj, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.CustomerProfile.Domain", csproj, StringComparison.Ordinal);

        var contracts = string.Join("\n", Directory.GetFiles(
                Path.Combine(root, ModuleRoot, "Tooba.CustomerProfile.Contracts"), "*.cs", SearchOption.AllDirectories)
            .Where(p => !p.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .Select(File.ReadAllText));
        Assert.DoesNotContain("Tooba.CustomerProfile.Domain", contracts, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.CustomerProfile.Application", contracts, StringComparison.Ordinal);
    }

    [Fact]
    public void CustomerProfile_host_closure_and_evidence_are_preserved()
    {
        var root = Repo();
        var hostCustomerProfile = Path.Combine(root, "src", "backend", "Host", "Tooba.Host", "CustomerProfile");
        Assert.True(!Directory.Exists(hostCustomerProfile)
            || Directory.GetFiles(hostCustomerProfile, "*.cs", SearchOption.AllDirectories).Length == 0);

        foreach (var wave in new[] { "W0", "W1", "W2", "W3" })
        {
            Assert.True(Directory.Exists(Path.Combine(
                root, "docs", "architecture", "evidence", $"TB-TMAR-CUSTOMERPROFILE-AMSC-001-{wave}")));
        }

        var w3Evidence = File.ReadAllText(Path.Combine(
            root, "docs", "architecture", "evidence", "TB-TMAR-CUSTOMERPROFILE-AMSC-001-W3", "certification.md"));
        Assert.Contains("ARCH-COMPLETE-002", w3Evidence, StringComparison.Ordinal);
        Assert.Contains("COMPLETE_REFERENCE_PATTERN", w3Evidence, StringComparison.Ordinal);

        // Repository-global Host root checkpoint must remain untouched by this module-local task.
        using var sot = JsonDocument.Parse(File.ReadAllText(Path.Combine(
            root, "docs/architecture/tmar-current-state.json")));
        Assert.Equal("TB-TMAR-HOST-ROOT-FINAL-CERT-001", sot.RootElement.GetProperty("lastAcceptedTask").GetString());
        Assert.Equal("NONE", sot.RootElement.GetProperty("automaticNextImplementationTask").GetString());

        // Master Recovery records the certification checkpoint.
        var recovery = File.ReadAllText(Path.Combine(root, "docs/architecture", "TOOBA-TMAR-MASTER-RECOVERY.md"));
        Assert.Contains("TB-TMAR-CUSTOMERPROFILE-AMSC-001-W3", recovery, StringComparison.Ordinal);
        Assert.Contains("USER_REVIEW_CUSTOMERPROFILE_AMSC_001_W3", recovery, StringComparison.Ordinal);
    }

    [Fact]
    public void CustomerProfile_amsc_w3r1_recovery_reconciliation_truth_is_locked()
    {
        var root = Repo();

        using var sot = JsonDocument.Parse(File.ReadAllText(Path.Combine(
            root, "docs/architecture/tmar-current-state.json")));

        // Additive R1 reconciliation record is present and honest.
        var w3r1 = sot.RootElement.GetProperty("customerProfileModuleAmsc001W3R1");
        Assert.Equal("CUSTOMERPROFILE_AMSC_001_RECOVERY_RECONCILED", w3r1.GetProperty("state").GetString());
        Assert.False(w3r1.GetProperty("productionCodeChanged").GetBoolean());
        Assert.Equal("2ca6822fa61443006d562c09d9210707589bacea", w3r1.GetProperty("certifiedCommit").GetString());
        Assert.Equal("RECORDED_2CA6822F", w3r1.GetProperty("masterRecoveryW3ShaState").GetString());
        Assert.Equal("PRESERVED_AND_SUPERSEDED_FOR_CURRENT_MODULE_RECOVERY",
            w3r1.GetProperty("historicalLineageState").GetString());
        Assert.Equal("PRESERVED", w3r1.GetProperty("globalHostCheckpointState").GetString());
        Assert.Equal("NONE", w3r1.GetProperty("automaticNextImplementationTask").GetString());

        // W3 architectural truth itself is preserved unchanged.
        var w3 = sot.RootElement.GetProperty("customerProfileModuleAmsc001W3");
        Assert.Equal("CUSTOMERPROFILE_AMSC_001_CERTIFIED", w3.GetProperty("state").GetString());
        Assert.Equal("COMPLETE_REFERENCE_PATTERN", w3.GetProperty("verdict").GetString());
        Assert.Equal("ARCH-COMPLETE-002", w3.GetProperty("lockVersion").GetString());
        Assert.Equal("CERTIFIED", w3.GetProperty("structureState").GetString());

        // Repository-global Host root checkpoint must remain untouched by this module-local task.
        Assert.Equal("TB-TMAR-HOST-ROOT-FINAL-CERT-001", sot.RootElement.GetProperty("lastAcceptedTask").GetString());
        Assert.Equal("NONE", sot.RootElement.GetProperty("automaticNextImplementationTask").GetString());

        // Master Recovery records the W3 final commit SHA and the R1 reconciliation.
        var recovery = File.ReadAllText(Path.Combine(root, "docs/architecture", "TOOBA-TMAR-MASTER-RECOVERY.md"));
        Assert.Contains("`TB-TMAR-CUSTOMERPROFILE-AMSC-001-W3` Certify `2ca6822f`", recovery, StringComparison.Ordinal);
        Assert.Contains("CustomerProfile AMSC W3-R1 recovery reconciliation", recovery, StringComparison.Ordinal);
        Assert.Contains("HISTORICAL / SUPERSEDED FOR CURRENT CUSTOMERPROFILE MODULE RECOVERY", recovery, StringComparison.Ordinal);
        Assert.Contains("USER_REVIEW_CUSTOMERPROFILE_AMSC_001_W3_R1", recovery, StringComparison.Ordinal);
        Assert.Contains("TB-TMAR-HOST-CUSTOMERPROFILE-EVACUATION-001", recovery, StringComparison.Ordinal);
        Assert.Contains("TB-TMAR-HOST-CUSTOMER-FULL-CLOSURE-001-R1", recovery, StringComparison.Ordinal);
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
