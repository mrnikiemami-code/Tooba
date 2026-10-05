using System.Text.Json;
using System.Text.RegularExpressions;
using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>
/// TB-TMAR-CONTENT-AMSC-001-W3 — durable ARCH-COMPLETE-002 certification lock:
/// SoT/manifest records, canonical mechanisms, single error-code owner, Host closure and evidence.
/// </summary>
public sealed class ContentModuleAmsc001W3CertGuardTests
{
    private const string ModuleRoot = "src/backend/Modules/Content";

    [Fact]
    public void Content_is_arch_complete_002_certified_in_sot_and_manifest()
    {
        var root = Repo();

        using var manifest = JsonDocument.Parse(File.ReadAllText(Path.Combine(
            root, "docs/architecture/tmar-module-structure-manifests.json")));
        var entries = manifest.RootElement.GetProperty("modules").EnumerateArray()
            .Where(m => m.GetProperty("module").GetString() == "Content").ToArray();
        Assert.Single(entries);
        Assert.True(entries[0].GetProperty("structureCertified").GetBoolean());
        Assert.Equal("ARCH-COMPLETE-002", entries[0].GetProperty("lockVersion").GetString());
        Assert.Contains("TB-TMAR-CONTENT-AMSC-001", entries[0].GetProperty("certificationNote").GetString(), StringComparison.Ordinal);

        using var sot = JsonDocument.Parse(File.ReadAllText(Path.Combine(
            root, "docs/architecture/tmar-current-state.json")));
        var w3 = sot.RootElement.GetProperty("contentModuleAmsc001W3");
        Assert.Equal("COMPLETE_REFERENCE_PATTERN", w3.GetProperty("verdict").GetString());
        Assert.Equal("CERTIFIED", w3.GetProperty("structureState").GetString());
        Assert.Equal("ARCH-COMPLETE-002", w3.GetProperty("lockVersion").GetString());
        Assert.Equal("EXACT", w3.GetProperty("pathNamespaceState").GetString());
        Assert.Equal("ENFORCED", w3.GetProperty("rootAllowlistState").GetString());
        Assert.Equal("ZERO", w3.GetProperty("foreignAppInfraDomainCoupling").GetString());
        Assert.Equal("MODULE_OWNED", w3.GetProperty("endpointOwnershipState").GetString());
        Assert.Equal(51, w3.GetProperty("endpointReachableRequests").GetInt32());
        Assert.Equal("ZERO", w3.GetProperty("blockingResidualDebt").GetString());
        Assert.True(w3.GetProperty("microserviceExtractable").GetBoolean());
        Assert.Equal("NONE", w3.GetProperty("automaticNextImplementationTask").GetString());
        Assert.Equal("PRESERVED", w3.GetProperty("hostFinalClosureState").GetString());

        var certified = sot.RootElement.GetProperty("structureLock").GetProperty("certifiedModules")
            .EnumerateArray().Select(x => x.GetString()).ToArray();
        Assert.Equal(1, certified.Count(x => x == "Content"));

        // Wave lineage is recorded with its own commit SHAs.
        Assert.Equal("702537be", sot.RootElement.GetProperty("contentModuleAmsc001W0").GetProperty("commit").GetString());
        Assert.Equal("deb13ae8", sot.RootElement.GetProperty("contentModuleAmsc001W1").GetProperty("commit").GetString());
        Assert.Equal("ce7b3938", sot.RootElement.GetProperty("contentModuleAmsc001W2").GetProperty("commit").GetString());
    }

    [Fact]
    public void Content_has_zero_hardcoded_fault_text_and_one_stable_code_owner()
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
            Assert.DoesNotMatch(new Regex("ContractOperationException\\(\"[^\"]+\"\\)"), text);
        }

        var domain = string.Join("\n", production
            .Where(p => p.Contains($"{Path.DirectorySeparatorChar}Tooba.Content.Domain{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .Select(File.ReadAllText));
        foreach (var legacy in new[]
                 {
                     "ContentArticleErrorCodes", "ContentAuthorErrorCodes", "ContentCategoryErrorCodes",
                     "ContentTagErrorCodes", "ArticleCommentCodes",
                 })
        {
            Assert.DoesNotContain(legacy, domain, StringComparison.Ordinal);
        }

        // Every declared stable code resolves to exactly one canonical descriptor owner.
        var codesFile = File.ReadAllText(Path.Combine(
            root, ModuleRoot, "Tooba.Content.Contracts", "Errors", "ContentErrorCodes.cs"));
        var declared = Regex.Matches(codesFile, "public const string \\w+ = \"(?<c>[^\"]+)\"")
            .Select(m => m.Groups["c"].Value).OrderBy(x => x, StringComparer.Ordinal).ToArray();

        var contributor = File.ReadAllText(Path.Combine(
            root, ModuleRoot, "Tooba.Content.Endpoints", "Errors", "ContentErrorCatalogContributor.cs"));
        var names = Regex.Matches(codesFile, "public const string (?<n>\\w+)")
            .Select(m => m.Groups["n"].Value).ToArray();
        var registered = Regex.Matches(contributor, "ContentErrorCodes\\.(?<n>\\w+)")
            .Select(m => m.Groups["n"].Value).ToHashSet(StringComparer.Ordinal);

        Assert.Equal(76, declared.Length);
        foreach (var name in names)
        {
            Assert.Contains(name, registered);
        }

        // Every code is localized in both resource sets.
        foreach (var culture in new[] { "ContentErrors.resx", "ContentErrors.fa.resx" })
        {
            var resx = File.ReadAllText(Path.Combine(
                root, ModuleRoot, "Tooba.Content.Endpoints", "Resources", culture));
            foreach (var code in declared)
            {
                Assert.Contains($"name=\"{code}\"", resx, StringComparison.Ordinal);
            }
        }
    }

    [Fact]
    public void Content_endpoints_and_domain_boundaries_are_canonical()
    {
        var root = Repo();

        var endpoints = Path.Combine(root, ModuleRoot, "Tooba.Content.Endpoints");
        var joined = string.Join("\n", Directory.GetFiles(endpoints, "*.cs", SearchOption.AllDirectories)
            .Where(p => !p.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .Select(File.ReadAllText));

        Assert.DoesNotContain("Tooba.Content.Domain", joined, StringComparison.Ordinal);
        Assert.DoesNotContain("Results.Ok()", joined, StringComparison.Ordinal);
        Assert.DoesNotContain("Results.BadRequest(", joined, StringComparison.Ordinal);
        Assert.DoesNotContain("Results.Problem(", joined, StringComparison.Ordinal);
        Assert.Contains("ApiResponseFactory", joined, StringComparison.Ordinal);

        var csproj = File.ReadAllText(Path.Combine(endpoints, "Tooba.Content.Endpoints.csproj"));
        Assert.DoesNotContain("Tooba.Content.Infrastructure", csproj, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.Content.Domain", csproj, StringComparison.Ordinal);

        var contracts = string.Join("\n", Directory.GetFiles(
                Path.Combine(root, ModuleRoot, "Tooba.Content.Contracts"), "*.cs", SearchOption.AllDirectories)
            .Where(p => !p.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .Select(File.ReadAllText));
        Assert.DoesNotContain("Tooba.Content.Domain", contracts, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.Content.Application", contracts, StringComparison.Ordinal);
    }

    [Fact]
    public void Content_host_closure_and_evidence_are_preserved()
    {
        var root = Repo();
        var hostContent = Path.Combine(root, "src", "backend", "Host", "Tooba.Host", "Content");
        Assert.True(!Directory.Exists(hostContent)
            || Directory.GetFiles(hostContent, "*.cs", SearchOption.AllDirectories).Length == 0);

        foreach (var wave in new[] { "W0", "W1", "W2", "W3" })
        {
            Assert.True(Directory.Exists(Path.Combine(
                root, "docs", "architecture", "evidence", $"TB-TMAR-CONTENT-AMSC-001-{wave}")));
        }

        var w3Evidence = File.ReadAllText(Path.Combine(
            root, "docs", "architecture", "evidence", "TB-TMAR-CONTENT-AMSC-001-W3", "certification.md"));
        Assert.Contains("ARCH-COMPLETE-002", w3Evidence, StringComparison.Ordinal);
        Assert.Contains("COMPLETE_REFERENCE_PATTERN", w3Evidence, StringComparison.Ordinal);
    }

    [Fact]
    public void Content_amsc_w3r1_recovery_reconciliation_truth_is_locked()
    {
        var root = Repo();

        using var sot = JsonDocument.Parse(File.ReadAllText(Path.Combine(
            root, "docs/architecture/tmar-current-state.json")));

        // Additive R1 reconciliation record is present and honest.
        var w3r1 = sot.RootElement.GetProperty("contentModuleAmsc001W3R1");
        Assert.Equal("CONTENT_AMSC_001_RECOVERY_RECONCILED", w3r1.GetProperty("state").GetString());
        Assert.False(w3r1.GetProperty("productionCodeChanged").GetBoolean());
        Assert.Equal("c012345d866fec6ecb32ca6f3ff4aef616d877ae", w3r1.GetProperty("certifiedCommit").GetString());
        Assert.Equal("RECORDED_C012345D", w3r1.GetProperty("masterRecoveryW3ShaState").GetString());
        Assert.Equal("PRESERVED_AND_SUPERSEDED_FOR_CURRENT_MODULE_RECOVERY",
            w3r1.GetProperty("historicalLineageState").GetString());
        Assert.Equal("PRESERVED", w3r1.GetProperty("globalHostCheckpointState").GetString());
        Assert.Equal("NONE", w3r1.GetProperty("automaticNextImplementationTask").GetString());

        // Historical Content lineage stays present and is explicitly marked historical.
        var historical = sot.RootElement.GetProperty("hostContentAmcR4");
        Assert.Equal("HISTORICAL / SUPERSEDED FOR CURRENT CONTENT MODULE RECOVERY",
            historical.GetProperty("historicalLineageAuthority").GetString());

        // W3 architectural truth itself is preserved unchanged.
        Assert.Equal("COMPLETE_REFERENCE_PATTERN",
            sot.RootElement.GetProperty("contentModuleAmsc001W3").GetProperty("verdict").GetString());
        Assert.Equal("ARCH-COMPLETE-002",
            sot.RootElement.GetProperty("contentModuleAmsc001W3").GetProperty("lockVersion").GetString());

        // Repository-global Host root checkpoint must remain untouched by this module-local task.
        Assert.Equal("TB-TMAR-HOST-ROOT-FINAL-CERT-001", sot.RootElement.GetProperty("lastAcceptedTask").GetString());
        Assert.Equal("HOST_ROOT_FINAL_CERTIFIED", sot.RootElement.GetProperty("currentHostCheckpoint").GetString());
        Assert.Equal("NONE", sot.RootElement.GetProperty("automaticNextImplementationTask").GetString());

        // Master Recovery records the W3 final commit SHA and the R1 reconciliation.
        var recovery = File.ReadAllText(Path.Combine(root, "docs/architecture/TOOBA-TMAR-MASTER-RECOVERY.md"));
        Assert.Contains("`TB-TMAR-CONTENT-AMSC-001-W3` Certify `c012345d`", recovery, StringComparison.Ordinal);
        Assert.Contains("Content AMSC W3-R1 recovery reconciliation", recovery, StringComparison.Ordinal);
        Assert.Contains("HISTORICAL / SUPERSEDED FOR CURRENT CONTENT MODULE RECOVERY", recovery, StringComparison.Ordinal);
        Assert.Contains("USER_REVIEW_CONTENT_AMSC_001_W3_R1", recovery, StringComparison.Ordinal);
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
