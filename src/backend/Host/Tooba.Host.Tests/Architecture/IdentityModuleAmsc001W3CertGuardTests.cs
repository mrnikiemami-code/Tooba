using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Tooba.BuildingBlocks;
using Tooba.Identity.Application.Auth.Commands;
using Tooba.Identity.Application.Auth.Validators;
using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>
/// TB-TMAR-IDENTITY-AMSC-001-W3 — durable ARCH-COMPLETE-002 certification lock for Identity:
/// SoT/manifest records, single stable-code owner, both-culture resource sets, canonical
/// endpoints/Domain/Contracts boundaries, the single typed-fault operation seam, exhaustive
/// validator coverage, the Host auth-platform boundary and the AMSC evidence tree.
/// </summary>
public sealed class IdentityModuleAmsc001W3CertGuardTests
{
    private const string ModuleRoot = "src/backend/Modules/Identity";

    [Fact]
    public void Identity_is_arch_complete_002_certified_in_sot_and_manifest()
    {
        var root = Repo();

        using var manifest = JsonDocument.Parse(File.ReadAllText(Path.Combine(
            root, "docs/architecture/tmar-module-structure-manifests.json")));
        var entries = manifest.RootElement.GetProperty("modules").EnumerateArray()
            .Where(m => m.GetProperty("module").GetString() == "Identity").ToArray();
        Assert.Single(entries);
        Assert.True(entries[0].GetProperty("structureCertified").GetBoolean());
        Assert.Equal("ARCH-COMPLETE-002", entries[0].GetProperty("lockVersion").GetString());
        Assert.Contains(
            "TB-TMAR-IDENTITY-AMSC-001-W3",
            entries[0].GetProperty("certificationNote").GetString()!,
            StringComparison.Ordinal);
        Assert.Equal(5, entries[0].GetProperty("projects").GetArrayLength());
        Assert.DoesNotContain(
            manifest.RootElement.GetProperty("preCertModules").EnumerateArray(),
            m => m.GetProperty("module").GetString() == "Identity");
        Assert.DoesNotContain(
            "Identity",
            manifest.RootElement.GetProperty("uncertifiedHttpOwningModules").EnumerateArray()
                .Select(x => x.GetString()!),
            StringComparer.Ordinal);

        using var sot = JsonDocument.Parse(File.ReadAllText(Path.Combine(
            root, "docs/architecture/tmar-current-state.json")));

        var w3 = sot.RootElement.GetProperty("identityModuleAmsc001W3");
        Assert.Equal("COMPLETE_REFERENCE_PATTERN", w3.GetProperty("verdict").GetString());
        Assert.Equal("CERTIFIED", w3.GetProperty("structureState").GetString());
        Assert.Equal("ARCH-COMPLETE-002", w3.GetProperty("lockVersion").GetString());
        Assert.True(w3.GetProperty("structureCertified").GetBoolean());
        Assert.Equal("EXACT", w3.GetProperty("pathNamespaceState").GetString());
        Assert.Equal("ENFORCED", w3.GetProperty("rootAllowlistState").GetString());
        Assert.Equal("ZERO", w3.GetProperty("foreignAppInfraDomainCoupling").GetString());
        Assert.Equal("MODULE_OWNED", w3.GetProperty("endpointOwnershipState").GetString());
        Assert.Equal(13, w3.GetProperty("endpointReachableRequests").GetInt32());
        Assert.Equal("ZERO", w3.GetProperty("blockingResidualDebt").GetString());
        Assert.True(w3.GetProperty("microserviceExtractable").GetBoolean());
        Assert.Equal("NONE", w3.GetProperty("automaticNextImplementationTask").GetString());
        Assert.Equal("PRESERVED", w3.GetProperty("hostFinalClosureState").GetString());
        Assert.Equal("/Modules/Identity/", w3.GetProperty("solutionFolder").GetString());
        Assert.Equal(5, w3.GetProperty("solutionProjectEntries").GetInt32());
        Assert.Equal(12, w3.GetProperty("declaredCodeCount").GetInt32());
        Assert.Equal(12, w3.GetProperty("registeredDescriptorCount").GetInt32());

        // The pre-existing AMC-001 record is HISTORICAL: TB-TMAR-IDENTITY-AMSC-001-W3-R1 restored its
        // truthful pre-W3 values (W3 had rewritten them with current AMSC truth). Current Identity
        // authority lives only in the identityModuleAmsc001W0..W3 records asserted above.
        var amc = sot.RootElement.GetProperty("identityAmc001");
        Assert.Equal("TB-TMAR-IDENTITY-AMC-001", amc.GetProperty("task").GetString());
        Assert.Equal("COMPLETE_REFERENCE_PATTERN", amc.GetProperty("state").GetString());
        Assert.Equal("READY_FOR_CERTIFY", amc.GetProperty("structureState").GetString());
        Assert.Equal(
            "COMPLETE_6_OF_6_REQUIRED_PRESENT_7_NO_VALIDATOR_REQUIRED",
            amc.GetProperty("validatorCoverage").GetString());
        Assert.Equal(6, amc.GetProperty("validatorRequiredCount").GetInt32());
        Assert.Equal(7, amc.GetProperty("noValidatorRequiredCount").GetInt32());
        Assert.Equal("ZERO", amc.GetProperty("foreignAppInfraDomainCoupling").GetString());
        Assert.Equal("MODULE_ENDPOINTS", amc.GetProperty("endpointOwnership").GetString());
        foreach (var currentAmcField in new[]
                 {
                     "amsc001Certified", "amsc001CertificationNote", "amsc001EvidenceRoot", "amsc001StopGate",
                 })
        {
            Assert.False(amc.TryGetProperty(currentAmcField, out _),
                $"historical identityAmc001 must not carry the current AMSC field '{currentAmcField}'");
        }

        // The additive W3-R1 recovery reconciliation record locks the restored historical truth.
        var r1 = sot.RootElement.GetProperty("identityModuleAmsc001W3R1");
        Assert.Equal("TB-TMAR-IDENTITY-AMSC-001-W3-R1", r1.GetProperty("task").GetString());
        Assert.Equal("TB-TMAR-IDENTITY-AMSC-001-W3", r1.GetProperty("parentTask").GetString());
        Assert.Equal("RECOVERY_SOT_RECONCILIATION_ONLY", r1.GetProperty("mode").GetString());
        Assert.Equal("IDENTITY_AMSC_001_RECOVERY_RECONCILED", r1.GetProperty("state").GetString());
        Assert.False(r1.GetProperty("productionCodeChanged").GetBoolean());
        Assert.Equal("e6d467740dc0305c665d72712fbc5f115ba4b4bf", r1.GetProperty("certifiedCommit").GetString());
        Assert.Equal("RESTORED_PRE_W3_TRUTH", r1.GetProperty("historicalAmcState").GetString());
        Assert.Equal(
            "PRESERVED_AND_SUPERSEDED_FOR_CURRENT_MODULE_RECOVERY",
            r1.GetProperty("historicalLineageState").GetString());
        Assert.Equal("RECORDED_E6D46774", r1.GetProperty("masterRecoveryW3ShaState").GetString());
        Assert.Equal("IDENTITY_AMSC_001_W0_TO_W3", r1.GetProperty("currentAuthority").GetString());
        Assert.Equal("PRESERVED", r1.GetProperty("globalHostCheckpointState").GetString());
        Assert.Equal("USER_REVIEW_IDENTITY_AMSC_001_W3_R1", r1.GetProperty("workflowStop").GetString());
        Assert.Equal("NONE", r1.GetProperty("automaticNextImplementationTask").GetString());

        var certified = sot.RootElement.GetProperty("structureLock").GetProperty("certifiedModules")
            .EnumerateArray().Select(x => x.GetString()).ToArray();
        Assert.Equal(1, certified.Count(x => x == "Identity"));

        // Wave lineage is recorded with its own commit SHAs.
        Assert.Equal("91eec1fd", sot.RootElement.GetProperty("identityModuleAmsc001W0").GetProperty("commit").GetString());
        Assert.Equal("93a6b192", sot.RootElement.GetProperty("identityModuleAmsc001W1").GetProperty("commit").GetString());
        Assert.Equal("7c79f8c6", sot.RootElement.GetProperty("identityModuleAmsc001W2").GetProperty("commit").GetString());
        Assert.Equal("7c79f8c6", sot.RootElement.GetProperty("identityModuleAmsc001W3").GetProperty("startingHead").GetString());
    }

    [Fact]
    public void Identity_has_one_stable_code_owner_and_both_culture_resource_sets()
    {
        var root = Repo();

        var production = Directory.GetFiles(Path.Combine(root, ModuleRoot), "*.cs", SearchOption.AllDirectories)
            .Where(p => !p.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                && !p.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                && !p.Contains($"{Path.DirectorySeparatorChar}Migrations{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .ToArray();

        foreach (var file in production)
        {
            // No raw string-literal fault payload; every Identity fault carries a stable code constant.
            Assert.DoesNotMatch(new Regex("ContractOperationException\\(\"[^\"]+\"\\)"), File.ReadAllText(file));
        }

        // The Domain consumes Contracts-owned codes but declares no code constant of its own.
        var domain = string.Join("\n", production
            .Where(p => p.Contains($"{Path.DirectorySeparatorChar}Tooba.Identity.Domain{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .Select(File.ReadAllText));
        Assert.DoesNotContain("public const string", domain, StringComparison.Ordinal);

        var codesFile = File.ReadAllText(Path.Combine(
            root, ModuleRoot, "Tooba.Identity.Contracts", "Errors", "IdentityErrorCodes.cs"));
        var declared = Regex.Matches(codesFile, "public const string \\w+ = \"(?<c>[^\"]+)\"")
            .Select(m => m.Groups["c"].Value).OrderBy(x => x, StringComparer.Ordinal).ToArray();
        Assert.Equal(12, declared.Length);

        // Every declared code resolves to exactly one canonical descriptor owner.
        var contributor = File.ReadAllText(Path.Combine(
            root, ModuleRoot, "Tooba.Identity.Contracts", "Errors", "IdentityErrorCatalogContributor.cs"));
        var names = Regex.Matches(codesFile, "public const string (?<n>\\w+)")
            .Select(m => m.Groups["n"].Value).ToArray();
        var registered = Regex.Matches(contributor, "IdentityErrorCodes\\.(?<n>\\w+)")
            .Select(m => m.Groups["n"].Value).ToHashSet(StringComparer.Ordinal);
        Assert.Equal(names.Length, registered.Count);
        foreach (var name in names)
        {
            Assert.Contains(name, registered);
        }

        // The Persian resource set covers every declared code; the English set is the pre-existing
        // catalogued subset (adding the three OTP-delivery keys there would change the English title).
        var faKeys = ResxKeys(Path.Combine(root, ModuleRoot, "Tooba.Identity.Contracts", "Resources", "IdentityErrors.fa.resx"));
        foreach (var code in declared)
        {
            Assert.Contains(code, faKeys);
        }

        var enKeys = ResxKeys(Path.Combine(root, ModuleRoot, "Tooba.Identity.Contracts", "Resources", "IdentityErrors.resx"));
        Assert.Contains("identity.validation.failed", enKeys);
        Assert.Contains("identity.authentication.failed", enKeys);
        Assert.Contains("identity.password.change.failed", enKeys);
        Assert.True(enKeys.IsSubsetOf(faKeys), "the Persian set must cover the English catalogued set");
    }

    [Fact]
    public void Identity_endpoints_domain_and_contracts_boundaries_are_canonical()
    {
        var root = Repo();

        var endpoints = Path.Combine(root, ModuleRoot, "Tooba.Identity.Endpoints");
        var endpointSource = string.Join("\n", Directory.GetFiles(endpoints, "*.cs", SearchOption.AllDirectories)
            .Where(p => !p.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                && !p.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .Select(File.ReadAllText));

        Assert.DoesNotContain("Tooba.Identity.Domain", endpointSource, StringComparison.Ordinal);
        Assert.DoesNotContain("Results.BadRequest(", endpointSource, StringComparison.Ordinal);
        Assert.DoesNotContain("Results.Problem(", endpointSource, StringComparison.Ordinal);
        Assert.Contains("ApiResponseFactory", endpointSource, StringComparison.Ordinal);
        Assert.Contains("ISender", endpointSource, StringComparison.Ordinal);

        var endpointsCsproj = File.ReadAllText(Path.Combine(endpoints, "Tooba.Identity.Endpoints.csproj"));
        Assert.DoesNotContain("Tooba.Identity.Infrastructure", endpointsCsproj, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.Identity.Domain", endpointsCsproj, StringComparison.Ordinal);

        var contracts = string.Join("\n", Directory.GetFiles(
                Path.Combine(root, ModuleRoot, "Tooba.Identity.Contracts"), "*.cs", SearchOption.AllDirectories)
            .Where(p => !p.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                && !p.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .Select(File.ReadAllText));
        Assert.DoesNotContain("Tooba.Identity.Domain", contracts, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.Identity.Application", contracts, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.Identity.Infrastructure", contracts, StringComparison.Ordinal);

        // The Domain keeps only the legitimate SELF-MODULE Contracts reference for stable codes.
        var domainCsproj = File.ReadAllText(Path.Combine(
            root, ModuleRoot, "Tooba.Identity.Domain", "Tooba.Identity.Domain.csproj"));
        Assert.Contains("Tooba.Identity.Contracts.csproj", domainCsproj, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.Identity.Application.csproj", domainCsproj, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.Identity.Infrastructure.csproj", domainCsproj, StringComparison.Ordinal);

        // Microservice extraction: no Identity project may reference a foreign module's Application,
        // Domain or Infrastructure assembly (only Tooba.BuildingBlocks + self-module projects).
        var foreignEdges = new List<string>();
        foreach (var csproj in Directory.GetFiles(Path.Combine(root, ModuleRoot), "*.csproj", SearchOption.AllDirectories))
        {
            foreach (var reference in Regex.Matches(csproj.Replace('/', Path.DirectorySeparatorChar), @"Include=""(?<p>[^""]+\.csproj)""")
                         .Select(m => m.Groups["p"].Value))
            {
                var fileName = Path.GetFileName(reference);
                if (fileName.StartsWith("Tooba.Identity.", StringComparison.Ordinal)
                    || fileName == "Tooba.BuildingBlocks.csproj")
                {
                    continue;
                }

                if (fileName.EndsWith(".Application.csproj", StringComparison.Ordinal)
                    || fileName.EndsWith(".Domain.csproj", StringComparison.Ordinal)
                    || fileName.EndsWith(".Infrastructure.csproj", StringComparison.Ordinal))
                {
                    foreignEdges.Add($"{Path.GetFileName(csproj)} -> {fileName}");
                }
            }
        }

        Assert.True(foreignEdges.Count == 0, $"foreign App/Domain/Infrastructure edges: {string.Join(", ", foreignEdges)}");
    }

    [Fact]
    public void Identity_operation_seam_and_validator_coverage_are_locked()
    {
        var root = Repo();

        var seam = File.ReadAllText(Path.Combine(
            root, ModuleRoot, "Tooba.Identity.Application", "Composition", "IdentityOperation.cs"));
        Assert.Contains("catch (ContractOperationException ex)", seam, StringComparison.Ordinal);
        Assert.DoesNotContain("catch (InvalidOperationException)", seam, StringComparison.Ordinal);
        Assert.DoesNotContain("catch (ArgumentException)", seam, StringComparison.Ordinal);
        Assert.Contains("new SemanticError(ex.Code)", seam, StringComparison.Ordinal);

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddToobaCqrsFoundation(typeof(RegisterAuthUserCommand).Assembly);
        using var sp = services.BuildServiceProvider();

        Assert.IsType<LoginWithPasswordCommandValidator>(sp.GetService<IValidator<LoginWithPasswordCommand>>());
        Assert.IsType<RefreshAuthSessionCommandValidator>(sp.GetService<IValidator<RefreshAuthSessionCommand>>());
        Assert.IsType<CompleteOtpLoginCommandValidator>(sp.GetService<IValidator<CompleteOtpLoginCommand>>());

        // The 13 endpoint-reachable requests are exactly 9 VALIDATOR_REQUIRED + 4 NO_VALIDATOR_REQUIRED.
        var endpoints = File.ReadAllText(Path.Combine(
            root, ModuleRoot, "Tooba.Identity.Endpoints", "Auth", "IdentityAuthEndpoints.cs"));
        Assert.Equal(13, Regex.Matches(endpoints, @"group\.Map(?:Get|Post|Put|Delete|Patch)\(").Count);

        var w2 = ReadSot(root).RootElement.GetProperty("identityModuleAmsc001W2");
        Assert.Equal(
            "EXHAUSTIVE_9_REQUIRED_PRESENT_4_NO_VALIDATOR_REQUIRED",
            w2.GetProperty("validatorCoverageState").GetString());
        Assert.Equal(9, w2.GetProperty("validatorRequiredCount").GetInt32());
        Assert.Equal(4, w2.GetProperty("noValidatorRequiredCount").GetInt32());
    }

    [Fact]
    public void Identity_host_platform_boundary_and_evidence_are_preserved()
    {
        var root = Repo();

        // Identity owns no Host folder and no Tooba.Host.Identity namespace; the Host keeps only the
        // generic platform auth seam (middleware / session / throttle) that Identity consumes by contract.
        Assert.False(Directory.Exists(Path.Combine(root, "src", "backend", "Host", "Tooba.Host", "Identity")));
        foreach (var file in Directory.GetFiles(Path.Combine(root, "src", "backend", "Host", "Tooba.Host"), "*.cs", SearchOption.AllDirectories))
        {
            if (file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                || file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            {
                continue;
            }

            Assert.DoesNotContain("namespace Tooba.Host.Identity", File.ReadAllText(file), StringComparison.Ordinal);
        }

        foreach (var wave in new[] { "W0", "W1", "W2", "W3" })
        {
            Assert.True(Directory.Exists(Path.Combine(
                root, "docs", "architecture", "evidence", $"TB-TMAR-IDENTITY-AMSC-001-{wave}")));
        }

        var w3Evidence = File.ReadAllText(Path.Combine(
            root, "docs", "architecture", "evidence", "TB-TMAR-IDENTITY-AMSC-001-W3", "certification.md"));
        Assert.Contains("ARCH-COMPLETE-002", w3Evidence, StringComparison.Ordinal);
        Assert.Contains("COMPLETE_REFERENCE_PATTERN", w3Evidence, StringComparison.Ordinal);

        // Repository-global Host root checkpoint must remain untouched by this module-local task.
        var sot = ReadSot(root).RootElement;
        Assert.Equal("TB-TMAR-HOST-ROOT-FINAL-CERT-001", sot.GetProperty("lastAcceptedTask").GetString());
        Assert.Equal("NONE", sot.GetProperty("automaticNextImplementationTask").GetString());

        // Master Recovery records the module certification checkpoint and its stop gate.
        var recovery = File.ReadAllText(Path.Combine(root, "docs/architecture", "TOOBA-TMAR-MASTER-RECOVERY.md"));
        Assert.Contains("TB-TMAR-IDENTITY-AMSC-001-W3", recovery, StringComparison.Ordinal);
        Assert.Contains("USER_REVIEW_IDENTITY_AMSC_001_W3", recovery, StringComparison.Ordinal);

        // The final W3 SHA is recorded explicitly and the historical lineage is marked superseded.
        Assert.Contains("`TB-TMAR-IDENTITY-AMSC-001-W3` Certify `e6d46774`", recovery, StringComparison.Ordinal);
        Assert.Contains(
            "HISTORICAL / SUPERSEDED FOR CURRENT IDENTITY MODULE RECOVERY", recovery, StringComparison.Ordinal);
        Assert.Contains("USER_REVIEW_IDENTITY_AMSC_001_W3_R1", recovery, StringComparison.Ordinal);
    }

    private static HashSet<string> ResxKeys(string path) =>
        XDocument.Load(path).Root!.Elements("data")
            .Select(d => (string?)d.Attribute("name") ?? string.Empty)
            .ToHashSet(StringComparer.Ordinal);

    private static JsonDocument ReadSot(string root) =>
        JsonDocument.Parse(File.ReadAllText(Path.Combine(root, "docs/architecture/tmar-current-state.json")));

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
