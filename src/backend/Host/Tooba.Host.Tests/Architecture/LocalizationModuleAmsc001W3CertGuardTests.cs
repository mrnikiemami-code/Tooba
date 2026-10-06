using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>
/// TB-TMAR-LOCALIZATION-AMSC-001-W3 — durable ARCH-COMPLETE-002 certification lock for Localization:
/// SoT/manifest certification records with the AMSC wave lineage, the single stable-code owner with
/// bilingual resource coverage, the typed-fault composition seam, module-owned HTTP surface with
/// Contracts-only boundaries and the AMSC evidence tree.
/// </summary>
public sealed class LocalizationModuleAmsc001W3CertGuardTests
{
    private const string ModuleRoot = "src/backend/Modules/Localization";

    private static readonly string[] ProductionProjects =
    [
        "Tooba.Localization.Application",
        "Tooba.Localization.Contracts",
        "Tooba.Localization.Domain",
        "Tooba.Localization.Endpoints",
        "Tooba.Localization.Infrastructure",
    ];

    [Fact]
    public void Localization_is_arch_complete_002_certified_in_sot_and_manifest()
    {
        var root = Repo();

        using var manifest = JsonDocument.Parse(File.ReadAllText(Path.Combine(
            root, "docs/architecture/tmar-module-structure-manifests.json")));
        var entries = manifest.RootElement.GetProperty("modules").EnumerateArray()
            .Where(m => string.Equals(m.GetProperty("module").GetString(), "Localization", StringComparison.Ordinal))
            .ToArray();
        Assert.Single(entries);
        Assert.True(entries[0].GetProperty("structureCertified").GetBoolean());
        Assert.Equal("ARCH-COMPLETE-002", entries[0].GetProperty("lockVersion").GetString());
        Assert.Equal(5, entries[0].GetProperty("projects").GetArrayLength());
        Assert.DoesNotContain(
            manifest.RootElement.GetProperty("preCertModules").EnumerateArray(),
            m => string.Equals(m.GetProperty("module").GetString(), "Localization", StringComparison.Ordinal));

        using var sot = JsonDocument.Parse(File.ReadAllText(Path.Combine(
            root, "docs/architecture/tmar-current-state.json")));
        var w3 = sot.RootElement.GetProperty("localizationModuleAmsc001W3");
        Assert.Equal("LOCALIZATION_AMSC_001_CERTIFIED", w3.GetProperty("state").GetString());
        Assert.Equal("COMPLETE_REFERENCE_PATTERN", w3.GetProperty("verdict").GetString());
        Assert.Equal("ARCH-COMPLETE-002", w3.GetProperty("lockVersion").GetString());
        Assert.Equal("CERTIFIED", w3.GetProperty("structureState").GetString());
        Assert.True(w3.GetProperty("structureCertified").GetBoolean());
        Assert.Equal("EXACT", w3.GetProperty("pathNamespaceState").GetString());
        Assert.Equal("ENFORCED", w3.GetProperty("rootAllowlistState").GetString());
        Assert.Equal("ZERO", w3.GetProperty("foreignAppInfraDomainCoupling").GetString());
        Assert.Equal("HTTP_OWNING", w3.GetProperty("httpApplicability").GetString());
        Assert.Equal("MODULE_ENDPOINTS", w3.GetProperty("endpointOwnershipState").GetString());
        Assert.Equal("ZERO", w3.GetProperty("hostHttpOwnership").GetString());
        Assert.Equal(4, w3.GetProperty("endpointReachableRequests").GetInt32());
        Assert.Equal(4, w3.GetProperty("routeCount").GetInt32());
        Assert.Equal("ZERO", w3.GetProperty("blockingResidualDebt").GetString());
        Assert.True(w3.GetProperty("microserviceExtractable").GetBoolean());
        Assert.Equal("NONE", w3.GetProperty("automaticNextImplementationTask").GetString());
        Assert.Equal("USER_REVIEW_LOCALIZATION_AMSC_001_W3", w3.GetProperty("stopGate").GetString());
        Assert.Equal("PRESERVED", w3.GetProperty("hostFinalClosureState").GetString());
        Assert.Equal("ZERO", w3.GetProperty("sinkFolderRegressionState").GetString());
        Assert.Equal("NONE", w3.GetProperty("sensitiveLoggingState").GetString());
        Assert.Equal("CONTRACTS_ONLY", w3.GetProperty("crossModuleBoundaryState").GetString());

        var certified = sot.RootElement.GetProperty("structureLock").GetProperty("certifiedModules")
            .EnumerateArray().Select(x => x.GetString()).ToArray();
        Assert.Equal(1, certified.Count(x => x == "Localization"));

        // Wave lineage is recorded with each wave's starting head being the parent wave's commit.
        Assert.Equal("dbdd08e7", sot.RootElement.GetProperty("localizationModuleAmsc001W0").GetProperty("startingHead").GetString());
        Assert.Equal("5a0b882b", sot.RootElement.GetProperty("localizationModuleAmsc001W1").GetProperty("startingHead").GetString());
        Assert.Equal("074fc3a4", sot.RootElement.GetProperty("localizationModuleAmsc001W2").GetProperty("startingHead").GetString());
        Assert.Equal("6bc3ee2d", sot.RootElement.GetProperty("localizationModuleAmsc001W3").GetProperty("startingHead").GetString());
        Assert.Equal("TB-TMAR-LOCALIZATION-AMSC-001-W2", sot.RootElement.GetProperty("localizationModuleAmsc001W3").GetProperty("parentTask").GetString());

        // The historical AMC-001 record stays in place as historical evidence.
        Assert.Equal("COMPLETE_REFERENCE_PATTERN", sot.RootElement.GetProperty("localizationAmc001").GetProperty("state").GetString());
    }

    [Fact]
    public void Localization_has_one_stable_code_owner_with_full_bilingual_coverage()
    {
        var root = Repo();

        var codesFile = File.ReadAllText(Path.Combine(
            root, ModuleRoot, "Tooba.Localization.Contracts", "Errors", "LanguageErrorCodes.cs"));
        var declared = Regex.Matches(codesFile, "public const string \\w+ = \"(?<c>[^\"]+)\"")
            .Select(m => m.Groups["c"].Value).OrderBy(x => x, StringComparer.Ordinal).ToArray();
        Assert.Equal(19, declared.Length);
        foreach (var code in declared)
        {
            Assert.StartsWith("localization.language.", code, StringComparison.Ordinal);
        }

        var contributor = File.ReadAllText(Path.Combine(
            root, ModuleRoot, "Tooba.Localization.Contracts", "Errors", "LocalizationErrorCatalogContributor.cs"));
        var names = Regex.Matches(codesFile, "public const string (?<n>\\w+)")
            .Select(m => m.Groups["n"].Value).ToArray();
        var descriptors = Regex.Matches(contributor, "D\\(LanguageErrorCodes\\.(?<n>\\w+)")
            .Select(m => m.Groups["n"].Value).ToArray();
        Assert.Equal(19, descriptors.Length);
        Assert.Equal(names.Length, descriptors.Distinct(StringComparer.Ordinal).Count());
        foreach (var name in names)
        {
            Assert.Contains(name, descriptors);
        }

        var resourceSet = File.ReadAllText(Path.Combine(
            root, ModuleRoot, "Tooba.Localization.Contracts", "Errors", "LocalizationErrorResourceSet.cs"));
        Assert.Contains("\"localization.\"", resourceSet, StringComparison.Ordinal);

        foreach (var culture in new[] { "LocalizationErrors.resx", "LocalizationErrors.fa.resx" })
        {
            var resx = File.ReadAllText(Path.Combine(
                root, ModuleRoot, "Tooba.Localization.Contracts", "Resources", culture));
            foreach (var code in declared)
            {
                Assert.Contains($"name=\"{code}\"", resx, StringComparison.Ordinal);
            }
        }
    }

    [Fact]
    public void Localization_typed_fault_seam_and_catalog_registration_are_canonical()
    {
        var root = Repo();

        var seam = File.ReadAllText(Path.Combine(
            root, ModuleRoot, "Tooba.Localization.Application", "Composition", "LocalizationOperation.cs"));
        Assert.Contains("public static async Task<Result<T>> ExecuteAsync<T>", seam, StringComparison.Ordinal);
        Assert.Contains("public static async Task<Result> ExecuteAsync", seam, StringComparison.Ordinal);
        Assert.Contains("catch (ContractOperationException ex) when (LanguageErrorCodes.IsKnown(ex.Code))", seam, StringComparison.Ordinal);
        Assert.Contains("catch (SemanticException ex)", seam, StringComparison.Ordinal);
        Assert.DoesNotContain(".Message", seam, StringComparison.Ordinal);

        var module = File.ReadAllText(Path.Combine(
            root, ModuleRoot, "Tooba.Localization.Infrastructure", "LocalizationModule.cs"));
        Assert.Contains("IErrorCatalogContributor, LocalizationErrorCatalogContributor", module, StringComparison.Ordinal);
        Assert.Contains("IErrorResourceSet, LocalizationErrorResourceSet", module, StringComparison.Ordinal);

        // No production file classifies failures by message text and no raw code literal is duplicated.
        // The single tolerated raw-literal fault is the framework outbox invariant (repo-wide idiom shared
        // by 13 modules, including certified ones, and never a user-facing contract).
        const string OutboxInvariantFile = "LocalizationOutboxRegistration.cs";
        foreach (var file in ProductionSources(root))
        {
            var text = File.ReadAllText(file).TrimStart('\uFEFF');
            Assert.DoesNotContain("ex.Message.Contains", text, StringComparison.Ordinal);
            Assert.DoesNotContain("ex.Message.StartsWith", text, StringComparison.Ordinal);
            if (!Path.GetFileName(file).Equals(OutboxInvariantFile, StringComparison.Ordinal))
            {
                Assert.DoesNotMatch(new Regex("InvalidOperationException\\(\"[^\"]+\"\\)"), text);
            }

            if (Path.GetFileName(file).Equals("LanguageErrorCodes.cs", StringComparison.Ordinal))
            {
                continue;
            }

            foreach (var code in new[]
                     {
                         "localization.language.not_found", "localization.language.code_duplicate",
                         "localization.language.invalid_direction", "localization.language.inactive",
                     })
            {
                Assert.DoesNotContain($"\"{code}\"", text, StringComparison.Ordinal);
            }
        }

        // The framework outbox invariant is the only raw-literal fault and it stays confined to its file.
        var outbox = File.ReadAllText(Path.Combine(
            root, ModuleRoot, "Tooba.Localization.Infrastructure", "Persistence", OutboxInvariantFile));
        Assert.Contains("throw new InvalidOperationException(\"Localization integration event is not registered.\")",
            outbox, StringComparison.Ordinal);
    }

    [Fact]
    public void Localization_http_surface_is_module_owned_and_host_residue_is_adapter_only()
    {
        var root = Repo();

        var endpoints = File.ReadAllText(Path.Combine(
            root, ModuleRoot, "Tooba.Localization.Endpoints", "Admin", "LocaleAdminEndpoints.cs"));
        Assert.Contains("MapGet(\"/\"", endpoints, StringComparison.Ordinal);
        Assert.Contains("MapPost(\"/\"", endpoints, StringComparison.Ordinal);
        Assert.Contains("MapPut(\"/{code}\"", endpoints, StringComparison.Ordinal);
        Assert.Contains("MapPatch(\"/{code}\"", endpoints, StringComparison.Ordinal);
        Assert.Contains("ISender", endpoints, StringComparison.Ordinal);
        Assert.DoesNotContain("Results.Json", endpoints, StringComparison.Ordinal);
        Assert.DoesNotContain("Results.BadRequest", endpoints, StringComparison.Ordinal);
        Assert.DoesNotContain("DbContext", endpoints, StringComparison.Ordinal);

        // Host closure: no Host Localization folder and only the authorizer + composition root remain.
        Assert.False(Directory.Exists(Path.Combine(root, "src", "backend", "Host", "Tooba.Host", "Localization")));
        var hostSources = Directory.EnumerateFiles(
                Path.Combine(root, "src", "backend", "Host", "Tooba.Host"), "*.cs", SearchOption.AllDirectories)
            .Where(p => !p.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                        && !p.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .Where(p => File.ReadAllText(p).Contains("Localization", StringComparison.Ordinal))
            .ToArray();
        var hostFiles = hostSources.Select(p => Path.GetFileName(p)!)
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(new[] { "HostLocalizationAdminAuthorizer.cs", "Program.cs", "ToobaModuleComposition.cs" }, hostFiles);

        // The only Host authority over Localization is the thin security adapter plus the composition root:
        // no Host business runtime, no Host persistence owner and no Host-owned route mapping.
        foreach (var file in hostSources)
        {
            var text = File.ReadAllText(file);
            Assert.DoesNotContain("namespace Tooba.Host.Localization", text, StringComparison.Ordinal);
            Assert.DoesNotContain("LocalizationDbContext", text, StringComparison.Ordinal);
            Assert.DoesNotContain("MapLocalizationEndpoints", text, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Localization_is_microservice_extractable_with_zero_foreign_coupling()
    {
        var root = Repo();

        foreach (var project in ProductionProjects)
        {
            var csproj = XDocument.Load(Path.Combine(root, ModuleRoot, project, project + ".csproj"));
            foreach (var reference in csproj.Descendants("ProjectReference")
                         .Select(x => (string?)x.Attribute("Include") ?? string.Empty))
            {
                if (reference.Contains("Tooba.Localization.", StringComparison.Ordinal))
                {
                    continue;
                }

                // The only legal foreign references are the generic platform BuildingBlocks seams.
                Assert.True(
                    reference.Contains("Tooba.BuildingBlocks", StringComparison.Ordinal)
                    || reference.Contains("Tooba.ModuleContracts", StringComparison.Ordinal)
                    || reference.Contains("Tooba.Persistence", StringComparison.Ordinal),
                    $"unexpected foreign reference: {reference}");
                Assert.DoesNotContain(".Application/", reference, StringComparison.Ordinal);
                Assert.DoesNotContain(".Infrastructure/", reference, StringComparison.Ordinal);
                Assert.DoesNotContain(".Domain/", reference, StringComparison.Ordinal);
                Assert.DoesNotContain(".Endpoints/", reference, StringComparison.Ordinal);
            }
        }

        var joined = string.Join("\n", ProductionSources(root).Select(File.ReadAllText));
        foreach (var foreignContext in new[]
                 {
                     "CatalogDbContext", "ContentDbContext", "OrderDbContext", "IdentityDbContext",
                     "CustomerProfileDbContext", "CartDbContext",
                 })
        {
            Assert.DoesNotContain(foreignContext, joined, StringComparison.Ordinal);
        }

        Assert.DoesNotContain("TypeForwardedTo", joined, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.Catalog.Application", joined, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.Content.Application", joined, StringComparison.Ordinal);
    }

    [Fact]
    public void Localization_w3_evidence_and_recovery_checkpoint_are_preserved()
    {
        var root = Repo();

        foreach (var wave in new[] { "W0", "W1", "W2", "W3" })
        {
            Assert.True(Directory.Exists(Path.Combine(
                root, "docs", "architecture", "evidence", $"TB-TMAR-LOCALIZATION-AMSC-001-{wave}")));
        }

        var w3Evidence = File.ReadAllText(Path.Combine(
            root, "docs", "architecture", "evidence", "TB-TMAR-LOCALIZATION-AMSC-001-W3", "certification.md"));
        Assert.Contains("ARCH-COMPLETE-002", w3Evidence, StringComparison.Ordinal);
        Assert.Contains("COMPLETE_REFERENCE_PATTERN", w3Evidence, StringComparison.Ordinal);

        // Repository-global Host root checkpoint must remain untouched by this module-local task.
        using var sot = JsonDocument.Parse(File.ReadAllText(Path.Combine(
            root, "docs/architecture/tmar-current-state.json")));
        Assert.Equal("TB-TMAR-HOST-ROOT-FINAL-CERT-001", sot.RootElement.GetProperty("lastAcceptedTask").GetString());
        Assert.Equal("NONE", sot.RootElement.GetProperty("automaticNextImplementationTask").GetString());
        Assert.Equal("HOST_ROOT_FINAL_CERTIFIED", sot.RootElement.GetProperty("currentHostCheckpoint").GetString());

        var recovery = File.ReadAllText(Path.Combine(root, "docs/architecture", "TOOBA-TMAR-MASTER-RECOVERY.md"));
        Assert.Contains("Localization AMSC module recovery checkpoint", recovery, StringComparison.Ordinal);
        Assert.Contains("TB-TMAR-LOCALIZATION-AMSC-001-W3", recovery, StringComparison.Ordinal);
        Assert.Contains("USER_REVIEW_LOCALIZATION_AMSC_001_W3", recovery, StringComparison.Ordinal);
    }

    private static IEnumerable<string> ProductionSources(string root) =>
        ProductionProjects
            .SelectMany(p => Directory.EnumerateFiles(
                Path.Combine(root, ModuleRoot, p), "*.cs", SearchOption.AllDirectories))
            .Where(p => !p.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                        && !p.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                        && !p.Contains($"{Path.DirectorySeparatorChar}Migrations{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                        && !Path.GetFileName(p).EndsWith("ModelSnapshot.cs", StringComparison.OrdinalIgnoreCase));

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
