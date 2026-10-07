using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using Tooba.BuildingBlocks.Localization;
using Tooba.BuildingBlocks.Presentation.Errors;
using Tooba.Pricing.Application.Composition;
using Tooba.Pricing.Contracts.Errors;
using Tooba.Pricing.Contracts.Ports;
using Tooba.Pricing.Contracts.Seller;
using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>
/// TB-TMAR-PRICING-AMSC-001-W3-R2 — INTERNAL_ONLY structure-repair lock (tooba-architecture-structure).
/// <para>
/// The former W3 certification was structurally stale: it certified Pricing as
/// <c>NOT_HTTP_OWNING_INTERNAL_CAPABILITY_PROVIDER</c> while the module still carried a ceremonial
/// <c>Tooba.Pricing.Endpoints</c> project, an empty <c>/v1/pricing</c> route group and Host
/// mapping/registration ceremony. This guard pins the repaired truth: Pricing is an INTERNAL_ONLY
/// capability provider with <b>no</b> Endpoints project, zero HTTP routes, presentation registration
/// moved into the Pricing Infrastructure composition root (Inventory precedent), exactly four
/// production projects, and honest pre-cert manifest/SoT state pending a fresh Certify wave.
/// </para>
/// <para>
/// The still-valid certification semantics (single declared-code home, typed-fault seam, one catalog
/// contributor + one resource set, composed-catalog uniqueness, bilingual composed resolution,
/// Contracts-only boundaries, own schema/migration) are re-asserted here so the repair cannot erode
/// them.
/// </para>
/// </summary>
public sealed class PricingModuleAmsc001W3R2RepairGuardTests
{
    private const string ModuleRoot = "src/backend/Modules/Pricing";

    private static readonly string[] ProductionProjects =
    [
        "Tooba.Pricing.Contracts",
        "Tooba.Pricing.Domain",
        "Tooba.Pricing.Application",
        "Tooba.Pricing.Infrastructure",
    ];

    private static readonly ErrorDefinitionCatalog Catalog = new(
    [
        new FoundationErrorCatalogContributor(),
        new PricingErrorCatalogContributor(),
    ]);

    private static readonly ResourceErrorMessageLocalizer Localizer = new(
        [new FoundationErrorResourceSet(), new PricingErrorResourceSet()],
        Array.Empty<IErrorMessageContributor>());

    [Fact]
    public void Prior_w3_certification_is_superseded_and_pricing_is_precert_ready_for_certify()
    {
        var root = Repo();

        using var manifest = JsonDocument.Parse(
            File.ReadAllText(Path.Combine(root, "docs/architecture/tmar-module-structure-manifests.json"))
                .Replace("\uFEFF", string.Empty));

        // Pricing must NOT remain currently structureCertified during this repair.
        Assert.DoesNotContain(
            manifest.RootElement.GetProperty("modules").EnumerateArray(),
            m => string.Equals(m.GetProperty("module").GetString(), "Pricing", StringComparison.Ordinal));

        var preCert = manifest.RootElement.GetProperty("preCertModules").EnumerateArray()
            .Single(m => string.Equals(m.GetProperty("module").GetString(), "Pricing", StringComparison.Ordinal));
        Assert.False(preCert.GetProperty("structureCertified").GetBoolean());
        Assert.Equal("ARCH-COMPLETE-002", preCert.GetProperty("lockVersion").GetString());
        Assert.Equal("READY_FOR_CERTIFY", preCert.GetProperty("structureState").GetString());
        Assert.Equal("TB-TMAR-PRICING-AMSC-001-W3-R2", preCert.GetProperty("structureRepairTask").GetString());

        // Exactly five projects (four production + Tests): the Endpoints project entry is gone.
        var projects = preCert.GetProperty("projects").EnumerateArray()
            .Select(p => p.GetProperty("projectName").GetString()!)
            .OrderBy(x => x, StringComparer.Ordinal).ToArray();
        Assert.Equal(
            new[]
            {
                "Tooba.Pricing.Application",
                "Tooba.Pricing.Contracts",
                "Tooba.Pricing.Domain",
                "Tooba.Pricing.Infrastructure",
                "Tooba.Pricing.Tests",
            },
            projects);
        Assert.DoesNotContain("Tooba.Pricing.Endpoints", projects);

        // Pricing owns no HTTP route so it is not an uncertified HTTP-owning module either.
        Assert.DoesNotContain(
            "Pricing",
            manifest.RootElement.GetProperty("uncertifiedHttpOwningModules").EnumerateArray()
                .Select(x => x.GetString()!),
            StringComparer.Ordinal);

        using var sot = JsonDocument.Parse(
            File.ReadAllText(Path.Combine(root, "docs/architecture/tmar-current-state.json"))
                .Replace("\uFEFF", string.Empty));

        // The historical W3 block is preserved as history but is explicitly superseded.
        var w3 = sot.RootElement.GetProperty("pricingAmsc001W3");
        Assert.Equal("PRICING_AMSC_001_CERTIFIED", w3.GetProperty("state").GetString());

        var r2 = sot.RootElement.GetProperty("pricingAmsc001W3R2");
        Assert.Equal("TB-TMAR-PRICING-AMSC-001-W3-R2", r2.GetProperty("task").GetString());
        Assert.Equal("TB-TMAR-PRICING-AMSC-001-W3-R1", r2.GetProperty("parentTask").GetString());
        Assert.Equal("STRUCTURE_REPAIR_INTERNAL_ONLY_APPLICABILITY", r2.GetProperty("mode").GetString());
        Assert.Equal("PRICING_AMSC_001_STRUCTURE_REPAIRED_READY_FOR_CERTIFY", r2.GetProperty("state").GetString());
        Assert.Equal("READY_FOR_CERTIFY", r2.GetProperty("verdict").GetString());
        Assert.Equal("SUPERSEDED_PENDING_FRESH_CERTIFY", r2.GetProperty("priorCertificationState").GetString());
        Assert.Equal("CANONICAL_NO_ENDPOINTS_PROJECT", r2.GetProperty("internalOnlyApplicabilityState").GetString());
        Assert.Equal("NOT_HTTP_OWNING_INTERNAL_CAPABILITY_PROVIDER", r2.GetProperty("httpApplicability").GetString());
        Assert.Equal("ABSENT", r2.GetProperty("endpointProjectState").GetString());
        Assert.Equal(0, r2.GetProperty("endpointRouteCount").GetInt32());
        Assert.Equal(0, r2.GetProperty("endpointReachableRequests").GetInt32());
        Assert.Equal("INFRASTRUCTURE_MODULE_COMPOSITION", r2.GetProperty("presentationRegistrationState").GetString());
        Assert.Equal(5, r2.GetProperty("solutionProjectCount").GetInt32());
        Assert.Equal("EXACT", r2.GetProperty("pathNamespaceState").GetString());
        Assert.Equal("ENFORCED", r2.GetProperty("rootAllowlistState").GetString());
        Assert.Equal("CLEAN", r2.GetProperty("physicalCopyState").GetString());
        Assert.Equal("PROFESSIONAL_SHALLOW", r2.GetProperty("folderGranularityState").GetString());
        Assert.Equal("ZERO", r2.GetProperty("foreignAppInfraDomainCoupling").GetString());
        Assert.Equal("UNCHANGED", r2.GetProperty("schemaMigrationState").GetString());
        Assert.Equal("PRESERVED", r2.GetProperty("productionBehaviorState").GetString());
        Assert.Equal("NONE", r2.GetProperty("guardsWeakened").GetString());
        Assert.Equal("NONE", r2.GetProperty("baselinesWidened").GetString());
        Assert.Equal("NONE", r2.GetProperty("automaticNextImplementationTask").GetString());
        Assert.Equal("USER_REVIEW_PRICING_AMSC_001_W3_R2", r2.GetProperty("workflowStop").GetString());

        // Pricing is temporarily removed from structureLock.certifiedModules until fresh Certify passes.
        var certified = sot.RootElement.GetProperty("structureLock").GetProperty("certifiedModules")
            .EnumerateArray().Select(x => x.GetString()).ToArray();
        Assert.DoesNotContain("Pricing", certified, StringComparer.Ordinal);
    }

    [Fact]
    public void Endpoints_ceremony_is_absent_and_registration_lives_in_infrastructure_composition()
    {
        var root = Repo();

        Assert.False(Directory.Exists(Path.Combine(root, ModuleRoot, "Tooba.Pricing.Endpoints")));
        Assert.False(Directory.Exists(Path.Combine(root, ModuleRoot, "Tooba.Pricing.Tests", "Endpoints")));

        foreach (var file in ProductionSources(root))
        {
            var text = File.ReadAllText(file);
            Assert.DoesNotContain("Tooba.Pricing.Endpoints", text, StringComparison.Ordinal);
            Assert.DoesNotContain("MapPricingModule", text, StringComparison.Ordinal);
            Assert.DoesNotContain("AddPricingEndpointPresentation", text, StringComparison.Ordinal);
            Assert.DoesNotContain("\"/v1/pricing\"", text, StringComparison.Ordinal);
        }

        // The single registration site is the Pricing Infrastructure composition root.
        var module = Read("src/backend/Modules/Pricing/Tooba.Pricing.Infrastructure/DependencyInjection/PricingModule.cs");
        Assert.Contains("using Tooba.Pricing.Contracts.Errors;", module, StringComparison.Ordinal);
        Assert.Equal(1, Regex.Matches(module, @"AddSingleton<\s*IErrorCatalogContributor,\s*PricingErrorCatalogContributor>").Count);
        Assert.Equal(1, Regex.Matches(module, @"AddSingleton<\s*IErrorResourceSet,\s*PricingErrorResourceSet>").Count);
        Assert.DoesNotContain("ISender", module, StringComparison.Ordinal);
        Assert.DoesNotContain("ApiResponseFactory", module, StringComparison.Ordinal);

        // Host composition carries no Pricing Endpoints reference and no Pricing route group.
        var program = Read("src/backend/Host/Tooba.Host/Program.cs");
        Assert.DoesNotContain("Tooba.Pricing.Endpoints", program, StringComparison.Ordinal);
        Assert.DoesNotContain("MapPricingModule", program, StringComparison.Ordinal);
        Assert.DoesNotContain("AddPricingEndpointPresentation", program, StringComparison.Ordinal);
        Assert.DoesNotContain("\"/v1/pricing\"", program, StringComparison.Ordinal);

        var hostCsproj = Read("src/backend/Host/Tooba.Host/Tooba.Host.csproj");
        Assert.DoesNotContain("Tooba.Pricing.Endpoints", hostCsproj, StringComparison.Ordinal);
        Assert.Contains("Tooba.Pricing.Infrastructure.csproj", hostCsproj, StringComparison.Ordinal);

        // /Modules/Pricing/ groups exactly five projects (four production + Tests).
        var slnx = File.ReadAllText(Path.Combine(root, "src/backend/Tooba.slnx"));
        var folderStart = slnx.IndexOf("<Folder Name=\"/Modules/Pricing/\">", StringComparison.Ordinal);
        Assert.True(folderStart >= 0);
        var group = slnx[folderStart..slnx.IndexOf("</Folder>", folderStart, StringComparison.Ordinal)];
        Assert.Equal(5, Regex.Matches(group, @"<Project Path=").Count);
        Assert.DoesNotContain("Tooba.Pricing.Endpoints", group, StringComparison.Ordinal);
    }

    [Fact]
    public void Amsc_lineage_pins_analyze_migrate_and_structure_shas()
    {
        using var sot = JsonDocument.Parse(
            File.ReadAllText(Path.Combine(Repo(), "docs/architecture/tmar-current-state.json"))
                .Replace("\uFEFF", string.Empty));

        var w0 = sot.RootElement.GetProperty("pricingAmsc001W0");
        Assert.Equal("ANALYZE_COMPLETE", w0.GetProperty("state").GetString());
        Assert.Equal("READY_TO_MIGRATE", w0.GetProperty("verdict").GetString());

        var w1 = sot.RootElement.GetProperty("pricingAmsc001W1");
        Assert.Equal("MIGRATE_COMPLETE", w1.GetProperty("state").GetString());
        Assert.Equal("READY_TO_STRUCTURE", w1.GetProperty("verdict").GetString());
        Assert.Equal("COMPLIANT", w1.GetProperty("contractsBoundaryState").GetString());

        var w2 = sot.RootElement.GetProperty("pricingAmsc001W2");
        Assert.Equal("STRUCTURE_COMPLETE", w2.GetProperty("state").GetString());
        Assert.Equal("READY_FOR_CERTIFY", w2.GetProperty("verdict").GetString());
        Assert.Equal("PROFESSIONAL_SHALLOW", w2.GetProperty("folderGranularityState").GetString());
        Assert.Equal("EXACT", w2.GetProperty("pathNamespaceState").GetString());
        Assert.Equal("ENFORCED", w2.GetProperty("rootAllowlistState").GetString());
        Assert.Equal("CLEAN", w2.GetProperty("physicalCopyState").GetString());
        Assert.Equal("CANONICAL", w2.GetProperty("solutionExplorerState").GetString());
        Assert.Equal("PRESERVED", w2.GetProperty("hostFinalClosure").GetString());

        // The certification wave must record the accepted AMSC lineage SHAs.
        var lineage = sot.RootElement.GetProperty("pricingAmsc001W3").GetProperty("acceptedLineage");
        Assert.Equal("08d47b6a", lineage.GetProperty("w0").GetString());
        Assert.Equal("069f77d2", lineage.GetProperty("w1").GetString());
        Assert.Equal("f7f6abfe", lineage.GetProperty("w2").GetString());
        Assert.Equal(
            "f7f6abfec455b771952852e8627df4c57b698caf",
            lineage.GetProperty("w2StructureCommit").GetString());
        Assert.Equal("3c2cc61e", lineage.GetProperty("w3").GetString());
        Assert.Equal(
            "3c2cc61e7c61813ac72773ccdb8bb16317cafe70",
            lineage.GetProperty("w3CertificationCommit").GetString());
        Assert.Equal("3c2cc61e", sot.RootElement.GetProperty("pricingAmsc001W3").GetProperty("commit").GetString());
        Assert.Equal(
            "3c2cc61e7c61813ac72773ccdb8bb16317cafe70",
            sot.RootElement.GetProperty("pricingAmsc001W3").GetProperty("commitFull").GetString());
    }

    [Fact]
    public void W3_r1_recovery_reconciliation_is_recorded()
    {
        using var sot = JsonDocument.Parse(
            File.ReadAllText(Path.Combine(Repo(), "docs/architecture/tmar-current-state.json"))
                .Replace("\uFEFF", string.Empty));

        var r1 = sot.RootElement.GetProperty("pricingAmsc001W3R1");
        Assert.Equal("TB-TMAR-PRICING-AMSC-001-W3-R1", r1.GetProperty("task").GetString());
        Assert.Equal("TB-TMAR-PRICING-AMSC-001-W3", r1.GetProperty("parentTask").GetString());
        Assert.Equal("RECOVERY_SOT_RECONCILIATION_ONLY", r1.GetProperty("mode").GetString());
        Assert.Equal("PRICING_AMSC_001_RECOVERY_RECONCILED", r1.GetProperty("state").GetString());
        Assert.Equal("3c2cc61e7c61813ac72773ccdb8bb16317cafe70", r1.GetProperty("certifiedCommit").GetString());
        Assert.Equal("PENDING_THIS_COMMIT", r1.GetProperty("masterRecoveryW3ShaBefore").GetString());
        Assert.Equal("RECORDED_3C2CC61E", r1.GetProperty("masterRecoveryW3ShaState").GetString());
        Assert.False(r1.GetProperty("productionCodeChanged").GetBoolean());
        Assert.Equal("PRESERVED", r1.GetProperty("globalHostCheckpointState").GetString());
        Assert.Equal("NOT_TOUCHED", r1.GetProperty("manifestStructuralState").GetString());
        Assert.Equal("NONE", r1.GetProperty("guardsWeakened").GetString());
        Assert.Equal("NONE", r1.GetProperty("automaticNextImplementationTask").GetString());

        // The W3-R2 repair records the R2 parent as the superseded W3 lineage.
        var r2 = sot.RootElement.GetProperty("pricingAmsc001W3R2");
        Assert.Equal("3c2cc61e7c61813ac72773ccdb8bb16317cafe70", r2.GetProperty("supersededCertificationCommit").GetString());
        Assert.Equal("TB-TMAR-PRICING-AMSC-001-W3", r2.GetProperty("supersededCertificationTask").GetString());
        Assert.Equal("2e664bb3", r2.GetProperty("startingHead").GetString());

        // The repository-global Host root checkpoint is not displaced by a module-local repair wave.
        Assert.Equal("TB-TMAR-HOST-ROOT-FINAL-CERT-001", sot.RootElement.GetProperty("lastAcceptedTask").GetString());
        Assert.Equal("NONE", sot.RootElement.GetProperty("automaticNextImplementationTask").GetString());

        var recovery = File.ReadAllText(Path.Combine(Repo(), "docs/architecture/TOOBA-TMAR-MASTER-RECOVERY.md"));
        Assert.Contains("TB-TMAR-PRICING-AMSC-001-W0` Analyze `08d47b6a`", recovery, StringComparison.Ordinal);
        Assert.Contains("TB-TMAR-PRICING-AMSC-001-W1` Migrate `069f77d2`", recovery, StringComparison.Ordinal);
        Assert.Contains("TB-TMAR-PRICING-AMSC-001-W2` Structure `f7f6abfe`", recovery, StringComparison.Ordinal);
        Assert.Contains("TB-TMAR-PRICING-AMSC-001-W3` Certify `3c2cc61e`", recovery, StringComparison.Ordinal);
        Assert.Contains("USER_REVIEW_PRICING_AMSC_001_W3_R1", recovery, StringComparison.Ordinal);
        Assert.Contains("Pricing AMSC W3-R2 structure repair (module-local)", recovery, StringComparison.Ordinal);
    }

    [Fact]
    public void Structure_gate_fields_are_ready_for_certify_on_disk()
    {
        var root = Repo();

        // Root-Allowlist-State: only Domain carries a root source file (the namespace bridge).
        foreach (var project in ProductionProjects)
        {
            var projectPath = Path.Combine(root, ModuleRoot, project);
            var rootFiles = Directory.GetFiles(projectPath, "*.cs", SearchOption.TopDirectoryOnly)
                .Select(Path.GetFileName!)
                .OrderBy(x => x, StringComparer.Ordinal)
                .ToArray();
            var expected = project == "Tooba.Pricing.Domain"
                ? new[] { "GlobalUsings.cs" }
                : Array.Empty<string>();
            Assert.Equal(expected, rootFiles);
        }

        // Folder-Granularity-State: capability-first, no technical-axis-first root, zero use-case leaves.
        Assert.Equal(
            new[] { "Dtos", "Errors", "Ports", "Resources", "Seller" },
            Directory.GetDirectories(Path.Combine(root, ModuleRoot, "Tooba.Pricing.Contracts"))
                .Select(Path.GetFileName!)
                .Where(n => n is not ("bin" or "obj" or "artifacts"))
                .OrderBy(x => x, StringComparer.Ordinal).ToArray());
        Assert.Equal(
            new[] { "Aggregates", "Enums", "Events", "ValueObjects" },
            Directory.GetDirectories(Path.Combine(root, ModuleRoot, "Tooba.Pricing.Domain"))
                .Select(Path.GetFileName!)
                .Where(n => n is not ("bin" or "obj" or "artifacts"))
                .OrderBy(x => x, StringComparer.Ordinal).ToArray());
        Assert.Equal(
            new[] { "Composition", "Ports" },
            Directory.GetDirectories(Path.Combine(root, ModuleRoot, "Tooba.Pricing.Application"))
                .Select(Path.GetFileName!)
                .Where(n => n is not ("bin" or "obj" or "artifacts"))
                .OrderBy(x => x, StringComparer.Ordinal).ToArray());
        Assert.Equal(
            new[] { "Adapters", "DependencyInjection", "Events", "Outbox", "Persistence" },
            Directory.GetDirectories(Path.Combine(root, ModuleRoot, "Tooba.Pricing.Infrastructure"))
                .Select(Path.GetFileName!)
                .Where(n => n is not ("bin" or "obj" or "artifacts"))
                .OrderBy(x => x, StringComparer.Ordinal).ToArray());

        foreach (var banned in new[] { "Commands", "Queries", "Validators", "Models", "Handlers", "Requests" })
        {
            Assert.False(Directory.Exists(Path.Combine(root, ModuleRoot, "Tooba.Pricing.Application", banned)),
                $"Application/{banned} must not exist: Pricing owns zero endpoint-reachable requests");
        }
    }

    [Fact]
    public void Path_namespace_is_exact_for_every_production_file()
    {
        var root = Repo();
        foreach (var project in ProductionProjects)
        {
            var projectPath = Path.Combine(root, ModuleRoot, project);
            foreach (var file in Directory.GetFiles(projectPath, "*.cs", SearchOption.AllDirectories))
            {
                var relative = file[Path.GetFullPath(projectPath).Length..]
                    .TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                if (relative.StartsWith($"obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                    || relative.StartsWith($"bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                    || Path.GetFileName(relative).StartsWith("GlobalUsings", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var dir = Path.GetDirectoryName(relative);
                var expected = string.IsNullOrEmpty(dir)
                    ? project
                    : project + "." + dir.Replace(Path.DirectorySeparatorChar, '.').Replace(Path.AltDirectorySeparatorChar, '.');
                var match = Regex.Match(
                    File.ReadAllText(file).TrimStart('\uFEFF'),
                    @"^namespace\s+([A-Za-z0-9_.]+)",
                    RegexOptions.Multiline);
                Assert.True(match.Success, $"no namespace in {project}/{relative}");
                Assert.Equal(expected, match.Groups[1].Value);
            }
        }
    }

    [Fact]
    public void Canonical_seams_are_present_and_single_owned()
    {
        var root = Repo();

        // One declared-code home, one KnownCodes guard, exactly 11 declared codes.
        var codes = Read("src/backend/Modules/Pricing/Tooba.Pricing.Contracts/Errors/PricingErrorCodes.cs");
        Assert.Contains("public static bool IsKnown(string? code)", codes, StringComparison.Ordinal);
        Assert.Contains("private static readonly HashSet<string> KnownCodes", codes, StringComparison.Ordinal);
        Assert.Equal(11, Regex.Matches(codes, "public const string ").Count);
        Assert.DoesNotContain("namespace Tooba.Pricing.Domain", codes, StringComparison.Ordinal);
        Assert.False(File.Exists(Path.Combine(root, ModuleRoot, "Tooba.Pricing.Domain", "Errors", "PricingErrorCodes.cs")));

        // The typed-fault seam keeps the certified dual mechanism and never parses message text.
        var operation = Read("src/backend/Modules/Pricing/Tooba.Pricing.Application/Composition/PricingOperation.cs");
        Assert.Contains(
            "catch (ContractOperationException ex) when (PricingErrorCodes.IsKnown(ex.Code))",
            operation,
            StringComparison.Ordinal);
        Assert.Contains("catch (SemanticException ex)", operation, StringComparison.Ordinal);
        Assert.Contains("public static async Task<Result> ExecuteAsync(Func<Task> action)", operation, StringComparison.Ordinal);
        Assert.DoesNotContain("ex.Message", operation, StringComparison.Ordinal);

        // One catalog contributor and one resource set, registered exactly once by the module composition.
        Assert.Equal(
            new[] { "PricingErrorCatalogContributor.cs" },
            Directory.EnumerateFiles(
                    Path.Combine(root, ModuleRoot, "Tooba.Pricing.Contracts", "Errors"), "*Contributor.cs")
                .Select(Path.GetFileName).ToArray());
        Assert.Equal(
            new[] { "PricingErrorResourceSet.cs" },
            Directory.EnumerateFiles(
                    Path.Combine(root, ModuleRoot, "Tooba.Pricing.Contracts", "Errors"), "*ResourceSet.cs")
                .Select(Path.GetFileName).ToArray());
        var module = Read("src/backend/Modules/Pricing/Tooba.Pricing.Infrastructure/DependencyInjection/PricingModule.cs");
        Assert.Contains("IErrorCatalogContributor, PricingErrorCatalogContributor>", module, StringComparison.Ordinal);
        Assert.Contains("IErrorResourceSet, PricingErrorResourceSet>", module, StringComparison.Ordinal);

        // The boundary types are still reachable through their certified path-derived namespaces.
        Assert.Equal("Tooba.Pricing.Contracts.Ports", typeof(IPriceDirectory).Namespace);
        Assert.Equal("Tooba.Pricing.Contracts.Seller", typeof(ISellerOfferPricingGateway).Namespace);
        Assert.Equal("Tooba.Pricing.Contracts.Errors", typeof(PricingErrorCodes).Namespace);
        Assert.Equal("Tooba.Pricing.Contracts", typeof(IPriceDirectory).Assembly.GetName().Name);
    }

    [Fact]
    public void Composed_catalog_has_no_duplicate_pricing_descriptor()
    {
        var contributor = new PricingErrorCatalogContributor();
        var codes = contributor.Contribute().Select(d => d.Code).ToArray();

        Assert.Equal(11, codes.Length);
        Assert.Equal(codes.Length, codes.Distinct(StringComparer.OrdinalIgnoreCase).Count());
        Assert.All(codes, code => Assert.StartsWith("pricing.", code, StringComparison.Ordinal));

        // Descriptor ownership is unique and every descriptor resolves to exactly one canonical owner.
        foreach (var code in codes)
        {
            Assert.True(Catalog.TryGet(code, out var descriptor), "missing descriptor " + code);
            Assert.Equal(code, descriptor.LocalizationKey);
            Assert.True(PricingErrorCodes.IsKnown(code), code);
        }

        // The Pricing-owned keyspace is exclusive: no other module may claim a pricing. descriptor.
        var registeredPricing = Catalog.RegisteredCodes
            .Where(code => code.StartsWith("pricing.", StringComparison.Ordinal))
            .OrderBy(code => code, StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(
            codes.OrderBy(code => code, StringComparer.Ordinal).ToArray(),
            registeredPricing);
    }

    [Fact]
    public void Bilingual_resources_resolve_every_pricing_owned_code_through_the_composed_localizer()
    {
        var contractsRoot = Path.Combine(Repo(), ModuleRoot, "Tooba.Pricing.Contracts");
        Assert.True(File.Exists(Path.Combine(contractsRoot, "Errors", "PricingErrorResourceSet.cs")));
        var en = File.ReadAllText(Path.Combine(contractsRoot, "Resources", "PricingErrors.resx"));
        var fa = File.ReadAllText(Path.Combine(contractsRoot, "Resources", "PricingErrors.fa.resx"));

        var codes = new PricingErrorCatalogContributor().Contribute().Select(d => d.Code).ToArray();
        Assert.Equal(11, codes.Length);

        var cultureEn = CultureInfo.GetCultureInfo("en");
        var cultureFa = CultureInfo.GetCultureInfo("fa");
        foreach (var code in codes)
        {
            Assert.Contains($"\"{code}\"", en, StringComparison.Ordinal);
            Assert.Contains($"\"{code}\"", fa, StringComparison.Ordinal);

            var titleEn = Localizer.Localize(code, cultureEn, new Dictionary<string, string?>(), "fallback");
            var titleFa = Localizer.Localize(code, cultureFa, new Dictionary<string, string?>(), "fallback");
            Assert.NotEqual("fallback", titleEn);
            Assert.NotEqual("fallback", titleFa);
            Assert.True(titleFa.Any(ch => ch is >= '\u0600' and <= '\u06ff'), "expected Persian for " + code);
        }

        // The resource pair is embedded from the Contracts assembly that owns the codes, so the module
        // carries its own user-facing text when extracted as a microservice.
        Assert.True(new PricingErrorResourceSet().Owns("pricing.amount.invalid"));
        Assert.False(new PricingErrorResourceSet().Owns("order.not_found"));
        Assert.Equal(
            "Tooba.Pricing.Contracts",
            typeof(PricingErrorResourceSet).Assembly.GetName().Name);
    }

    [Fact]
    public void Boundaries_stay_contracts_only_and_persistence_is_module_owned()
    {
        var root = Repo();
        var foreignPattern =
            @"Tooba\.(Order|Cart|Catalog|Identity|AccessControl|Content|CustomerProfile|Notification|Media|Fulfillment|Inventory|BulkInquiry|Localization|Settlement|Story|Wishlist|UserPreference|ProductQnA|Reviews|OperatorProfile|PageComposition|Promotion|Tax|Returns|Wallet|Support|AddressBook|Payment|StoreContext)\.(Application|Infrastructure|Domain|Endpoints)";

        foreach (var project in ProductionProjects)
        {
            foreach (var file in Directory.EnumerateFiles(Path.Combine(root, ModuleRoot, project), "*.cs", SearchOption.AllDirectories))
            {
                if (file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                    || file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
                {
                    continue;
                }

                Assert.False(
                    Regex.IsMatch(File.ReadAllText(file), foreignPattern),
                    $"foreign module Application/Infrastructure/Domain/Endpoints reference in {file}");
            }
        }

        // Every outbound project edge is Contracts-only (or internal own-module layering).
        foreach (var project in ProductionProjects)
        {
            foreach (var reference in ProjectRefs(project))
            {
                Assert.DoesNotContain("Host", reference, StringComparison.OrdinalIgnoreCase);
                Assert.False(
                    Regex.IsMatch(reference, @"Tooba\.(?!Pricing\.)[A-Za-z]+\.(Application|Infrastructure|Domain|Endpoints)\.csproj"),
                    $"{project} -> {reference}");
            }
        }

        // Own schema + own DbContext + own outbox + own migration tooling.
        var db = Read("src/backend/Modules/Pricing/Tooba.Pricing.Infrastructure/Persistence/PricingDbContext.cs");
        Assert.Contains("\"pricing\"", db, StringComparison.Ordinal);
        Assert.Contains("DbSet<AuthoredPrice>", db, StringComparison.Ordinal);
        Assert.Equal(1, Regex.Matches(db, @"class PricingDbContext\b").Count);
        Assert.Contains(
            "AddModuleSchemaMigrator(\"Pricing\", ModuleSchemaMigrationOrder.Pricing,",
            Read("src/backend/Modules/Pricing/Tooba.Pricing.Infrastructure/DependencyInjection/PricingModule.cs"),
            StringComparison.Ordinal);

        // The single migration, its designer and the snapshot keep their identities; nothing was regenerated.
        var migrations = Directory
            .EnumerateFiles(Path.Combine(root, ModuleRoot, "Tooba.Pricing.Infrastructure", "Persistence", "Migrations"), "*.cs")
            .Select(Path.GetFileName!)
            .Where(n => !n.EndsWith("Designer.cs", StringComparison.Ordinal)
                        && !n.EndsWith("ModelSnapshot.cs", StringComparison.Ordinal))
            .OrderBy(n => n, StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(new[] { "20260823085546_InitialPricing.cs" }, migrations);

        // The Promotion inbound edge stays Contracts-only and never reaches Pricing.Application.
        foreach (var file in Directory.EnumerateFiles(
                     Path.Combine(root, "src/backend/Modules/Promotion"), "*.csproj", SearchOption.AllDirectories))
        {
            Assert.DoesNotContain("Tooba.Pricing.Application", File.ReadAllText(file), StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Host_final_closure_is_preserved_for_pricing()
    {
        var root = Repo();

        // No Host-owned Pricing business folder and no Host-owned Pricing route.
        Assert.False(Directory.Exists(Path.Combine(root, "src/backend/Host/Tooba.Host/Pricing")));
        var program = Read("src/backend/Host/Tooba.Host/Program.cs");
        Assert.DoesNotContain("MapPricingModule", program, StringComparison.Ordinal);
        Assert.DoesNotContain("AddPricingEndpointPresentation", program, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.Pricing.Endpoints", program, StringComparison.Ordinal);
        Assert.DoesNotContain("MapGroup(\"/v1/pricing\")", program, StringComparison.Ordinal);

        // No Host file may own Pricing persistence or Pricing business policy.
        var hostRoot = Path.Combine(root, "src/backend/Host/Tooba.Host");
        var hostHits = Directory.EnumerateFiles(hostRoot, "*.cs", SearchOption.AllDirectories)
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}")
                           && !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}"))
            .Where(path =>
            {
                var text = File.ReadAllText(path);
                return text.Contains("PricingDbContext", StringComparison.Ordinal)
                       || text.Contains("AuthoredPrice", StringComparison.Ordinal)
                       || text.Contains("IPriceDirectory", StringComparison.Ordinal);
            })
            .ToList();
        Assert.Empty(hostHits);

        // Host final closure flags stay certified.
        using var sot = JsonDocument.Parse(
            File.ReadAllText(Path.Combine(root, "docs/architecture/tmar-current-state.json"))
                .Replace("\uFEFF", string.Empty));
        Assert.Equal("HOST_ROOT_FINAL_CERTIFIED", sot.RootElement.GetProperty("currentHostCheckpoint").GetString());
    }

    private static string Read(string relativePath) =>
        File.ReadAllText(Path.Combine(Repo(), relativePath));

    /// <summary>
    /// Every Pricing <c>.cs</c> outside the Test project and outside <c>bin</c>/<c>obj</c> — i.e. the
    /// production surface the retired ceremony must never reappear on. The Test project is excluded
    /// because the durable guards necessarily name the retired identifiers as negative assertions.
    /// </summary>
    private static IEnumerable<string> ProductionSources(string root)
    {
        var testsRoot = Path.Combine(root, ModuleRoot, "Tooba.Pricing.Tests") + Path.DirectorySeparatorChar;
        return Directory.EnumerateFiles(Path.Combine(root, ModuleRoot), "*.cs", SearchOption.AllDirectories)
            .Where(file => !file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                           && !file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                           && !file.StartsWith(testsRoot, StringComparison.Ordinal));
    }

    private static IReadOnlyList<string> ProjectRefs(string project)
    {
        var doc = System.Xml.Linq.XDocument.Load(Path.Combine(
            Repo(), ModuleRoot, project, $"{project}.csproj"));
        return doc.Descendants("ProjectReference")
            .Select(x => (string?)x.Attribute("Include") ?? string.Empty)
            .Where(x => x.Length > 0)
            .ToArray();
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
