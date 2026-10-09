using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Tooba.BuildingBlocks.Localization;
using Tooba.BuildingBlocks.Presentation.Errors;
using Tooba.Tax.Application.Composition;
using Tooba.Tax.Contracts.Errors;
using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>
/// TB-TMAR-TAX-AMSC-001-W3 — durable ARCH-COMPLETE-002 certification lock for Tax
/// (<c>tooba-architecture-certify</c>).
/// <para>
/// Tax is <c>INTERNAL_ONLY</c>: the W0 applicability gate proved zero module HTTP routes and zero
/// endpoint-reachable requests, so the certify §0b applicability gate makes endpoint ownership,
/// CQRS and the validator matrix <c>NOT_APPLICABLE_INTERNAL_ONLY</c> while <b>forbidding</b> any
/// ceremonial Endpoints project, empty route group, Host endpoint-presentation registration or
/// unused CQRS/validator tree kept for framework symmetry. The W2 structure wave retired exactly
/// that ceremony, so this wave promotes Tax from <c>preCertModules</c> into the certified
/// <c>modules[]</c> array.
/// </para>
/// <para>
/// The guard pins the SoT/manifest certification records with the AMSC wave lineage, the
/// INTERNAL_ONLY physical shape, the single canonical stable-code owner with bilingual resources,
/// the typed-fault composition seam, the Contracts-only microservice-extractable boundary, the
/// unchanged single migration, the exact path↔namespace rule, the canonical
/// <c>/Modules/Tax/</c> solution grouping, and the preserved Host final closure. Structural
/// invariants are re-asserted here as defense in depth; the structure authority remains
/// <c>TB-TMAR-TAX-AMSC-001-W2</c>.
/// </para>
/// </summary>
public sealed class TaxModuleAmsc001W3CertGuardTests
{
    private const string ModuleRoot = "src/backend/Modules/Tax";
    private const string ManifestPath = "docs/architecture/tmar-module-structure-manifests.json";
    private const string StatePath = "docs/architecture/tmar-current-state.json";

    private static readonly string[] ProductionProjects =
    [
        "Tooba.Tax.Contracts",
        "Tooba.Tax.Domain",
        "Tooba.Tax.Application",
        "Tooba.Tax.Infrastructure",
    ];

    /// <summary>The exact certified set after the Tax promotion (31 members, Tax exactly once).</summary>
    private static readonly string[] LockCertifiedModules =
    [
        "AccessControl", "AddressBook", "BulkInquiry", "Cart", "Catalog", "Content", "CustomerProfile",
        "Fulfillment", "Identity", "Inventory", "Localization", "Media", "Notification", "Offer",
        "OperatorProfile", "Order", "PageComposition", "Party", "Payment", "Pricing", "ProductQnA",
        "ProductWorkspace", "Promotion", "Returns", "Settlement", "StoreContext", "Story", "Support",
        "Tax", "UserPreference", "Wishlist",
    ];

    /// <summary>The eleven Tax-owned stable machine codes, in the declared order of the code home.</summary>
    private static readonly string[] DeclaredCodes =
    [
        "tax.rule.id_required",
        "tax.jurisdiction.required",
        "tax.market.required",
        "tax.validity.inverted",
        "tax.rate.out_of_range",
        "tax.rate.not_applicable",
        "tax.rate.kind_mismatch",
        "tax.category.id_required",
        "tax.category.code_required",
        "tax.category.missing",
        "tax.outbox.unmapped_event_type",
    ];

    private static readonly ErrorDefinitionCatalog Catalog = new(
    [
        new FoundationErrorCatalogContributor(),
        new TaxErrorCatalogContributor(),
    ]);

    private static readonly ResourceErrorMessageLocalizer Localizer = new(
        [new FoundationErrorResourceSet(), new TaxErrorResourceSet()],
        Array.Empty<IErrorMessageContributor>());

    // ---------------------------------------------------------------------------------------------
    // 1. Certification truth (manifest promotion + SoT + wave lineage)
    // ---------------------------------------------------------------------------------------------

    [Fact]
    public void Tax_is_arch_complete_002_certified_in_manifest_and_sot()
    {
        var root = Repo();

        using var manifest = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, ManifestPath))
            .Replace("\uFEFF", string.Empty));

        var entries = manifest.RootElement.GetProperty("modules").EnumerateArray()
            .Where(m => string.Equals(m.GetProperty("module").GetString(), "Tax", StringComparison.Ordinal))
            .ToArray();
        Assert.Single(entries);
        Assert.True(entries[0].GetProperty("structureCertified").GetBoolean());
        Assert.Equal("ARCH-COMPLETE-002", entries[0].GetProperty("lockVersion").GetString());
        Assert.Contains(
            "TB-TMAR-TAX-AMSC-001-W3",
            entries[0].GetProperty("certificationNote").GetString()!,
            StringComparison.Ordinal);

        // Exactly five projects (four production + Tests): the ceremonial Endpoints project is gone.
        var projects = entries[0].GetProperty("projects").EnumerateArray()
            .Select(p => p.GetProperty("projectName").GetString()!)
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(
            new[]
            {
                "Tooba.Tax.Application",
                "Tooba.Tax.Contracts",
                "Tooba.Tax.Domain",
                "Tooba.Tax.Infrastructure",
                "Tooba.Tax.Tests",
            },
            projects);

        // Promotion is a move, not a copy.
        Assert.DoesNotContain(
            manifest.RootElement.GetProperty("preCertModules").EnumerateArray(),
            m => string.Equals(m.GetProperty("module").GetString(), "Tax", StringComparison.Ordinal));
        Assert.DoesNotContain(
            "Tax",
            manifest.RootElement.GetProperty("uncertifiedHttpOwningModules").EnumerateArray()
                .Select(x => x.GetString()!),
            StringComparer.Ordinal);

        using var sot = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, StatePath))
            .Replace("\uFEFF", string.Empty));

        var certified = sot.RootElement.GetProperty("structureLock").GetProperty("certifiedModules")
            .EnumerateArray().Select(x => x.GetString()!)
            .OrderBy(x => x, StringComparer.Ordinal).ToArray();
        Assert.Equal(LockCertifiedModules, certified);
        Assert.Equal(31, certified.Length);
        Assert.Equal(1, certified.Count(x => string.Equals(x, "Tax", StringComparison.Ordinal)));

        var w3 = sot.RootElement.GetProperty("taxAmsc001W3");
        Assert.Equal("TB-TMAR-TAX-AMSC-001-W3", w3.GetProperty("task").GetString());
        Assert.Equal("TB-TMAR-TAX-AMSC-001-W2", w3.GetProperty("parentTask").GetString());
        Assert.Equal("TAX_AMSC_001_CERTIFIED", w3.GetProperty("state").GetString());
        Assert.Equal("COMPLETE_REFERENCE_PATTERN", w3.GetProperty("verdict").GetString());
        Assert.Equal("ARCH-COMPLETE-002", w3.GetProperty("lockVersion").GetString());
        Assert.True(w3.GetProperty("structureCertified").GetBoolean());
        Assert.Equal("INTERNAL_ONLY", w3.GetProperty("httpApplicability").GetString());
        Assert.Equal("NOT_APPLICABLE_INTERNAL_ONLY", w3.GetProperty("endpointOwnershipState").GetString());
        Assert.Equal(0, w3.GetProperty("moduleOwnedRouteCount").GetInt32());
        Assert.Equal(0, w3.GetProperty("hostOwnedRouteCount").GetInt32());
        Assert.Equal(0, w3.GetProperty("endpointReachableRequests").GetInt32());
        Assert.Equal("NOT_APPLICABLE_INTERNAL_ONLY", w3.GetProperty("cqrsState").GetString());
        Assert.Equal("EXHAUSTIVE_0_OF_0_NO_VALIDATOR_REQUIRED", w3.GetProperty("validatorMatrixState").GetString());
        Assert.Equal("EXACT", w3.GetProperty("pathNamespaceState").GetString());
        Assert.Equal("ENFORCED", w3.GetProperty("rootAllowlistState").GetString());
        Assert.Equal("NONE", w3.GetProperty("aliasWorkaroundState").GetString());
        Assert.Equal("PROFESSIONAL_SHALLOW", w3.GetProperty("folderGranularityState").GetString());
        Assert.Equal("CANONICAL", w3.GetProperty("solutionExplorerState").GetString());
        Assert.Equal("CLEAN", w3.GetProperty("physicalCopyState").GetString());
        Assert.Equal("ZERO", w3.GetProperty("foreignApplicationInfrastructureDomainEdges").GetString());
        Assert.Equal("ZERO", w3.GetProperty("crossModuleJoinState").GetString());
        Assert.Equal("UNCHANGED", w3.GetProperty("schemaState").GetString());
        Assert.True(w3.GetProperty("microserviceExtractable").GetBoolean());
        Assert.Equal("HOST_TMAR_EVACUATION_FINAL_CLOSURE_CERTIFIED_PRESERVED", w3.GetProperty("hostFinalClosureState").GetString());
        Assert.Equal("NONE", w3.GetProperty("guardsWeakened").GetString());
        Assert.Equal("NONE", w3.GetProperty("baselinesWidened").GetString());
        Assert.Equal("NONE", w3.GetProperty("automaticNextImplementationTask").GetString());
        Assert.Equal("USER_REVIEW_TAX_AMSC_001_W3", w3.GetProperty("stopGate").GetString());

        // Wave lineage: each wave's starting head is the parent wave's commit, so the chain is
        // verifiable end to end.
        Assert.Equal("36d243cc", sot.RootElement.GetProperty("taxAmsc001W0").GetProperty("commit").GetString());
        Assert.Equal("fa87201a", sot.RootElement.GetProperty("taxAmsc001W1").GetProperty("commit").GetString());
        Assert.Equal("eff3cf5b", sot.RootElement.GetProperty("taxAmsc001W2").GetProperty("commit").GetString());
        Assert.Equal("eff3cf5b846d33f90652ee691fa23048ffa45f48", w3.GetProperty("startingHead").GetString());
        Assert.Equal("TB-TMAR-TAX-AMSC-001-W2", w3.GetProperty("currentStructureAuthority").GetString());
        Assert.Equal("READY_FOR_CERTIFY_CONSUMED_BY_W3", w3.GetProperty("currentStructureState").GetString());
    }

    // ---------------------------------------------------------------------------------------------
    // 2. INTERNAL_ONLY applicability — zero routes, no ceremony (certify §0b)
    // ---------------------------------------------------------------------------------------------

    [Fact]
    public void Tax_is_internal_only_with_zero_routes_and_no_endpoint_ceremony()
    {
        var root = Repo();

        Assert.False(Directory.Exists(Path.Combine(root, ModuleRoot, "Tooba.Tax.Endpoints")));
        Assert.False(Directory.Exists(Path.Combine(root, ModuleRoot, "Tooba.Tax.Tests", "Endpoints")));
        Assert.False(Directory.Exists(Path.Combine(root, "src/backend/Host/Tooba.Host/Tax")));

        foreach (var file in ProductionSources(root))
        {
            var text = File.ReadAllText(file);
            Assert.DoesNotContain("Tooba.Tax.Endpoints", text, StringComparison.Ordinal);
            Assert.DoesNotContain("MapTaxModule", text, StringComparison.Ordinal);
            Assert.DoesNotContain("AddTaxEndpointPresentation", text, StringComparison.Ordinal);
            Assert.DoesNotContain("TaxEndpointModule", text, StringComparison.Ordinal);
            Assert.DoesNotContain("IEndpointRouteBuilder", text, StringComparison.Ordinal);
            Assert.DoesNotContain("MapGroup(", text, StringComparison.Ordinal);
            Assert.DoesNotContain("MapGet(", text, StringComparison.Ordinal);
            Assert.DoesNotContain("MapPost(", text, StringComparison.Ordinal);
            Assert.DoesNotContain("MapPut(", text, StringComparison.Ordinal);
            Assert.DoesNotContain("MapPatch(", text, StringComparison.Ordinal);
            Assert.DoesNotContain("MapDelete(", text, StringComparison.Ordinal);
            Assert.DoesNotContain("ISender", text, StringComparison.Ordinal);
            Assert.DoesNotContain("MediatR", text, StringComparison.Ordinal);
            Assert.DoesNotContain("\"/v1/tax\"", text, StringComparison.Ordinal);
        }

        // Zero endpoint-reachable requests, so no CQRS/validator ceremony may be invented.
        var application = Path.Combine(root, ModuleRoot, "Tooba.Tax.Application");
        Assert.Equal(
            new[] { "Composition", "Ports" },
            Directory.GetDirectories(application)
                .Select(Path.GetFileName!)
                .Where(n => n is not ("bin" or "obj" or "artifacts"))
                .OrderBy(x => x, StringComparer.Ordinal).ToArray());
        foreach (var banned in new[] { "Commands", "Queries", "Validators", "Models", "Handlers", "Requests" })
        {
            Assert.False(Directory.Exists(Path.Combine(application, banned)),
                $"Application/{banned} must not exist: Tax owns zero endpoint-reachable requests");
        }

        // The Host composition path is retired; Tax is referenced for composition only.
        var program = Read(root, "src/backend/Host/Tooba.Host/Program.cs");
        Assert.DoesNotContain("MapTaxModule", program, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.Tax.Endpoints", program, StringComparison.Ordinal);
        Assert.DoesNotContain("\"/v1/tax\"", program, StringComparison.Ordinal);

        var hostCsproj = Read(root, "src/backend/Host/Tooba.Host/Tooba.Host.csproj");
        Assert.DoesNotContain("Tooba.Tax.Endpoints", hostCsproj, StringComparison.Ordinal);
        Assert.Contains("Tooba.Tax.Infrastructure.csproj", hostCsproj, StringComparison.Ordinal);
    }

    // ---------------------------------------------------------------------------------------------
    // 3. Stable codes / descriptors / bilingual resources (canonical localization)
    // ---------------------------------------------------------------------------------------------

    [Fact]
    public void Tax_error_surface_has_one_stable_code_owner_and_bilingual_resources()
    {
        var root = Repo();
        var errors = Path.Combine(root, ModuleRoot, "Tooba.Tax.Contracts", "Errors");

        var codesFile = File.ReadAllText(Path.Combine(errors, "TaxErrorCodes.cs"));
        var declared = Regex.Matches(codesFile, "public const string \\w+ = \"(?<c>[^\"]+)\"")
            .Select(m => m.Groups["c"].Value).ToArray();
        Assert.Equal(DeclaredCodes, declared);
        Assert.All(declared, code => Assert.StartsWith("tax.", code, StringComparison.Ordinal));
        Assert.Contains("private static readonly HashSet<string> KnownCodes", codesFile, StringComparison.Ordinal);
        Assert.Contains("public static bool IsKnown(string? code)", codesFile, StringComparison.Ordinal);

        // Exactly one declared-code home, one catalog contributor, one resource set.
        Assert.Equal(new[] { "TaxErrorCatalogContributor.cs" },
            Directory.EnumerateFiles(errors, "*Contributor.cs").Select(Path.GetFileName!).OrderBy(x => x, StringComparer.Ordinal).ToArray());
        Assert.Equal(new[] { "TaxErrorResourceSet.cs" },
            Directory.EnumerateFiles(errors, "*ResourceSet.cs").Select(Path.GetFileName!).OrderBy(x => x, StringComparer.Ordinal).ToArray());

        // One descriptor per declared code, each resolving to exactly one canonical owner.
        var contributor = File.ReadAllText(Path.Combine(errors, "TaxErrorCatalogContributor.cs"));
        var descriptorNames = Regex.Matches(contributor, @"D\(TaxErrorCodes\.(?<n>\w+)")
            .Select(m => m.Groups["n"].Value).ToArray();
        var declaredNames = Regex.Matches(codesFile, "public const string (?<n>\\w+)")
            .Select(m => m.Groups["n"].Value).ToArray();
        Assert.Equal(11, descriptorNames.Length);
        Assert.Equal(declaredNames.Length, descriptorNames.Distinct(StringComparer.Ordinal).Count());
        foreach (var name in declaredNames)
        {
            Assert.Contains(name, descriptorNames);
        }

        // Order owns checkout.tax.unavailable; Tax must never claim that descriptor. (The code home
        // and the contributor mention the code only in explanatory prose, never as a declared code,
        // a descriptor or a resource key, so ownership is asserted against the real artifacts.)
        Assert.DoesNotContain("checkout.tax.unavailable", declared);
        Assert.DoesNotContain("checkout.tax.unavailable", descriptorNames);

        var descriptors = new TaxErrorCatalogContributor().Contribute().ToArray();
        Assert.Equal(11, descriptors.Length);
        foreach (var descriptor in descriptors)
        {
            Assert.StartsWith("tax.", descriptor.Code, StringComparison.Ordinal);
            Assert.Equal(descriptor.Code, descriptor.LocalizationKey);
            Assert.True(TaxErrorCodes.IsKnown(descriptor.Code), descriptor.Code);
            Assert.True(Catalog.TryGet(descriptor.Code, out var resolved), "missing descriptor " + descriptor.Code);
            Assert.Equal(descriptor.Code, resolved.LocalizationKey);
        }

        var registeredTax = Catalog.RegisteredCodes
            .Where(code => code.StartsWith("tax.", StringComparison.Ordinal))
            .OrderBy(code => code, StringComparer.Ordinal).ToArray();
        Assert.Equal(descriptors.Select(d => d.Code).OrderBy(x => x, StringComparer.Ordinal).ToArray(), registeredTax);

        // Bilingual resources: 11 EN + 11 FA keys with composed resolution and real Persian text.
        foreach (var resource in new[] { "TaxErrors.resx", "TaxErrors.fa.resx" })
        {
            var resx = File.ReadAllText(Path.Combine(root, ModuleRoot, "Tooba.Tax.Contracts", "Resources", resource));
            var keys = Regex.Matches(resx, "data name=\"([^\"]+)\"")
                .Select(m => m.Groups[1].Value).OrderBy(x => x, StringComparer.Ordinal).ToArray();
            Assert.Equal(DeclaredCodes.OrderBy(x => x, StringComparer.Ordinal).ToArray(), keys);
        }

        var cultureEn = CultureInfo.GetCultureInfo("en");
        var cultureFa = CultureInfo.GetCultureInfo("fa");
        foreach (var code in DeclaredCodes)
        {
            var titleEn = Localizer.Localize(code, cultureEn, new Dictionary<string, string?>(), "fallback");
            var titleFa = Localizer.Localize(code, cultureFa, new Dictionary<string, string?>(), "fallback");
            Assert.NotEqual("fallback", titleEn);
            Assert.NotEqual("fallback", titleFa);
            Assert.True(titleFa.Any(ch => ch is >= '\u0600' and <= '\u06ff'), "expected Persian for " + code);
        }

        Assert.True(new TaxErrorResourceSet().Owns("tax.rule.id_required"));
        Assert.False(new TaxErrorResourceSet().Owns("order.not_found"));
    }

    // ---------------------------------------------------------------------------------------------
    // 4. Canonical typed-fault seam + single presentation registration
    // ---------------------------------------------------------------------------------------------

    [Fact]
    public void Tax_typed_fault_seam_and_catalog_registration_are_canonical()
    {
        var root = Repo();

        var seam = File.ReadAllText(Path.Combine(
            root, ModuleRoot, "Tooba.Tax.Application", "Composition", "TaxOperation.cs"));
        Assert.Equal(
            2,
            Regex.Matches(seam, @"catch \(ContractOperationException ex\) when \(TaxErrorCodes\.IsKnown\(ex\.Code\)\)").Count);
        Assert.Equal(2, Regex.Matches(seam, @"catch \(SemanticException ex\)").Count);
        Assert.Contains("public static async Task<Result<T>> ExecuteAsync<T>", seam, StringComparison.Ordinal);
        Assert.Contains("public static async Task<Result> ExecuteAsync(Func<Task> action)", seam, StringComparison.Ordinal);
        Assert.DoesNotContain("ex.Message", seam, StringComparison.Ordinal);
        Assert.DoesNotContain("StartsWith", seam, StringComparison.Ordinal);
        Assert.DoesNotContain("IResult", seam, StringComparison.Ordinal);
        Assert.Equal("Tooba.Tax.Application.Composition", typeof(TaxOperation).Namespace);

        // Zero message-prose classification anywhere in the Tax production surface.
        foreach (var file in ProductionSources(root))
        {
            var text = File.ReadAllText(file);
            Assert.DoesNotContain(".Message.Contains(", text, StringComparison.Ordinal);
            Assert.DoesNotContain(".Message.StartsWith(", text, StringComparison.Ordinal);
            Assert.DoesNotContain(".Message ==", text, StringComparison.Ordinal);
            Assert.DoesNotContain("InvalidOperationException(\"", text, StringComparison.Ordinal);
        }

        // INTERNAL_ONLY: the presentation concerns are registered exactly once by the Infrastructure
        // composition root (the certified Inventory/Pricing precedent), never by a fake Endpoints surface.
        var module = File.ReadAllText(Path.Combine(
            root, ModuleRoot, "Tooba.Tax.Infrastructure", "DependencyInjection", "TaxModule.cs"));
        Assert.Equal(1, Regex.Matches(module,
            @"AddSingleton<\s*IErrorCatalogContributor,\s*TaxErrorCatalogContributor>").Count);
        Assert.Equal(1, Regex.Matches(module,
            @"AddSingleton<\s*IErrorResourceSet,\s*TaxErrorResourceSet>").Count);
        Assert.Equal("Tooba.Tax.Contracts", typeof(TaxErrorCatalogContributor).Assembly.GetName().Name);
        Assert.Equal("Tooba.Tax.Contracts.Errors", typeof(TaxErrorCodes).Namespace);
        Assert.Equal("Tooba.Tax.Contracts.Errors", typeof(TaxErrorResourceSet).Namespace);
    }

    // ---------------------------------------------------------------------------------------------
    // 5. Contracts-only microservice boundary
    // ---------------------------------------------------------------------------------------------

    [Fact]
    public void Tax_is_contracts_only_and_microservice_extractable()
    {
        var root = Repo();

        foreach (var project in ProductionProjects)
        {
            var csproj = XDocument.Load(Path.Combine(root, ModuleRoot, project, project + ".csproj"));
            foreach (var reference in csproj.Descendants("ProjectReference")
                         .Select(x => ((string?)x.Attribute("Include") ?? string.Empty).Replace('\\', '/')))
            {
                if (reference.Contains("Tooba.Tax.", StringComparison.Ordinal)
                    || reference.Contains("Tooba.BuildingBlocks", StringComparison.Ordinal)
                    || reference.Contains("Tooba.ModuleContracts", StringComparison.Ordinal)
                    || reference.Contains("Tooba.Persistence", StringComparison.Ordinal))
                {
                    continue;
                }

                Assert.Fail($"{project} carries an unexpected project edge {reference}");
            }
        }

        // Zero foreign module Application/Infrastructure/Domain/Endpoints coupling.
        var joined = string.Join("\n", ProductionSources(root).Select(File.ReadAllText));
        var foreignPattern =
            @"Tooba\.(Order|Cart|Catalog|Identity|AccessControl|Content|CustomerProfile|Notification|Media|Fulfillment|Inventory|BulkInquiry|Localization|Settlement|Story|Wishlist|UserPreference|ProductQnA|Reviews|OperatorProfile|PageComposition|Promotion|Returns|Wallet|Support|AddressBook|Payment|StoreContext|Pricing|Offer|Party)\.(Application|Infrastructure|Domain|Endpoints)";
        Assert.False(Regex.IsMatch(joined, foreignPattern), "foreign module coupling found in Tax production");

        foreach (var foreignContext in new[]
                 {
                     "OrderDbContext", "CatalogDbContext", "PricingDbContext", "PaymentDbContext",
                     "WalletDbContext", "InventoryDbContext", "PartyDbContext", "OfferDbContext",
                     "SupportDbContext", "NotificationDbContext", "AccessControlDbContext",
                 })
        {
            Assert.DoesNotContain(foreignContext, joined, StringComparison.Ordinal);
        }

        Assert.DoesNotContain("TypeForwardedTo", joined, StringComparison.Ordinal);
        Assert.DoesNotContain("namespace Tooba.Tax.Contracts;", joined, StringComparison.Ordinal);
        Assert.DoesNotContain("namespace Tooba.Tax.Domain;", joined, StringComparison.Ordinal);
    }

    // ---------------------------------------------------------------------------------------------
    // 6. Persistence / schema preservation
    // ---------------------------------------------------------------------------------------------

    [Fact]
    public void Tax_schema_and_single_migration_are_preserved()
    {
        var root = Repo();

        var context = File.ReadAllText(Path.Combine(
            root, ModuleRoot, "Tooba.Tax.Infrastructure", "Persistence", "TaxDbContext.cs"));
        Assert.Contains("public const string Schema = \"tax\"", context, StringComparison.Ordinal);
        Assert.Contains("HasDefaultSchema(Schema)", context, StringComparison.Ordinal);
        Assert.Equal(1, Regex.Matches(context, @"class TaxDbContext\b").Count);

        var migrations = Directory.EnumerateFiles(
                Path.Combine(root, ModuleRoot, "Tooba.Tax.Infrastructure", "Persistence", "Migrations"),
                "*.cs", SearchOption.TopDirectoryOnly)
            .Select(Path.GetFileName).OrderBy(n => n, StringComparer.Ordinal).ToArray();
        Assert.Equal(
            [
                "20260823190000_InitialTax.Designer.cs",
                "20260823190000_InitialTax.cs",
                "TaxDbContextModelSnapshot.cs",
            ],
            migrations);

        // No migration may appear outside the canonical Persistence/Migrations folder.
        Assert.False(Directory.Exists(Path.Combine(root, ModuleRoot, "Tooba.Tax.Infrastructure", "Migrations")));

        // The four outbox integration event names are the locked shipped contract.
        var events = File.ReadAllText(Path.Combine(
            root, ModuleRoot, "Tooba.Tax.Infrastructure", "Events", "TaxEvents.cs"));
        foreach (var name in new[]
                 {
                     "tax.rule_created.v1", "tax.rule_activated.v1",
                     "tax.rule_changed.v1", "tax.calculation_failed.v1",
                 })
        {
            Assert.Contains(name, events, StringComparison.Ordinal);
        }
    }

    // ---------------------------------------------------------------------------------------------
    // 7. Structure invariants (defense in depth; authority stays W2)
    // ---------------------------------------------------------------------------------------------

    [Fact]
    public void Tax_structure_invariants_hold_for_the_five_project_solution_group()
    {
        var root = Repo();

        // Root-Allowlist-State: only Domain carries a root source file (the namespace bridge).
        foreach (var project in ProductionProjects)
        {
            var rootFiles = Directory.GetFiles(Path.Combine(root, ModuleRoot, project), "*.cs", SearchOption.TopDirectoryOnly)
                .Select(Path.GetFileName!).OrderBy(x => x, StringComparer.Ordinal).ToArray();
            var expected = project == "Tooba.Tax.Domain"
                ? new[] { "GlobalUsings.cs" }
                : Array.Empty<string>();
            Assert.Equal(expected, rootFiles);
        }

        // Folder-Granularity-State: capability-first shallow, no technical-axis-first tree.
        Assert.Equal(
            new[] { "Dtos", "Errors", "Ports", "Resources" },
            Directory.GetDirectories(Path.Combine(root, ModuleRoot, "Tooba.Tax.Contracts"))
                .Select(Path.GetFileName!).Where(n => n is not ("bin" or "obj" or "artifacts"))
                .OrderBy(x => x, StringComparer.Ordinal).ToArray());
        Assert.Equal(
            new[] { "Aggregates", "Enums", "Events", "Policies" },
            Directory.GetDirectories(Path.Combine(root, ModuleRoot, "Tooba.Tax.Domain"))
                .Select(Path.GetFileName!).Where(n => n is not ("bin" or "obj" or "artifacts"))
                .OrderBy(x => x, StringComparer.Ordinal).ToArray());
        Assert.Equal(
            new[] { "Adapters", "DependencyInjection", "Events", "Outbox", "Persistence" },
            Directory.GetDirectories(Path.Combine(root, ModuleRoot, "Tooba.Tax.Infrastructure"))
                .Select(Path.GetFileName!).Where(n => n is not ("bin" or "obj" or "artifacts"))
                .OrderBy(x => x, StringComparer.Ordinal).ToArray());

        // Path-Namespace-State: EXACT for every production file (GlobalUsings declares no namespace).
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

        // Physical-Copy-State: one authoritative home for the error/localization surface.
        var contracts = Path.Combine(root, ModuleRoot, "Tooba.Tax.Contracts");
        Assert.False(File.Exists(Path.Combine(contracts, "TaxErrorCodes.cs")));
        Assert.False(Directory.Exists(Path.Combine(root, ModuleRoot, "Tooba.Tax.Domain", "Errors")));

        // Solution-Explorer-State: canonical /Modules/Tax/ grouping with exactly five projects.
        var slnx = File.ReadAllText(Path.Combine(root, "src/backend/Tooba.slnx"));
        var folderStart = slnx.IndexOf("<Folder Name=\"/Modules/Tax/\">", StringComparison.Ordinal);
        Assert.True(folderStart >= 0, "missing /Modules/Tax/ solution folder");
        var group = slnx[folderStart..slnx.IndexOf("</Folder>", folderStart, StringComparison.Ordinal)];
        Assert.Equal(5, Regex.Matches(group, @"<Project Path=").Count);
        Assert.DoesNotContain("Tooba.Tax.Endpoints", group, StringComparison.Ordinal);
        foreach (var project in ProductionProjects.Concat(["Tooba.Tax.Tests"]))
        {
            Assert.Contains($"Modules/Tax/{project}/{project}.csproj", group, StringComparison.Ordinal);
        }

        // The certified manifest entry's allowlists/forbidden lists still reconcile with disk.
        using var manifest = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, ManifestPath))
            .Replace("\uFEFF", string.Empty));
        var entry = manifest.RootElement.GetProperty("modules").EnumerateArray()
            .Single(m => string.Equals(m.GetProperty("module").GetString(), "Tax", StringComparison.Ordinal));
        foreach (var project in entry.GetProperty("projects").EnumerateArray())
        {
            var projectName = project.GetProperty("projectName").GetString()!;
            var projectPath = Path.Combine(root, ModuleRoot, projectName);
            var allowlist = project.GetProperty("rootAllowlist").EnumerateArray().Select(x => x.GetString()!)
                .OrderBy(x => x, StringComparer.Ordinal).ToArray();
            var actualRoot = Directory.GetFiles(projectPath, "*.cs", SearchOption.TopDirectoryOnly)
                .Select(Path.GetFileName!).OrderBy(x => x, StringComparer.Ordinal).ToArray();
            Assert.Equal(allowlist, actualRoot);

            foreach (var forbidden in project.GetProperty("forbiddenRootFiles").EnumerateArray())
            {
                Assert.False(File.Exists(Path.Combine(projectPath, forbidden.GetString()!)),
                    $"{projectName} resurrected forbidden root file {forbidden.GetString()}");
            }

            foreach (var forbiddenFolder in project.GetProperty("forbiddenTopLevelFolders").EnumerateArray())
            {
                Assert.False(Directory.Exists(Path.Combine(projectPath, forbiddenFolder.GetString()!)),
                    $"{projectName} resurrected forbidden top-level folder {forbiddenFolder.GetString()}");
            }
        }
    }

    // ---------------------------------------------------------------------------------------------
    // 8. Wave evidence + preserved Host final closure
    // ---------------------------------------------------------------------------------------------

    [Fact]
    public void Tax_wave_evidence_and_host_closure_are_preserved()
    {
        var root = Repo();

        foreach (var wave in new[] { "W0", "W1", "W2", "W3" })
        {
            Assert.True(Directory.Exists(Path.Combine(
                root, "docs/architecture/evidence", $"TB-TMAR-TAX-AMSC-001-{wave}")),
                $"evidence directory for {wave} is required");
        }

        var w3Evidence = File.ReadAllText(Path.Combine(
            root, "docs/architecture/evidence/TB-TMAR-TAX-AMSC-001-W3/certification.md"));
        Assert.Contains("ARCH-COMPLETE-002", w3Evidence, StringComparison.Ordinal);
        Assert.Contains("COMPLETE_REFERENCE_PATTERN", w3Evidence, StringComparison.Ordinal);
        Assert.Contains("STRUCTURE_CERTIFIED", w3Evidence, StringComparison.Ordinal);

        // Repository-global Host root checkpoint must remain untouched by this module-local task.
        using var sot = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, StatePath))
            .Replace("\uFEFF", string.Empty));
        Assert.Equal("TB-TMAR-HOST-ROOT-FINAL-CERT-001", sot.RootElement.GetProperty("lastAcceptedTask").GetString());
        Assert.Equal("HOST_ROOT_FINAL_CERTIFIED", sot.RootElement.GetProperty("currentHostCheckpoint").GetString());
        Assert.Equal("NONE", sot.RootElement.GetProperty("automaticNextImplementationTask").GetString());

        // Master Recovery records the Tax certification checkpoint.
        var recovery = File.ReadAllText(Path.Combine(root, "docs/architecture/TOOBA-TMAR-MASTER-RECOVERY.md"));
        Assert.Contains("Tax AMSC module recovery checkpoint W3", recovery, StringComparison.Ordinal);
        Assert.Contains("TB-TMAR-TAX-AMSC-001-W3", recovery, StringComparison.Ordinal);
        Assert.Contains("USER_REVIEW_TAX_AMSC_001_W3", recovery, StringComparison.Ordinal);
    }

    // ---------------------------------------------------------------------------------------------
    // Helpers
    // ---------------------------------------------------------------------------------------------

    private static IEnumerable<string> ProductionSources(string root) =>
        ProductionProjects
            .SelectMany(p => Directory.EnumerateFiles(
                Path.Combine(root, ModuleRoot, p), "*.cs", SearchOption.AllDirectories))
            .Where(p => !p.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                        && !p.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                        && !p.Contains($"{Path.DirectorySeparatorChar}Migrations{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                        && !Path.GetFileName(p).EndsWith("ModelSnapshot.cs", StringComparison.OrdinalIgnoreCase));

    private static string Read(string root, string relativePath) =>
        File.ReadAllText(Path.Combine(root, relativePath.Replace('/', Path.DirectorySeparatorChar)));

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
