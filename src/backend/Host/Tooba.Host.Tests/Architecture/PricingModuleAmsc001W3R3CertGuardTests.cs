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
/// TB-TMAR-PRICING-AMSC-001-W3-R3 — fresh certification lock (tooba-architecture-certify).
/// <para>
/// Independent re-certification of the INTERNAL_ONLY Pricing surface after the W3-R2 structure repair
/// (commit <c>7159c8f7</c>). The superseded W3 certification (<c>3c2cc61e</c>) claimed
/// <c>NOT_HTTP_OWNING_INTERNAL_CAPABILITY_PROVIDER</c> while the module still carried a ceremonial
/// <c>Tooba.Pricing.Endpoints</c> project, an empty <c>/v1/pricing</c> route group and Host mapping
/// ceremony. This guard pins the fresh truth: five projects (Contracts, Domain, Application,
/// Infrastructure, Tests), zero HTTP routes, zero endpoint-reachable requests, presentation
/// registration exactly once in the Pricing Infrastructure composition root, and Pricing promoted back
/// to the certified manifest set only by this wave.
/// </para>
/// <para>
/// Structural invariants (path↔namespace, root allowlists, capability-first folder granularity,
/// physical copies, solution grouping) are re-asserted here as defense in depth; the structure
/// authority remains <c>TB-TMAR-PRICING-AMSC-001-W3-R2</c> at <c>7159c8f7</c>.
/// </para>
/// </summary>
public sealed class PricingModuleAmsc001W3R3CertGuardTests
{
    private const string ModuleRoot = "src/backend/Modules/Pricing";

    private static readonly string[] ProductionProjects =
    [
        "Tooba.Pricing.Contracts",
        "Tooba.Pricing.Domain",
        "Tooba.Pricing.Application",
        "Tooba.Pricing.Infrastructure",
    ];

    private static readonly string[] ManifestCertifiedModules =
    [
        "AccessControl", "AddressBook", "BulkInquiry", "Cart", "Catalog", "Content", "CustomerProfile",
        "Fulfillment", "Identity", "Inventory", "Localization", "Media", "Notification", "Offer",
        "OperatorProfile", "Order", "PageComposition", "Party", "Payment", "Pricing", "ProductQnA",
        "Settlement", "StoreContext", "Story", "UserPreference", "Wishlist",
    ];

    private static readonly string[] LockCertifiedModules =
    [
        "AccessControl", "AddressBook", "BulkInquiry", "Cart", "Catalog", "Content", "CustomerProfile",
        "Fulfillment", "Identity", "Inventory", "Localization", "Media", "Notification", "Offer",
        "OperatorProfile", "Order", "PageComposition", "Party", "Payment", "Pricing", "ProductQnA",
        "Settlement", "StoreContext", "UserPreference", "Wishlist",
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
    public void Pricing_is_freshly_certified_in_manifest_and_sot()
    {
        var root = Repo();

        using var manifest = JsonDocument.Parse(
            File.ReadAllText(Path.Combine(root, "docs/architecture/tmar-module-structure-manifests.json"))
                .Replace("\uFEFF", string.Empty));

        var certified = manifest.RootElement.GetProperty("modules").EnumerateArray()
            .Select(m => m.GetProperty("module").GetString()!)
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(ManifestCertifiedModules, certified);

        var pricing = manifest.RootElement.GetProperty("modules").EnumerateArray()
            .Single(m => string.Equals(m.GetProperty("module").GetString(), "Pricing", StringComparison.Ordinal));
        Assert.True(pricing.GetProperty("structureCertified").GetBoolean());
        Assert.Equal("ARCH-COMPLETE-002", pricing.GetProperty("lockVersion").GetString());
        Assert.Contains(
            "TB-TMAR-PRICING-AMSC-001-W3-R3",
            pricing.GetProperty("certificationNote").GetString()!,
            StringComparison.Ordinal);
        Assert.Equal("7159c8f773c1faa9b4b6d425b19067f50ca27572",
            pricing.GetProperty("structureAuthorityCommit").GetString());
        Assert.Equal("TB-TMAR-PRICING-AMSC-001-W3-R2",
            pricing.GetProperty("structureAuthorityTask").GetString());

        // Exactly five projects (four production + Tests): the Endpoints project entry is gone.
        var projects = pricing.GetProperty("projects").EnumerateArray()
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

        // The fresh promotion removes the pre-cert duplicate and does not leave an HTTP-owning claim.
        Assert.DoesNotContain(
            manifest.RootElement.GetProperty("preCertModules").EnumerateArray(),
            m => string.Equals(m.GetProperty("module").GetString(), "Pricing", StringComparison.Ordinal));
        Assert.DoesNotContain(
            "Pricing",
            manifest.RootElement.GetProperty("uncertifiedHttpOwningModules").EnumerateArray()
                .Select(x => x.GetString()!),
            StringComparer.Ordinal);

        using var sot = JsonDocument.Parse(
            File.ReadAllText(Path.Combine(root, "docs/architecture/tmar-current-state.json"))
                .Replace("\uFEFF", string.Empty));

        var certifiedInSot = sot.RootElement.GetProperty("structureLock").GetProperty("certifiedModules")
            .EnumerateArray().Select(x => x.GetString()!)
            .OrderBy(x => x, StringComparer.Ordinal).ToArray();
        Assert.Equal(LockCertifiedModules, certifiedInSot);
        Assert.Equal(1, certifiedInSot.Count(x => string.Equals(x, "Pricing", StringComparison.Ordinal)));

        var w3 = sot.RootElement.GetProperty("pricingAmsc001W3R3");
        Assert.Equal("TB-TMAR-PRICING-AMSC-001-W3-R3", w3.GetProperty("task").GetString());
        Assert.Equal("TB-TMAR-PRICING-AMSC-001-W3-R2", w3.GetProperty("parentTask").GetString());
        Assert.Equal("FRESH_CERTIFY_AFTER_INTERNAL_ONLY_STRUCTURE_REPAIR", w3.GetProperty("mode").GetString());
        Assert.Equal("PRICING_AMSC_001_RECERTIFIED", w3.GetProperty("state").GetString());
        Assert.Equal("COMPLETE_REFERENCE_PATTERN", w3.GetProperty("verdict").GetString());
        Assert.True(w3.GetProperty("structureCertified").GetBoolean());
        Assert.Equal("7159c8f773c1faa9b4b6d425b19067f50ca27572", w3.GetProperty("startingHead").GetString());
        Assert.Equal("TB-TMAR-PRICING-AMSC-001-W3-R2", w3.GetProperty("currentStructureAuthority").GetString());
        Assert.Equal("7159c8f773c1faa9b4b6d425b19067f50ca27572", w3.GetProperty("currentStructureCommit").GetString());
        Assert.Equal("TB-TMAR-PRICING-AMSC-001-W3", w3.GetProperty("supersededCertificationTask").GetString());
        Assert.Equal("3c2cc61e7c61813ac72773ccdb8bb16317cafe70",
            w3.GetProperty("supersededCertificationCommit").GetString());
        Assert.Equal("USER_REVIEW_PRICING_AMSC_001_W3_R3", w3.GetProperty("workflowStop").GetString());
        Assert.Equal("NONE", w3.GetProperty("automaticNextImplementationTask").GetString());
        Assert.Equal("POST_CERT_RECOVERY_RECONCILIATION_REQUIRED",
            w3.GetProperty("postCertRecoveryState").GetString());
        Assert.Equal("NONE", w3.GetProperty("guardsWeakened").GetString());
        Assert.Equal("NONE", w3.GetProperty("baselinesWidened").GetString());
        Assert.False(w3.GetProperty("productionCodeChanged").GetBoolean());
        Assert.Equal("PROMOTED_TO_CERTIFIED_MODULES_26_PRICING_ENTRY_REFRESHED",
            w3.GetProperty("manifestCertificationState").GetString());
        Assert.Equal("PRICING_PRESENT_EXACTLY_ONCE", w3.GetProperty("structureLockState").GetString());
        Assert.Equal("ZERO_BUSINESS_ZERO_HTTP", w3.GetProperty("hostAuthorityState").GetString());
        Assert.Equal("PRESERVED", w3.GetProperty("hostFinalClosure").GetString());

        // A self-referential placeholder must never be recorded as the certification commit; the
        // reported SHA lives in the Bridge result and the post-cert reconciliation disclosure.
        Assert.Equal(
            "REPORTED_IN_BRIDGE_RESULT_ONLY_NO_SELF_REFERENTIAL_SOT_SHA",
            w3.GetProperty("certificationCommitState").GetString());
        Assert.False(w3.TryGetProperty("commit", out _));
        Assert.False(w3.TryGetProperty("commitFull", out _));

        // The repository-global Host root checkpoint is not displaced by a module-local certification.
        Assert.Equal("TB-TMAR-HOST-ROOT-FINAL-CERT-001", sot.RootElement.GetProperty("lastAcceptedTask").GetString());
        Assert.Equal("HOST_ROOT_FINAL_CERTIFIED", sot.RootElement.GetProperty("currentHostCheckpoint").GetString());
        Assert.Equal("NONE", sot.RootElement.GetProperty("automaticNextImplementationTask").GetString());
    }

    [Fact]
    public void Internal_only_applicability_holds_zero_routes_and_zero_requests()
    {
        var root = Repo();

        Assert.False(Directory.Exists(Path.Combine(root, ModuleRoot, "Tooba.Pricing.Endpoints")));
        Assert.False(Directory.Exists(Path.Combine(root, ModuleRoot, "Tooba.Pricing.Tests", "Endpoints")));

        // No production Pricing source owns an endpoint surface, a dispatcher, or CQRS ceremony.
        foreach (var file in ProductionSources(root))
        {
            var text = File.ReadAllText(file);
            Assert.DoesNotContain("Tooba.Pricing.Endpoints", text, StringComparison.Ordinal);
            Assert.DoesNotContain("MapPricingModule", text, StringComparison.Ordinal);
            Assert.DoesNotContain("AddPricingEndpointPresentation", text, StringComparison.Ordinal);
            Assert.DoesNotContain("PricingEndpointModule", text, StringComparison.Ordinal);
            Assert.DoesNotContain("IEndpointRouteBuilder", text, StringComparison.Ordinal);
            Assert.DoesNotContain("MapGroup(", text, StringComparison.Ordinal);
            Assert.DoesNotContain("MapGet(", text, StringComparison.Ordinal);
            Assert.DoesNotContain("MapPost(", text, StringComparison.Ordinal);
            Assert.DoesNotContain("MapPut(", text, StringComparison.Ordinal);
            Assert.DoesNotContain("MapPatch(", text, StringComparison.Ordinal);
            Assert.DoesNotContain("MapDelete(", text, StringComparison.Ordinal);
            Assert.DoesNotContain("ISender", text, StringComparison.Ordinal);
            Assert.DoesNotContain("MediatR", text, StringComparison.Ordinal);
            Assert.DoesNotContain("\"/v1/pricing\"", text, StringComparison.Ordinal);
        }

        // No endpoint-reachable request exists, so no CQRS/validator ceremony may be invented.
        var application = Path.Combine(root, ModuleRoot, "Tooba.Pricing.Application");
        Assert.Equal(
            new[] { "Composition", "Ports" },
            Directory.GetDirectories(application)
                .Select(Path.GetFileName!)
                .Where(n => n is not ("bin" or "obj" or "artifacts"))
                .OrderBy(x => x, StringComparer.Ordinal).ToArray());
        foreach (var banned in new[] { "Commands", "Queries", "Validators", "Models", "Handlers", "Requests" })
        {
            Assert.False(Directory.Exists(Path.Combine(application, banned)),
                $"Application/{banned} must not exist: Pricing owns zero endpoint-reachable requests");
        }

        // The Host composition path is retired and Pricing is referenced for composition only.
        var program = Read("src/backend/Host/Tooba.Host/Program.cs");
        Assert.DoesNotContain("MapPricingModule", program, StringComparison.Ordinal);
        Assert.DoesNotContain("AddPricingEndpointPresentation", program, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.Pricing.Endpoints", program, StringComparison.Ordinal);
        Assert.DoesNotContain("\"/v1/pricing\"", program, StringComparison.Ordinal);

        var hostCsproj = Read("src/backend/Host/Tooba.Host/Tooba.Host.csproj");
        Assert.DoesNotContain("Tooba.Pricing.Endpoints", hostCsproj, StringComparison.Ordinal);
        Assert.Contains("Tooba.Pricing.Infrastructure.csproj", hostCsproj, StringComparison.Ordinal);
    }

    [Fact]
    public void Structure_invariants_hold_for_the_five_project_solution_group()
    {
        var root = Repo();

        // Root-Allowlist-State: only Domain carries a root source file (the namespace bridge).
        foreach (var project in ProductionProjects)
        {
            var rootFiles = Directory.GetFiles(Path.Combine(root, ModuleRoot, project), "*.cs", SearchOption.TopDirectoryOnly)
                .Select(Path.GetFileName!)
                .OrderBy(x => x, StringComparer.Ordinal).ToArray();
            var expected = project == "Tooba.Pricing.Domain"
                ? new[] { "GlobalUsings.cs" }
                : Array.Empty<string>();
            Assert.Equal(expected, rootFiles);
        }

        // Folder-Granularity-State: capability-first shallow, no technical-axis-first tree.
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
            new[] { "Adapters", "DependencyInjection", "Events", "Outbox", "Persistence" },
            Directory.GetDirectories(Path.Combine(root, ModuleRoot, "Tooba.Pricing.Infrastructure"))
                .Select(Path.GetFileName!)
                .Where(n => n is not ("bin" or "obj" or "artifacts"))
                .OrderBy(x => x, StringComparer.Ordinal).ToArray());

        // Path-Namespace-State: EXACT for every production file.
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

        // Physical-Copy-State: one authoritative home for the relocated localization surface.
        var contracts = Path.Combine(root, ModuleRoot, "Tooba.Pricing.Contracts");
        Assert.False(File.Exists(Path.Combine(contracts, "PricingErrorCodes.cs")));
        Assert.False(File.Exists(Path.Combine(contracts, "SellerOfferPricingContracts.cs")));
        Assert.False(Directory.Exists(Path.Combine(root, ModuleRoot, "Tooba.Pricing.Domain", "Errors")));

        // Solution-Explorer-State: canonical /Modules/Pricing/ grouping with exactly five projects.
        var slnx = Read("src/backend/Tooba.slnx");
        var folderStart = slnx.IndexOf("<Folder Name=\"/Modules/Pricing/\">", StringComparison.Ordinal);
        Assert.True(folderStart >= 0, "missing /Modules/Pricing/ solution folder");
        var group = slnx[folderStart..slnx.IndexOf("</Folder>", folderStart, StringComparison.Ordinal)];
        Assert.Equal(5, Regex.Matches(group, @"<Project Path=").Count);
        Assert.DoesNotContain("Tooba.Pricing.Endpoints", group, StringComparison.Ordinal);
        foreach (var project in ProductionProjects.Concat(["Tooba.Pricing.Tests"]))
        {
            Assert.Contains($"Modules/Pricing/{project}/{project}.csproj", group, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Stable_codes_descriptors_and_bilingual_resources_are_exact()
    {
        var codes = Read("src/backend/Modules/Pricing/Tooba.Pricing.Contracts/Errors/PricingErrorCodes.cs");
        Assert.Equal(11, Regex.Matches(codes, "public const string ").Count);
        Assert.Contains("private static readonly HashSet<string> KnownCodes", codes, StringComparison.Ordinal);
        Assert.Contains("public static bool IsKnown(string? code)", codes, StringComparison.Ordinal);

        // One declared-code home, one catalog contributor, one resource set.
        var errors = Path.Combine(Repo(), ModuleRoot, "Tooba.Pricing.Contracts", "Errors");
        Assert.Equal(new[] { "PricingErrorCatalogContributor.cs" },
            Directory.EnumerateFiles(errors, "*Contributor.cs").Select(Path.GetFileName).ToArray());
        Assert.Equal(new[] { "PricingErrorResourceSet.cs" },
            Directory.EnumerateFiles(errors, "*ResourceSet.cs").Select(Path.GetFileName).ToArray());

        // Presentation-Registration-State: registered exactly once by the Infrastructure composition root.
        var module = Read("src/backend/Modules/Pricing/Tooba.Pricing.Infrastructure/DependencyInjection/PricingModule.cs");
        Assert.Equal(1, Regex.Matches(module,
            @"AddSingleton<\s*IErrorCatalogContributor,\s*PricingErrorCatalogContributor>").Count);
        Assert.Equal(1, Regex.Matches(module,
            @"AddSingleton<\s*IErrorResourceSet,\s*PricingErrorResourceSet>").Count);
        Assert.Equal("Tooba.Pricing.Contracts",
            typeof(PricingErrorCatalogContributor).Assembly.GetName().Name);
        Assert.Equal("Tooba.Pricing.Contracts.Errors", typeof(PricingErrorCodes).Namespace);
        Assert.Equal("Tooba.Pricing.Contracts.Errors", typeof(PricingErrorResourceSet).Namespace);

        // Composed-catalog uniqueness: 11 descriptors, each resolving to exactly one canonical owner.
        var descriptors = new PricingErrorCatalogContributor().Contribute().ToArray();
        Assert.Equal(11, descriptors.Length);
        Assert.Equal(descriptors.Length, descriptors.Select(d => d.Code).Distinct(StringComparer.OrdinalIgnoreCase).Count());
        foreach (var descriptor in descriptors)
        {
            Assert.StartsWith("pricing.", descriptor.Code, StringComparison.Ordinal);
            Assert.True(Catalog.TryGet(descriptor.Code, out var resolved), "missing descriptor " + descriptor.Code);
            Assert.Equal(descriptor.Code, resolved.LocalizationKey);
            Assert.True(PricingErrorCodes.IsKnown(descriptor.Code), descriptor.Code);
        }

        var registeredPricing = Catalog.RegisteredCodes
            .Where(code => code.StartsWith("pricing.", StringComparison.Ordinal))
            .OrderBy(code => code, StringComparer.Ordinal).ToArray();
        Assert.Equal(descriptors.Select(d => d.Code).OrderBy(x => x, StringComparer.Ordinal).ToArray(), registeredPricing);

        // Localization-State: 11 EN + 11 FA keys with composed bilingual resolution and no fallback.
        var en = Read("src/backend/Modules/Pricing/Tooba.Pricing.Contracts/Resources/PricingErrors.resx");
        var fa = Read("src/backend/Modules/Pricing/Tooba.Pricing.Contracts/Resources/PricingErrors.fa.resx");
        var enKeys = Regex.Matches(en, "<data name=\"(pricing\\.[^\"]+)\"").Select(m => m.Groups[1].Value)
            .OrderBy(x => x, StringComparer.Ordinal).ToArray();
        var faKeys = Regex.Matches(fa, "<data name=\"(pricing\\.[^\"]+)\"").Select(m => m.Groups[1].Value)
            .OrderBy(x => x, StringComparer.Ordinal).ToArray();
        Assert.Equal(11, enKeys.Length);
        Assert.Equal(enKeys, faKeys);

        var cultureEn = CultureInfo.GetCultureInfo("en");
        var cultureFa = CultureInfo.GetCultureInfo("fa");
        foreach (var descriptor in descriptors)
        {
            var titleEn = Localizer.Localize(descriptor.Code, cultureEn, new Dictionary<string, string?>(), "fallback");
            var titleFa = Localizer.Localize(descriptor.Code, cultureFa, new Dictionary<string, string?>(), "fallback");
            Assert.NotEqual("fallback", titleEn);
            Assert.NotEqual("fallback", titleFa);
            Assert.True(titleFa.Any(ch => ch is >= '\u0600' and <= '\u06ff'), "expected Persian for " + descriptor.Code);
        }

        Assert.True(new PricingErrorResourceSet().Owns("pricing.amount.invalid"));
        Assert.False(new PricingErrorResourceSet().Owns("order.not_found"));
    }

    [Fact]
    public void Typed_fault_seam_classifies_by_code_only()
    {
        var operation = Read("src/backend/Modules/Pricing/Tooba.Pricing.Application/Composition/PricingOperation.cs");
        Assert.Equal(
            2,
            Regex.Matches(operation, @"catch \(ContractOperationException ex\) when \(PricingErrorCodes\.IsKnown\(ex\.Code\)\)").Count);
        Assert.Equal(2, Regex.Matches(operation, @"catch \(SemanticException ex\)").Count);
        Assert.Contains("public static async Task<Result<T>> ExecuteAsync<T>", operation, StringComparison.Ordinal);
        Assert.Contains("public static async Task<Result> ExecuteAsync(Func<Task> action)", operation, StringComparison.Ordinal);
        Assert.DoesNotContain("ex.Message", operation, StringComparison.Ordinal);
        Assert.DoesNotContain("StartsWith", operation, StringComparison.Ordinal);
        Assert.DoesNotContain("PlatformHttpException", operation, StringComparison.Ordinal);
        Assert.Equal("Tooba.Pricing.Application.Composition", typeof(PricingOperation).Namespace);

        // Zero message-prose classification anywhere in the Pricing production surface.
        foreach (var file in ProductionSources(Repo()))
        {
            var text = File.ReadAllText(file);
            Assert.DoesNotContain(".Message.Contains(", text, StringComparison.Ordinal);
            Assert.DoesNotContain(".Message.StartsWith(", text, StringComparison.Ordinal);
            Assert.DoesNotContain(".Message ==", text, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Boundaries_stay_contracts_only_and_persistence_stays_module_owned()
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

        // Every outbound project edge is Contracts-only or own-module layering.
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

        // The Promotion inbound edge stays Pricing.Contracts-only.
        foreach (var file in Directory.EnumerateFiles(
                     Path.Combine(root, "src/backend/Modules/Promotion"), "*.csproj", SearchOption.AllDirectories))
        {
            Assert.DoesNotContain("Tooba.Pricing.Application", File.ReadAllText(file), StringComparison.Ordinal);
        }

        // Persistence-State: own pricing schema, own DbContext, own migrator, own outbox, single migration.
        var db = Read("src/backend/Modules/Pricing/Tooba.Pricing.Infrastructure/Persistence/PricingDbContext.cs");
        Assert.Contains("\"pricing\"", db, StringComparison.Ordinal);
        Assert.Contains("DbSet<AuthoredPrice>", db, StringComparison.Ordinal);
        Assert.Equal(1, Regex.Matches(db, @"class PricingDbContext\b").Count);
        Assert.Contains(
            "AddModuleSchemaMigrator(\"Pricing\", ModuleSchemaMigrationOrder.Pricing,",
            Read("src/backend/Modules/Pricing/Tooba.Pricing.Infrastructure/DependencyInjection/PricingModule.cs"),
            StringComparison.Ordinal);

        var migrations = Directory
            .EnumerateFiles(Path.Combine(root, ModuleRoot, "Tooba.Pricing.Infrastructure", "Persistence", "Migrations"), "*.cs")
            .Select(Path.GetFileName!)
            .Where(n => !n.EndsWith("Designer.cs", StringComparison.Ordinal)
                        && !n.EndsWith("ModelSnapshot.cs", StringComparison.Ordinal))
            .OrderBy(n => n, StringComparer.Ordinal).ToArray();
        Assert.Equal(new[] { "20260823085546_InitialPricing.cs" }, migrations);
    }

    [Fact]
    public void Host_authority_is_zero_and_global_closure_is_preserved()
    {
        var root = Repo();

        Assert.False(Directory.Exists(Path.Combine(root, "src/backend/Host/Tooba.Host/Pricing")));

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

        // Global Host final closure flags stay certified.
        using var sot = JsonDocument.Parse(
            File.ReadAllText(Path.Combine(root, "docs/architecture/tmar-current-state.json"))
                .Replace("\uFEFF", string.Empty));
        Assert.Equal("HOST_ROOT_FINAL_CERTIFIED", sot.RootElement.GetProperty("currentHostCheckpoint").GetString());
        Assert.Equal("TB-TMAR-HOST-ROOT-FINAL-CERT-001", sot.RootElement.GetProperty("lastAcceptedTask").GetString());
    }

    [Fact]
    public void Structure_authority_and_superseded_lineage_are_recorded()
    {
        var recovery = File.ReadAllText(Path.Combine(Repo(), "docs/architecture/TOOBA-TMAR-MASTER-RECOVERY.md"));
        Assert.Contains("TB-TMAR-PRICING-AMSC-001-W0` Analyze `08d47b6a`", recovery, StringComparison.Ordinal);
        Assert.Contains("TB-TMAR-PRICING-AMSC-001-W1` Migrate `069f77d2`", recovery, StringComparison.Ordinal);
        Assert.Contains("TB-TMAR-PRICING-AMSC-001-W2` Structure `f7f6abfe`", recovery, StringComparison.Ordinal);
        Assert.Contains("TB-TMAR-PRICING-AMSC-001-W3` Certify `3c2cc61e`", recovery, StringComparison.Ordinal);
        Assert.Contains("Pricing AMSC W3-R2 structure repair (module-local)", recovery, StringComparison.Ordinal);
        Assert.Contains("Pricing AMSC W3-R3 fresh certification (module-local)", recovery, StringComparison.Ordinal);
        Assert.Contains("POST_CERT_RECOVERY_RECONCILIATION_REQUIRED", recovery, StringComparison.Ordinal);
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

    private static string Read(string relativePath) =>
        File.ReadAllText(Path.Combine(Repo(), relativePath));

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
}
