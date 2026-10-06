using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>
/// TB-TMAR-MEDIA-AMSC-001-W3 — durable ARCH-COMPLETE-002 certification lock for Media:
/// SoT/manifest certification records with the AMSC wave lineage, the single stable-code owner with
/// bilingual resource coverage, the typed-fault composition seam, module-owned HTTP surface with
/// Contracts-only boundaries and the AMSC evidence tree.
/// </summary>
public sealed class MediaModuleAmsc001W3CertGuardTests
{
    private const string ModuleRoot = "src/backend/Modules/Media";

    private static readonly string[] ProductionProjects =
    [
        "Tooba.Media.Application",
        "Tooba.Media.Contracts",
        "Tooba.Media.Domain",
        "Tooba.Media.Endpoints",
        "Tooba.Media.Infrastructure",
    ];

    [Fact]
    public void Media_is_arch_complete_002_certified_in_sot_and_manifest()
    {
        var root = Repo();

        using var manifest = JsonDocument.Parse(File.ReadAllText(Path.Combine(
            root, "docs/architecture/tmar-module-structure-manifests.json")));
        var entries = manifest.RootElement.GetProperty("modules").EnumerateArray()
            .Where(m => string.Equals(m.GetProperty("module").GetString(), "Media", StringComparison.Ordinal))
            .ToArray();
        Assert.Single(entries);
        Assert.True(entries[0].GetProperty("structureCertified").GetBoolean());
        Assert.Equal("ARCH-COMPLETE-002", entries[0].GetProperty("lockVersion").GetString());
        Assert.Equal(5, entries[0].GetProperty("projects").GetArrayLength());
        Assert.DoesNotContain(
            manifest.RootElement.GetProperty("preCertModules").EnumerateArray(),
            m => string.Equals(m.GetProperty("module").GetString(), "Media", StringComparison.Ordinal));

        using var sot = JsonDocument.Parse(File.ReadAllText(Path.Combine(
            root, "docs/architecture/tmar-current-state.json")));
        var w3 = sot.RootElement.GetProperty("mediaModuleAmsc001W3");
        Assert.Equal("MEDIA_AMSC_001_CERTIFIED", w3.GetProperty("state").GetString());
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
        Assert.Equal(5, w3.GetProperty("routeCount").GetInt32());
        Assert.Equal("ZERO", w3.GetProperty("blockingResidualDebt").GetString());
        Assert.True(w3.GetProperty("microserviceExtractable").GetBoolean());
        Assert.Equal("NONE", w3.GetProperty("automaticNextImplementationTask").GetString());
        Assert.Equal("USER_REVIEW_MEDIA_AMSC_001_W3", w3.GetProperty("stopGate").GetString());
        Assert.Equal("PRESERVED", w3.GetProperty("hostFinalClosureState").GetString());
        Assert.Equal("ZERO", w3.GetProperty("sinkFolderRegressionState").GetString());
        Assert.Equal("NONE", w3.GetProperty("sensitiveLoggingState").GetString());
        Assert.Equal("CONTRACTS_ONLY", w3.GetProperty("crossModuleBoundaryState").GetString());

        var certified = sot.RootElement.GetProperty("structureLock").GetProperty("certifiedModules")
            .EnumerateArray().Select(x => x.GetString()).ToArray();
        Assert.Equal(1, certified.Count(x => x == "Media"));

        // Wave lineage is recorded with each wave's starting head being the parent wave's commit.
        Assert.Equal("70e55d40", sot.RootElement.GetProperty("mediaModuleAmsc001W0").GetProperty("startingHead").GetString());
        Assert.Equal("06f7de21", sot.RootElement.GetProperty("mediaModuleAmsc001W1").GetProperty("startingHead").GetString());
        Assert.Equal("0b0fde0a", sot.RootElement.GetProperty("mediaModuleAmsc001W2").GetProperty("startingHead").GetString());
        Assert.Equal("991551e9", sot.RootElement.GetProperty("mediaModuleAmsc001W3").GetProperty("startingHead").GetString());
        Assert.Equal("TB-TMAR-MEDIA-AMSC-001-W2", sot.RootElement.GetProperty("mediaModuleAmsc001W3").GetProperty("parentTask").GetString());

        // The historical AMC-001 record stays in place as historical evidence.
        Assert.Equal("COMPLETE_REFERENCE_PATTERN", sot.RootElement.GetProperty("mediaAmc001").GetProperty("state").GetString());
    }

    [Fact]
    public void Media_has_one_stable_code_owner_with_full_bilingual_coverage()
    {
        var root = Repo();

        var codesFile = File.ReadAllText(Path.Combine(
            root, ModuleRoot, "Tooba.Media.Contracts", "Errors", "MediaErrorCodes.cs"));
        var declared = Regex.Matches(codesFile, "public const string \\w+ = \"(?<c>[^\"]+)\"")
            .Select(m => m.Groups["c"].Value).OrderBy(x => x, StringComparer.Ordinal).ToArray();
        Assert.Equal(6, declared.Length);
        foreach (var code in declared)
        {
            Assert.StartsWith("media.", code, StringComparison.Ordinal);
        }

        var contractCode = File.ReadAllText(Path.Combine(
            root, ModuleRoot, "Tooba.Media.Contracts", "Ports", "MediaAssetContractCodes.cs"));
        var assetMissing = Regex.Match(contractCode, "public const string \\w+ = \"(?<c>[^\"]+)\"")
            .Groups["c"].Value;
        Assert.Equal("media.asset.missing", assetMissing);

        var contributor = File.ReadAllText(Path.Combine(
            root, ModuleRoot, "Tooba.Media.Contracts", "Errors", "MediaErrorCatalogContributor.cs"));
        var names = Regex.Matches(codesFile, "public const string (?<n>\\w+)")
            .Select(m => m.Groups["n"].Value).ToArray();
        var descriptors = Regex.Matches(contributor, "D\\(MediaErrorCodes\\.(?<n>\\w+)")
            .Select(m => m.Groups["n"].Value).ToArray();
        Assert.Equal(6, descriptors.Length);
        Assert.Equal(names.Length, descriptors.Distinct(StringComparer.Ordinal).Count());
        foreach (var name in names)
        {
            Assert.Contains(name, descriptors);
        }

        // The seventh declared code is the cross-module contract code, registered by the same owner.
        Assert.Contains("D(MediaAssetContractCodes.AssetMissing", contributor, StringComparison.Ordinal);

        var resourceSet = File.ReadAllText(Path.Combine(
            root, ModuleRoot, "Tooba.Media.Contracts", "Errors", "MediaErrorResourceSet.cs"));
        Assert.Contains("\"media.\"", resourceSet, StringComparison.Ordinal);

        var expectedKeys = declared.Append(assetMissing).OrderBy(x => x, StringComparer.Ordinal).ToArray();
        foreach (var culture in new[] { "MediaErrors.resx", "MediaErrors.fa.resx" })
        {
            var resx = File.ReadAllText(Path.Combine(
                root, ModuleRoot, "Tooba.Media.Contracts", "Resources", culture));
            foreach (var code in expectedKeys)
            {
                Assert.Contains($"name=\"{code}\"", resx, StringComparison.Ordinal);
            }
        }
    }

    [Fact]
    public void Media_typed_fault_seam_and_catalog_registration_are_canonical()
    {
        var root = Repo();

        var seam = File.ReadAllText(Path.Combine(
            root, ModuleRoot, "Tooba.Media.Application", "Composition", "MediaOperation.cs"));
        Assert.Contains("public static async Task<Result<T>> ExecuteAsync<T>", seam, StringComparison.Ordinal);
        Assert.Contains("public static async Task<Result> ExecuteAsync", seam, StringComparison.Ordinal);
        Assert.Contains("catch (ContractOperationException ex) when (MediaErrorCodes.IsKnown(ex.Code))", seam, StringComparison.Ordinal);
        Assert.Contains("catch (SemanticException ex)", seam, StringComparison.Ordinal);
        Assert.DoesNotContain(".Message", seam, StringComparison.Ordinal);

        var module = File.ReadAllText(Path.Combine(
            root, ModuleRoot, "Tooba.Media.Infrastructure", "MediaModule.cs"));
        Assert.Contains("IErrorCatalogContributor, MediaErrorCatalogContributor", module, StringComparison.Ordinal);
        Assert.Contains("IErrorResourceSet, MediaErrorResourceSet", module, StringComparison.Ordinal);

        // No production file classifies failures by message text and no raw code literal is duplicated.
        // Exactly two files may carry a raw-literal framework/domain invariant, and both are the
        // repository-wide idiom shared by certified modules — never a user-facing contract:
        //   * MediaModule.cs   — the framework outbox invariant (Localization/Content/CustomerProfile identical);
        //   * MediaAsset.cs    — Domain aggregate invariants (Order/Catalog/Promotion/Notification/Wallet/
        //                        Support/Returns/Tax all raise InvalidOperationException with literal text).
        var invariantFiles = new[] { "MediaAsset.cs", "MediaModule.cs" };
        foreach (var file in ProductionSources(root))
        {
            var text = File.ReadAllText(file).TrimStart('\uFEFF');
            Assert.DoesNotContain("ex.Message.Contains", text, StringComparison.Ordinal);
            Assert.DoesNotContain("ex.Message.StartsWith", text, StringComparison.Ordinal);
            if (!invariantFiles.Contains(Path.GetFileName(file), StringComparer.Ordinal))
            {
                Assert.DoesNotMatch(new Regex("InvalidOperationException\\(\"[^\"]+\"\\)"), text);
            }

            if (Path.GetFileName(file).Equals("MediaErrorCodes.cs", StringComparison.Ordinal))
            {
                continue;
            }

            foreach (var code in new[]
                     {
                         "media.upload.failed", "media.type.unsupported", "media.too_large",
                         "media.storage.unavailable", "media.missing", "media.validation.failed",
                     })
            {
                Assert.DoesNotContain($"\"{code}\"", text, StringComparison.Ordinal);
            }
        }

        // The two raw-literal idiom files are exactly the ones that carry them — no silent drift.
        var literalCarriers = ProductionSources(root)
            .Where(p => Regex.IsMatch(File.ReadAllText(p), "InvalidOperationException\\(\"[^\"]+\"\\)"))
            .Select(Path.GetFileName!)
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(invariantFiles, literalCarriers);

        // Domain invariants can never become a user-facing contract: the seam maps only typed codes.
        Assert.Contains("throw new InvalidOperationException(\"Media integration event is not registered.\")",
            module, StringComparison.Ordinal);
        Assert.DoesNotContain("InvalidOperationException", seam, StringComparison.Ordinal);
        Assert.DoesNotContain("InvalidOperationException", File.ReadAllText(Path.Combine(
            root, ModuleRoot, "Tooba.Media.Application", "Assets", "Commands", "UploadMediaAssetCommand.cs")),
            StringComparison.Ordinal);
    }

    [Fact]
    public void Media_http_surface_is_module_owned_and_host_residue_is_composition_only()
    {
        var root = Repo();

        var endpoints = File.ReadAllText(Path.Combine(
            root, ModuleRoot, "Tooba.Media.Endpoints", "Admin", "MediaAdminEndpoints.cs"));
        Assert.Contains("MapPost(\"/upload\"", endpoints, StringComparison.Ordinal);
        Assert.Contains("MapGet(\"/\"", endpoints, StringComparison.Ordinal);
        Assert.Contains("MapGet(\"/{id:guid}\"", endpoints, StringComparison.Ordinal);
        Assert.Contains("ISender", endpoints, StringComparison.Ordinal);
        Assert.DoesNotContain("Results.Json", endpoints, StringComparison.Ordinal);
        Assert.DoesNotContain("Results.BadRequest", endpoints, StringComparison.Ordinal);
        Assert.DoesNotContain("DbContext", endpoints, StringComparison.Ordinal);

        var module = File.ReadAllText(Path.Combine(
            root, ModuleRoot, "Tooba.Media.Endpoints", "MediaEndpointModule.cs"));
        Assert.Contains("MapMediaModuleEndpoints", module, StringComparison.Ordinal);
        Assert.Contains("/v1/media/{id:guid}", module, StringComparison.Ordinal);

        var storefront = File.ReadAllText(Path.Combine(
            root, ModuleRoot, "Tooba.Media.Endpoints", "Storefront", "MediaStorefrontEndpoints.cs"));
        Assert.Contains("/v1/storefront/media/{assetId:guid}", storefront, StringComparison.Ordinal);

        // Host closure: no Host Media folder and only composition roots remain.
        Assert.False(Directory.Exists(Path.Combine(root, "src", "backend", "Host", "Tooba.Host", "Media")));
        var hostSources = Directory.EnumerateFiles(
                Path.Combine(root, "src", "backend", "Host", "Tooba.Host"), "*.cs", SearchOption.AllDirectories)
            .Where(p => !p.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                        && !p.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .Where(p => File.ReadAllText(p).Contains("Media", StringComparison.Ordinal))
            .Select(p => Path.GetFileName(p)!)
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(
            new[] { "MarketplaceDevelopmentBootstrap.cs", "Program.cs", "ToobaModuleComposition.cs" },
            hostSources);

        // The only Host authority over Media is the composition root plus the dev-migration seam:
        // no Host business runtime, no Host persistence owner and no Host-owned route mapping.
        foreach (var file in new[]
                 {
                     "Program.cs",
                     "Composition/ToobaModuleComposition.cs",
                     "Development/MarketplaceDevelopmentBootstrap.cs",
                 })
        {
            var text = File.ReadAllText(Path.Combine(root, "src", "backend", "Host", "Tooba.Host", file));
            Assert.DoesNotContain("namespace Tooba.Host.Media", text, StringComparison.Ordinal);
            Assert.DoesNotContain("MapMediaAdminEndpoints", text, StringComparison.Ordinal);
            Assert.DoesNotContain("MapMediaStorefrontEndpoints", text, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Media_is_microservice_extractable_with_zero_foreign_coupling()
    {
        var root = Repo();

        foreach (var project in ProductionProjects)
        {
            var csproj = XDocument.Load(Path.Combine(root, ModuleRoot, project, project + ".csproj"));
            foreach (var reference in csproj.Descendants("ProjectReference")
                         .Select(x => (string?)x.Attribute("Include") ?? string.Empty))
            {
                if (reference.Contains("Tooba.Media.", StringComparison.Ordinal))
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
                     "CustomerProfileDbContext", "CartDbContext", "LocalizationDbContext",
                 })
        {
            Assert.DoesNotContain(foreignContext, joined, StringComparison.Ordinal);
        }

        Assert.DoesNotContain("TypeForwardedTo", joined, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.Catalog.", joined, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.Content.", joined, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.Payment.", joined, StringComparison.Ordinal);

        // No cross-module SQL/EF join and no foreign schema reach-through.
        foreach (var file in ProductionSources(root))
        {
            var text = File.ReadAllText(file);
            Assert.DoesNotContain("FromSqlRaw", text, StringComparison.Ordinal);
            Assert.DoesNotContain("ExecuteSqlRaw", text, StringComparison.Ordinal);
            foreach (var foreignSchema in new[]
                     {
                         "\"catalog.", "\"order.", "\"content.", "\"payment.", "\"identity.",
                         "\"localization.", "\"inventory.", "\"offer.", "\"party.",
                     })
            {
                Assert.DoesNotContain(foreignSchema, text, StringComparison.Ordinal);
            }
        }
    }

    [Fact]
    public void Media_schema_and_migrations_are_preserved_by_certification()
    {
        var root = Repo();
        var context = File.ReadAllText(Path.Combine(
            root, ModuleRoot, "Tooba.Media.Infrastructure", "Persistence", "MediaDbContext.cs"));
        Assert.Contains("public const string Schema = \"media\";", context, StringComparison.Ordinal);
        Assert.Contains("HasDefaultSchema(Schema)", context, StringComparison.Ordinal);

        var migrations = Directory.EnumerateFiles(Path.Combine(
                root, ModuleRoot, "Tooba.Media.Infrastructure", "Persistence", "Migrations"), "*.cs")
            .Select(Path.GetFileName)
            .Where(name => name!.EndsWith("_InitialMedia.cs", StringComparison.Ordinal)
                || name.EndsWith("_AddMediaFocalPoint.cs", StringComparison.Ordinal))
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(
            new[] { "20260830060000_InitialMedia.cs", "20260916053000_AddMediaFocalPoint.cs" },
            migrations);
    }

    [Fact]
    public void Media_w3_evidence_and_recovery_checkpoint_are_preserved()
    {
        var root = Repo();

        foreach (var wave in new[] { "W0", "W1", "W2", "W3" })
        {
            Assert.True(Directory.Exists(Path.Combine(
                root, "docs", "architecture", "evidence", $"TB-TMAR-MEDIA-AMSC-001-{wave}")));
        }

        var w3Evidence = File.ReadAllText(Path.Combine(
            root, "docs", "architecture", "evidence", "TB-TMAR-MEDIA-AMSC-001-W3", "certification.md"));
        Assert.Contains("ARCH-COMPLETE-002", w3Evidence, StringComparison.Ordinal);
        Assert.Contains("COMPLETE_REFERENCE_PATTERN", w3Evidence, StringComparison.Ordinal);

        // Repository-global Host root checkpoint must remain untouched by this module-local task.
        using var sot = JsonDocument.Parse(File.ReadAllText(Path.Combine(
            root, "docs/architecture/tmar-current-state.json")));
        Assert.Equal("TB-TMAR-HOST-ROOT-FINAL-CERT-001", sot.RootElement.GetProperty("lastAcceptedTask").GetString());
        Assert.Equal("NONE", sot.RootElement.GetProperty("automaticNextImplementationTask").GetString());
        Assert.Equal("HOST_ROOT_FINAL_CERTIFIED", sot.RootElement.GetProperty("currentHostCheckpoint").GetString());

        var recovery = File.ReadAllText(Path.Combine(root, "docs/architecture", "TOOBA-TMAR-MASTER-RECOVERY.md"));
        Assert.Contains("Media AMSC module recovery checkpoint", recovery, StringComparison.Ordinal);
        Assert.Contains("TB-TMAR-MEDIA-AMSC-001-W3", recovery, StringComparison.Ordinal);
        Assert.Contains("USER_REVIEW_MEDIA_AMSC_001_W3", recovery, StringComparison.Ordinal);
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
