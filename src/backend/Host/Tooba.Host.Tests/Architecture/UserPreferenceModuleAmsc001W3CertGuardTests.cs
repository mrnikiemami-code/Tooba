using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Tooba.BuildingBlocks.Localization;
using Tooba.BuildingBlocks.Presentation.Errors;
using Tooba.UserPreference.Application.Composition;
using Tooba.UserPreference.Contracts.Errors;
using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>
/// TB-TMAR-USERPREFERENCE-AMSC-001-W3 — durable ARCH-COMPLETE-002 certification lock for
/// UserPreference (<c>tooba-architecture-certify</c>).
/// <para>
/// UserPreference is <c>HTTP_OWNING</c>: the W0 applicability gate proved three frontend-consumed
/// route groups (six routes), so endpoint ownership, CQRS and the exhaustive validator matrix are
/// all applicable and asserted here rather than excused.
/// </para>
/// <para>
/// The guard pins the SoT/manifest certification records with the AMSC wave lineage, the HTTP_OWNING
/// route ownership, the single canonical stable-code owner with the declared-code guard and the
/// bilingual locked resources, the canonical dual-mechanism typed-fault seam, the Contracts-only
/// microservice-extractable boundary, the unchanged schema/migration set, the exact
/// path↔namespace rule, the canonical <c>/Modules/UserPreference/</c> solution grouping and the
/// preserved Host final closure. Structural invariants are re-asserted here as defense in depth; the
/// structure authority remains <c>TB-TMAR-USERPREFERENCE-AMSC-001-W2</c>.
/// </para>
/// </summary>
public sealed class UserPreferenceModuleAmsc001W3CertGuardTests
{
    private const string ModuleRoot = "src/backend/Modules/UserPreference";
    private const string ManifestPath = "docs/architecture/tmar-module-structure-manifests.json";
    private const string StatePath = "docs/architecture/tmar-current-state.json";

    private static readonly string[] ProductionProjects =
    [
        "Tooba.UserPreference.Application",
        "Tooba.UserPreference.Contracts",
        "Tooba.UserPreference.Domain",
        "Tooba.UserPreference.Endpoints",
        "Tooba.UserPreference.Infrastructure",
    ];

    /// <summary>The exact certified set after the AMSC-001 lineage (31 members, UserPreference once).</summary>
    private static readonly string[] LockCertifiedModules =
    [
        "AccessControl", "AddressBook", "BulkInquiry", "Cart", "Catalog", "Content", "CustomerProfile",
        "Fulfillment", "Identity", "Inventory", "Localization", "Media", "Notification", "Offer",
        "OperatorProfile", "Order", "PageComposition", "Party", "Payment", "Pricing", "ProductQnA",
        "ProductWorkspace", "Promotion", "Returns", "Settlement", "StoreContext", "Story", "Support",
        "Tax", "UserPreference", "Wallet", "Wishlist",
    ];

    /// <summary>The ten UserPreference-owned stable machine codes.</summary>
    private static readonly string[] DeclaredCodes =
    [
        "preference.rejected",
        "ui_preference.rejected",
        "ui_preference.invalid_json",
        "ui_preference.json_required",
        "preference.validation.actor_required",
        "preference.validation.locale_required",
        "ui_preference.validation.actor_required",
        "ui_preference.validation.key_required",
        "ui_preference.validation.json_required",
        "user_preference.outbox.unmapped_event_type",
    ];

    private static readonly ErrorDefinitionCatalog Catalog = new(
    [
        new FoundationErrorCatalogContributor(),
        new UserPreferenceErrorCatalogContributor(),
    ]);

    private static readonly ResourceErrorMessageLocalizer Localizer = new(
        [new FoundationErrorResourceSet(), new UserPreferenceErrorResourceSet()],
        Array.Empty<IErrorMessageContributor>());

    // ---------------------------------------------------------------------------------------------
    // 1. Certification truth (manifest refresh + SoT + wave lineage)
    // ---------------------------------------------------------------------------------------------

    [Fact]
    public void UserPreference_is_arch_complete_002_certified_in_manifest_and_sot()
    {
        var root = Repo();

        using var manifest = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, ManifestPath))
            .Replace("\uFEFF", string.Empty));

        var entries = manifest.RootElement.GetProperty("modules").EnumerateArray()
            .Where(m => string.Equals(m.GetProperty("module").GetString(), "UserPreference", StringComparison.Ordinal))
            .ToArray();
        Assert.Single(entries);
        Assert.True(entries[0].GetProperty("structureCertified").GetBoolean());
        Assert.Equal("ARCH-COMPLETE-002", entries[0].GetProperty("lockVersion").GetString());
        Assert.Equal("TB-TMAR-USERPREFERENCE-AMSC-001-W3",
            entries[0].GetProperty("currentCertificationTask").GetString());
        Assert.Equal("COMPLETE_REFERENCE_PATTERN", entries[0].GetProperty("currentVerdict").GetString());
        Assert.Equal("TB-TMAR-USERPREFERENCE-AMSC-001-W2",
            entries[0].GetProperty("structureAuthorityTask").GetString());
        Assert.Contains(
            "TB-TMAR-USERPREFERENCE-AMSC-001-W3",
            entries[0].GetProperty("certificationNote").GetString()!,
            StringComparison.Ordinal);
        Assert.Contains(
            "TB-TMAR-USERPREFERENCE-AMC-001-W4",
            entries[0].GetProperty("certificationNote").GetString()!,
            StringComparison.Ordinal);

        var projects = entries[0].GetProperty("projects").EnumerateArray()
            .Select(p => p.GetProperty("projectName").GetString()!)
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(ProductionProjects.OrderBy(x => x, StringComparer.Ordinal).ToArray(), projects);

        // Refresh-in-place, never a parallel duplicate.
        Assert.DoesNotContain(
            manifest.RootElement.GetProperty("preCertModules").EnumerateArray(),
            m => string.Equals(m.GetProperty("module").GetString(), "UserPreference", StringComparison.Ordinal));
        Assert.DoesNotContain(
            "UserPreference",
            manifest.RootElement.GetProperty("uncertifiedHttpOwningModules").EnumerateArray()
                .Select(x => x.GetString()!),
            StringComparer.Ordinal);

        using var sot = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, StatePath))
            .Replace("\uFEFF", string.Empty));

        var certified = sot.RootElement.GetProperty("structureLock").GetProperty("certifiedModules")
            .EnumerateArray().Select(x => x.GetString()!)
            .OrderBy(x => x, StringComparer.Ordinal).ToArray();
        Assert.Equal(LockCertifiedModules, certified);
        Assert.Equal(32, certified.Length);
        Assert.Equal(1, certified.Count(x => string.Equals(x, "UserPreference", StringComparison.Ordinal)));

        var w3 = sot.RootElement.GetProperty("userPreferenceAmsc001W3");
        Assert.Equal("TB-TMAR-USERPREFERENCE-AMSC-001-W3", w3.GetProperty("task").GetString());
        Assert.Equal("TB-TMAR-USERPREFERENCE-AMSC-001-W2", w3.GetProperty("parentTask").GetString());
        Assert.Equal("USERPREFERENCE_AMSC_001_CERTIFIED", w3.GetProperty("state").GetString());
        Assert.Equal("COMPLETE_REFERENCE_PATTERN", w3.GetProperty("verdict").GetString());
        Assert.Equal("ARCH-COMPLETE-002", w3.GetProperty("lockVersion").GetString());
        Assert.True(w3.GetProperty("structureCertified").GetBoolean());
        Assert.Equal("HTTP_OWNING", w3.GetProperty("httpApplicability").GetString());
        Assert.Equal("MODULE_OWNED", w3.GetProperty("endpointOwnershipState").GetString());
        Assert.Equal(6, w3.GetProperty("moduleOwnedRouteCount").GetInt32());
        Assert.Equal(0, w3.GetProperty("hostOwnedRouteCount").GetInt32());
        Assert.Equal(4, w3.GetProperty("endpointReachableRequests").GetInt32());
        Assert.Equal("COMPLIANT_4_OF_4_REAL_IREQUEST_AND_HANDLER_ISENDER", w3.GetProperty("cqrsState").GetString());
        Assert.Equal("EXHAUSTIVE_4_OF_4_3_VALIDATOR_REQUIRED_1_NO_VALIDATOR_REQUIRED",
            w3.GetProperty("validatorMatrixState").GetString());
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
        Assert.Equal("HOST_TMAR_EVACUATION_FINAL_CLOSURE_CERTIFIED_PRESERVED",
            w3.GetProperty("hostFinalClosureState").GetString());
        Assert.Equal("NONE", w3.GetProperty("guardsWeakened").GetString());
        Assert.Equal("NONE", w3.GetProperty("baselinesWidened").GetString());
        Assert.Equal("NONE", w3.GetProperty("automaticNextImplementationTask").GetString());
        Assert.Equal("USER_REVIEW_USERPREFERENCE_AMSC_001_W3", w3.GetProperty("stopGate").GetString());

        // Wave lineage: each wave's starting head is the parent wave's commit.
        Assert.Equal("27c273be", sot.RootElement.GetProperty("userPreferenceAmsc001W0").GetProperty("commit").GetString());
        Assert.Equal("27c273be", sot.RootElement.GetProperty("userPreferenceAmsc001W1").GetProperty("startingHead").GetString());
        Assert.Equal("a0edab10", sot.RootElement.GetProperty("userPreferenceAmsc001W1").GetProperty("commit").GetString());
        Assert.Equal("a0edab10", sot.RootElement.GetProperty("userPreferenceAmsc001W2").GetProperty("startingHead").GetString());
        Assert.Equal("2547037e", sot.RootElement.GetProperty("userPreferenceAmsc001W2").GetProperty("commit").GetString());
        Assert.Equal("2547037e79cb2154872c0bfef036ee812c4b037b", w3.GetProperty("startingHead").GetString());
        Assert.Equal("TB-TMAR-USERPREFERENCE-AMSC-001-W2", w3.GetProperty("currentStructureAuthority").GetString());
        Assert.Equal("READY_FOR_CERTIFY_CONSUMED_BY_W3", w3.GetProperty("currentStructureState").GetString());
    }

    // ---------------------------------------------------------------------------------------------
    // 2. HTTP_OWNING applicability — six module-owned routes, zero Host routes
    // ---------------------------------------------------------------------------------------------

    [Fact]
    public void UserPreference_is_http_owning_with_module_owned_routes_and_no_host_routes()
    {
        var root = Repo();
        var endpoints = Path.Combine(root, ModuleRoot, "Tooba.UserPreference.Endpoints");

        Assert.True(Directory.Exists(endpoints), "UserPreference must own an Endpoints project (HTTP_OWNING)");

        var moduleMapper = File.ReadAllText(Path.Combine(endpoints, "UserPreferenceEndpointModule.cs"));
        Assert.Contains("MapUserPreferenceModuleEndpoints", moduleMapper, StringComparison.Ordinal);
        Assert.Contains("\"/v1/customer/preferences\"", moduleMapper, StringComparison.Ordinal);
        Assert.Contains("\"/v1/admin/operator/preferences\"", moduleMapper, StringComparison.Ordinal);
        Assert.Contains("\"/v1/admin/ui-preferences\"", moduleMapper, StringComparison.Ordinal);

        // Three route groups, six mapped operations.
        var mapped = Regex.Matches(
            string.Join("\n", Directory.EnumerateFiles(endpoints, "*.cs", SearchOption.AllDirectories)
                .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                            && !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
                .Select(File.ReadAllText)),
            @"group\.Map(Get|Put|Post|Patch|Delete)\(").Count;
        Assert.Equal(6, mapped);

        // The Host owns zero UserPreference routes and maps the module boundary only.
        var program = File.ReadAllText(Path.Combine(root, "src/backend/Host/Tooba.Host/Program.cs"));
        Assert.Contains("MapUserPreferenceModuleEndpoints", program, StringComparison.Ordinal);
        Assert.DoesNotContain("\"/v1/customer/preferences\"", program, StringComparison.Ordinal);
        Assert.DoesNotContain("\"/v1/admin/operator/preferences\"", program, StringComparison.Ordinal);
        Assert.DoesNotContain("\"/v1/admin/ui-preferences\"", program, StringComparison.Ordinal);

        // The four endpoint-reachable requests are real CQRS requests dispatched through ISender.
        var application = Path.Combine(root, ModuleRoot, "Tooba.UserPreference.Application");
        var requestNames = new[]
        {
            "UpsertUserPreferenceCommand", "GetUserPreferenceQuery",
            "UpsertUiPreferenceCommand", "GetUiPreferenceQuery",
        };
        foreach (var request in requestNames)
        {
            var match = Directory.EnumerateFiles(application, request + ".cs", SearchOption.AllDirectories).Single();
            var text = File.ReadAllText(match);
            Assert.Contains("IRequest<Result<", text, StringComparison.Ordinal);
            Assert.Contains($"IRequestHandler<{request}", text, StringComparison.Ordinal);
        }

        // Thin endpoints: each endpoint class dispatches through ISender and constructs no raw result.
        // (Actor resolution helpers and transport DTO records are co-located and carry no handler.)
        foreach (var file in Directory.EnumerateFiles(endpoints, "*.cs", SearchOption.AllDirectories)
                     .Where(f => f.EndsWith("Endpoints.cs", StringComparison.Ordinal))
                     .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                                 && !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal)))
        {
            var text = File.ReadAllText(file);
            Assert.Contains("ISender", text, StringComparison.Ordinal);
            Assert.Contains("api.From(", text, StringComparison.Ordinal);
            Assert.DoesNotContain("Results.Json", text, StringComparison.Ordinal);
            Assert.DoesNotContain("Results.BadRequest", text, StringComparison.Ordinal);
            Assert.DoesNotContain("Results.Problem", text, StringComparison.Ordinal);
            Assert.DoesNotContain("ProblemDetails", text, StringComparison.Ordinal);
            Assert.DoesNotContain("catch (SemanticException", text, StringComparison.Ordinal);
            Assert.DoesNotContain("catch (PlatformHttpException", text, StringComparison.Ordinal);
            Assert.DoesNotContain("FromPlatformException(", text, StringComparison.Ordinal);
            Assert.DoesNotContain("DbContext", text, StringComparison.Ordinal);
        }

        // Exhaustive validator matrix: 3 VALIDATOR_REQUIRED present, 1 NO_VALIDATOR_REQUIRED (actor
        // is server-derived, so no caller-controlled transport shape exists).
        Assert.True(File.Exists(Path.Combine(application, "LocalePreferences", "Validators", "UpsertUserPreferenceCommandValidator.cs")));
        Assert.True(File.Exists(Path.Combine(application, "UiPreferences", "Validators", "UpsertUiPreferenceCommandValidator.cs")));
        Assert.True(File.Exists(Path.Combine(application, "UiPreferences", "Validators", "GetUiPreferenceQueryValidator.cs")));
        Assert.Empty(Directory.EnumerateFiles(application, "GetUserPreferenceQueryValidator.cs", SearchOption.AllDirectories));
    }

    // ---------------------------------------------------------------------------------------------
    // 3. Stable codes / descriptors / bilingual resources (canonical localization)
    // ---------------------------------------------------------------------------------------------

    [Fact]
    public void UserPreference_error_surface_has_one_stable_code_owner_and_bilingual_resources()
    {
        var root = Repo();
        var errors = Path.Combine(root, ModuleRoot, "Tooba.UserPreference.Contracts", "Errors");

        var codesFile = File.ReadAllText(Path.Combine(errors, "UserPreferenceErrorCodes.cs"));
        var declared = Regex.Matches(codesFile, "public const string \\w+ = \"(?<c>[^\"]+)\"")
            .Select(m => m.Groups["c"].Value).ToArray();
        // 10 module-owned codes plus the Foundation-owned session constant.
        Assert.Equal(11, declared.Length);
        Assert.Equal(
            DeclaredCodes.Append("customer.session.required").OrderBy(x => x, StringComparer.Ordinal).ToArray(),
            declared.OrderBy(x => x, StringComparer.Ordinal).ToArray());
        Assert.Contains("private static readonly HashSet<string> KnownCodes", codesFile, StringComparison.Ordinal);
        Assert.Contains("public static bool IsKnown(string? code)", codesFile, StringComparison.Ordinal);

        // The Foundation-owned code is consumable but never a module use-case fault.
        Assert.False(UserPreferenceErrorCodes.IsKnown("customer.session.required"));
        Assert.All(DeclaredCodes, code => Assert.True(UserPreferenceErrorCodes.IsKnown(code), code));
        Assert.False(UserPreferenceErrorCodes.IsKnown("order.not_found"));

        // Exactly one declared-code home, one catalog contributor, one resource set.
        Assert.Equal(new[] { "UserPreferenceErrorCatalogContributor.cs" },
            Directory.EnumerateFiles(errors, "*Contributor.cs").Select(Path.GetFileName!).OrderBy(x => x, StringComparer.Ordinal).ToArray());
        Assert.Equal(new[] { "UserPreferenceErrorResourceSet.cs" },
            Directory.EnumerateFiles(errors, "*ResourceSet.cs").Select(Path.GetFileName!).OrderBy(x => x, StringComparer.Ordinal).ToArray());

        // One descriptor per declared module code, each resolving to exactly one canonical owner.
        var contributor = File.ReadAllText(Path.Combine(errors, "UserPreferenceErrorCatalogContributor.cs"));
        var descriptorNames = Regex.Matches(contributor, @"D\(UserPreferenceErrorCodes\.(?<n>\w+)")
            .Select(m => m.Groups["n"].Value).ToArray();
        var declaredNames = Regex.Matches(codesFile, "public const string (?<n>\\w+)")
            .Select(m => m.Groups["n"].Value).ToArray();
        Assert.Equal(10, descriptorNames.Length);
        Assert.Equal(declaredNames.Length - 1, descriptorNames.Distinct(StringComparer.Ordinal).Count());
        foreach (var name in declaredNames.Where(n => n != "SessionRequired"))
        {
            Assert.Contains(name, descriptorNames);
        }

        var descriptors = new UserPreferenceErrorCatalogContributor().Contribute().ToArray();
        Assert.Equal(10, descriptors.Length);
        foreach (var descriptor in descriptors)
        {
            Assert.Equal(descriptor.Code, descriptor.LocalizationKey);
            Assert.True(UserPreferenceErrorCodes.IsKnown(descriptor.Code), descriptor.Code);
            Assert.True(Catalog.TryGet(descriptor.Code, out var resolved), "missing descriptor " + descriptor.Code);
            Assert.Equal(descriptor.Code, resolved.LocalizationKey);
        }

        // Foundation owns customer.session.required; UserPreference must never claim its descriptor.
        Assert.DoesNotContain(descriptors, d => d.Code == "customer.session.required");

        var registered = Catalog.RegisteredCodes
            .Where(code => code.StartsWith("preference.", StringComparison.Ordinal)
                           || code.StartsWith("ui_preference.", StringComparison.Ordinal)
                           || code.StartsWith("user_preference.", StringComparison.Ordinal))
            .OrderBy(code => code, StringComparer.Ordinal).ToArray();
        Assert.Equal(descriptors.Select(d => d.Code).OrderBy(x => x, StringComparer.Ordinal).ToArray(), registered);

        // Bilingual resources: 10 EN + 10 FA keys with composed resolution and real Persian text.
        foreach (var resource in new[] { "UserPreferenceErrors.resx", "UserPreferenceErrors.fa.resx" })
        {
            var resx = File.ReadAllText(Path.Combine(root, ModuleRoot, "Tooba.UserPreference.Contracts", "Resources", resource));
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

        // The explicit embedded logical names are locked in the Contracts csproj.
        var contracts = File.ReadAllText(Path.Combine(root, ModuleRoot, "Tooba.UserPreference.Contracts", "Tooba.UserPreference.Contracts.csproj"));
        Assert.Contains("Tooba.UserPreference.Contracts.Resources.UserPreferenceErrors.resources", contracts, StringComparison.Ordinal);
        Assert.Contains("Tooba.UserPreference.Contracts.Resources.UserPreferenceErrors.fa.resources", contracts, StringComparison.Ordinal);

        Assert.True(new UserPreferenceErrorResourceSet().Owns("preference.rejected"));
        Assert.True(new UserPreferenceErrorResourceSet().Owns("ui_preference.validation.key_required"));
        Assert.True(new UserPreferenceErrorResourceSet().Owns("user_preference.outbox.unmapped_event_type"));
        Assert.False(new UserPreferenceErrorResourceSet().Owns("customer.session.required"));
        Assert.False(new UserPreferenceErrorResourceSet().Owns("order.not_found"));
    }

    // ---------------------------------------------------------------------------------------------
    // 4. Canonical typed-fault seam + single presentation registration
    // ---------------------------------------------------------------------------------------------

    [Fact]
    public void UserPreference_typed_fault_seam_and_catalog_registration_are_canonical()
    {
        var root = Repo();

        var seam = File.ReadAllText(Path.Combine(
            root, ModuleRoot, "Tooba.UserPreference.Application", "Composition", "UserPreferenceOperation.cs"));
        Assert.Equal(
            2,
            Regex.Matches(seam, @"catch \(ContractOperationException ex\) when \(UserPreferenceErrorCodes\.IsKnown\(ex\.Code\)\)").Count);
        Assert.Equal(2, Regex.Matches(seam, @"catch \(SemanticException ex\)").Count);
        Assert.Contains("public static async Task<Result<T>> ExecuteAsync<T>", seam, StringComparison.Ordinal);
        Assert.Contains("public static async Task<Result> ExecuteAsync(Func<Task> action)", seam, StringComparison.Ordinal);
        Assert.DoesNotContain("ex.Message", seam, StringComparison.Ordinal);
        Assert.DoesNotContain("StartsWith", seam, StringComparison.Ordinal);
        Assert.DoesNotContain("IResult", seam, StringComparison.Ordinal);
        Assert.Equal("Tooba.UserPreference.Application.Composition", typeof(UserPreferenceOperation).Namespace);

        // Zero message-prose classification and zero raw prose fault anywhere in the production surface.
        foreach (var file in ProductionSources(root))
        {
            var text = File.ReadAllText(file);
            Assert.DoesNotContain(".Message.Contains(", text, StringComparison.Ordinal);
            Assert.DoesNotContain(".Message.StartsWith(", text, StringComparison.Ordinal);
            Assert.DoesNotContain(".Message ==", text, StringComparison.Ordinal);
            Assert.DoesNotContain("InvalidOperationException(\"", text, StringComparison.Ordinal);
            Assert.DoesNotContain("UserPreference integration event is not registered.", text, StringComparison.Ordinal);
        }

        // HTTP_OWNING: the module owns its boundary, and the error catalog + resource set are registered
        // exactly once by the Infrastructure composition root.
        var module = File.ReadAllText(Path.Combine(
            root, ModuleRoot, "Tooba.UserPreference.Infrastructure", "UserPreferenceModule.cs"));
        Assert.Equal(1, Regex.Matches(module,
            @"AddSingleton<\s*IErrorCatalogContributor,\s*UserPreferenceErrorCatalogContributor>").Count);
        Assert.Equal(1, Regex.Matches(module,
            @"AddSingleton<\s*IErrorResourceSet,\s*UserPreferenceErrorResourceSet>").Count);
        Assert.Equal("Tooba.UserPreference.Contracts", typeof(UserPreferenceErrorCatalogContributor).Assembly.GetName().Name);
        Assert.Equal("Tooba.UserPreference.Contracts.Errors", typeof(UserPreferenceErrorCodes).Namespace);
        Assert.Equal("Tooba.UserPreference.Contracts.Errors", typeof(UserPreferenceErrorResourceSet).Namespace);
    }

    // ---------------------------------------------------------------------------------------------
    // 5. Contracts-only microservice boundary
    // ---------------------------------------------------------------------------------------------

    [Fact]
    public void UserPreference_is_contracts_only_and_microservice_extractable()
    {
        var root = Repo();

        foreach (var project in ProductionProjects)
        {
            var csproj = XDocument.Load(Path.Combine(root, ModuleRoot, project, project + ".csproj"));
            foreach (var reference in csproj.Descendants("ProjectReference")
                         .Select(x => ((string?)x.Attribute("Include") ?? string.Empty).Replace('\\', '/')))
            {
                if (reference.Contains("Tooba.UserPreference.", StringComparison.Ordinal)
                    || reference.Contains("Tooba.BuildingBlocks", StringComparison.Ordinal)
                    || reference.Contains("Tooba.ModuleContracts", StringComparison.Ordinal)
                    || reference.Contains("Tooba.Persistence", StringComparison.Ordinal))
                {
                    continue;
                }

                // The single legal foreign edge: the Order guest-actor vocabulary at the HTTP boundary.
                Assert.Equal("Tooba.UserPreference.Endpoints", project);
                Assert.Contains("Tooba.Order.Contracts", reference, StringComparison.Ordinal);
            }
        }

        // Zero foreign module Application/Infrastructure/Domain/Endpoints coupling.
        var joined = string.Join("\n", ProductionSources(root).Select(File.ReadAllText));
        var foreignPattern =
            @"Tooba\.(Order|Cart|Catalog|Identity|AccessControl|Content|CustomerProfile|Notification|Media|Fulfillment|Inventory|BulkInquiry|Localization|Settlement|Story|Wishlist|ProductQnA|Reviews|OperatorProfile|PageComposition|Promotion|Returns|Wallet|Support|AddressBook|Payment|StoreContext|Pricing|Offer|Party|Tax|ProductWorkspace|Host)\.(Application|Infrastructure|Domain|Endpoints)";
        Assert.False(Regex.IsMatch(joined, foreignPattern), "foreign module coupling found in UserPreference production");

        foreach (var foreignContext in new[]
                 {
                     "OrderDbContext", "CatalogDbContext", "PricingDbContext", "PaymentDbContext",
                     "WalletDbContext", "InventoryDbContext", "PartyDbContext", "OfferDbContext",
                     "SupportDbContext", "NotificationDbContext", "AccessControlDbContext",
                     "TaxDbContext", "IdentityDbContext", "CartDbContext",
                 })
        {
            Assert.DoesNotContain(foreignContext, joined, StringComparison.Ordinal);
        }

        Assert.DoesNotContain("TypeForwardedTo", joined, StringComparison.Ordinal);
        Assert.DoesNotContain("namespace Tooba.UserPreference.Contracts;", joined, StringComparison.Ordinal);
        Assert.DoesNotContain("namespace Tooba.UserPreference.Domain;", joined, StringComparison.Ordinal);
    }

    // ---------------------------------------------------------------------------------------------
    // 6. Persistence / schema preservation
    // ---------------------------------------------------------------------------------------------

    [Fact]
    public void UserPreference_schema_and_migrations_are_preserved()
    {
        var root = Repo();

        var context = File.ReadAllText(Path.Combine(
            root, ModuleRoot, "Tooba.UserPreference.Infrastructure", "Persistence", "UserPreferenceDbContext.cs"));
        Assert.Contains("public const string Schema = \"user_preference\"", context, StringComparison.Ordinal);
        Assert.Contains("HasDefaultSchema(Schema)", context, StringComparison.Ordinal);
        Assert.Equal(1, Regex.Matches(context, @"class UserPreferenceDbContext\b").Count);
        Assert.Contains("ToTable(\"user_preferences\")", context, StringComparison.Ordinal);
        Assert.Contains("ToTable(\"ui_preferences\")", context, StringComparison.Ordinal);

        var migrations = Directory.EnumerateFiles(
                Path.Combine(root, ModuleRoot, "Tooba.UserPreference.Infrastructure", "Persistence", "Migrations"),
                "*.cs", SearchOption.TopDirectoryOnly)
            .Select(Path.GetFileName).OrderBy(n => n, StringComparer.Ordinal).ToArray();
        Assert.Equal(
            [
                "20260827215300_InitialUserPreference.Designer.cs",
                "20260827215300_InitialUserPreference.cs",
                "20260828020000_AddUiPreferences.Designer.cs",
                "20260828020000_AddUiPreferences.cs",
                "UserPreferenceDbContextModelSnapshot.cs",
            ],
            migrations);

        // No migration may appear outside the canonical Persistence/Migrations folder.
        Assert.False(Directory.Exists(Path.Combine(root, ModuleRoot, "Tooba.UserPreference.Infrastructure", "Migrations")));

        // The outbox owns the module schema and declares no external event this version.
        var outbox = File.ReadAllText(Path.Combine(
            root, ModuleRoot, "Tooba.UserPreference.Infrastructure", "Persistence", "UserPreferenceOutboxRegistration.cs"));
        Assert.Contains("Translate(IDomainEvent domainEvent, EventMetadata metadata) => null", outbox, StringComparison.Ordinal);
        Assert.Contains("ResolveEventClrType(string eventTypeName) => null", outbox, StringComparison.Ordinal);
        Assert.Contains("ContractOperationException(UserPreferenceErrorCodes.OutboxUnmappedEventType)", outbox, StringComparison.Ordinal);
    }

    // ---------------------------------------------------------------------------------------------
    // 7. Structure invariants (defense in depth; authority stays W2)
    // ---------------------------------------------------------------------------------------------

    [Fact]
    public void UserPreference_structure_invariants_hold_for_the_five_project_solution_group()
    {
        var root = Repo();

        // Root-Allowlist-State: Contracts/Domain/Application empty; Endpoints/Infrastructure composition only.
        var expectedRoots = new Dictionary<string, string[]>(StringComparer.Ordinal)
        {
            ["Tooba.UserPreference.Application"] = [],
            ["Tooba.UserPreference.Contracts"] = [],
            ["Tooba.UserPreference.Domain"] = [],
            ["Tooba.UserPreference.Endpoints"] = ["UserPreferenceEndpointModule.cs"],
            ["Tooba.UserPreference.Infrastructure"] = ["UserPreferenceModule.cs"],
        };
        foreach (var (project, expected) in expectedRoots)
        {
            var actual = Directory.GetFiles(Path.Combine(root, ModuleRoot, project), "*.cs", SearchOption.TopDirectoryOnly)
                .Select(Path.GetFileName!).OrderBy(x => x, StringComparer.Ordinal).ToArray();
            Assert.Equal(expected, actual);
        }

        // Path-Namespace-State: EXACT for every production file.
        foreach (var project in ProductionProjects)
        {
            var projectPath = Path.Combine(root, ModuleRoot, project);
            foreach (var file in Directory.GetFiles(projectPath, "*.cs", SearchOption.AllDirectories))
            {
                var relative = file[Path.GetFullPath(projectPath).Length..]
                    .TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                if (relative.StartsWith($"obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                    || relative.StartsWith($"bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
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
        var contracts = Path.Combine(root, ModuleRoot, "Tooba.UserPreference.Contracts");
        Assert.False(File.Exists(Path.Combine(contracts, "UserPreferenceErrorCodes.cs")));
        Assert.False(Directory.Exists(Path.Combine(root, ModuleRoot, "Tooba.UserPreference.Domain", "Errors")));
        Assert.False(Directory.Exists(Path.Combine(root, ModuleRoot, "Tooba.UserPreference.Endpoints", "Errors")));

        // Solution-Explorer-State: canonical /Modules/UserPreference/ grouping with exactly five projects.
        var slnx = File.ReadAllText(Path.Combine(root, "src/backend/Tooba.slnx"));
        var folderStart = slnx.IndexOf("<Folder Name=\"/Modules/UserPreference/\">", StringComparison.Ordinal);
        Assert.True(folderStart >= 0, "missing /Modules/UserPreference/ solution folder");
        var group = slnx[folderStart..slnx.IndexOf("</Folder>", folderStart, StringComparison.Ordinal)];
        Assert.Equal(5, Regex.Matches(group, @"<Project Path=").Count);
        foreach (var project in ProductionProjects)
        {
            Assert.Contains($"Modules/UserPreference/{project}/{project}.csproj", group, StringComparison.Ordinal);
        }

        // The certified manifest entry's allowlists/forbidden lists still reconcile with disk.
        using var manifest = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, ManifestPath))
            .Replace("\uFEFF", string.Empty));
        var entry = manifest.RootElement.GetProperty("modules").EnumerateArray()
            .Single(m => string.Equals(m.GetProperty("module").GetString(), "UserPreference", StringComparison.Ordinal));
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
    public void UserPreference_wave_evidence_and_host_closure_are_preserved()
    {
        var root = Repo();

        foreach (var wave in new[] { "W0", "W1", "W2", "W3" })
        {
            Assert.True(Directory.Exists(Path.Combine(
                root, "docs/architecture/evidence", $"TB-TMAR-USERPREFERENCE-AMSC-001-{wave}")),
                $"evidence directory for {wave} is required");
        }

        var w3Evidence = File.ReadAllText(Path.Combine(
            root, "docs/architecture/evidence/TB-TMAR-USERPREFERENCE-AMSC-001-W3/certification.md"));
        Assert.Contains("ARCH-COMPLETE-002", w3Evidence, StringComparison.Ordinal);
        Assert.Contains("COMPLETE_REFERENCE_PATTERN", w3Evidence, StringComparison.Ordinal);
        Assert.Contains("HTTP_OWNING", w3Evidence, StringComparison.Ordinal);

        // Repository-global Host root checkpoint must remain untouched by this module-local task.
        using var sot = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, StatePath))
            .Replace("\uFEFF", string.Empty));
        Assert.Equal("TB-TMAR-HOST-ROOT-FINAL-CERT-001", sot.RootElement.GetProperty("lastAcceptedTask").GetString());
        Assert.Equal("HOST_ROOT_FINAL_CERTIFIED", sot.RootElement.GetProperty("currentHostCheckpoint").GetString());
        Assert.Equal("NONE", sot.RootElement.GetProperty("automaticNextImplementationTask").GetString());

        // Master Recovery records the UserPreference certification checkpoint.
        var recovery = File.ReadAllText(Path.Combine(root, "docs/architecture/TOOBA-TMAR-MASTER-RECOVERY.md"));
        Assert.Contains("UserPreference AMSC module recovery checkpoint W3", recovery, StringComparison.Ordinal);
        Assert.Contains("TB-TMAR-USERPREFERENCE-AMSC-001-W3", recovery, StringComparison.Ordinal);
        Assert.Contains("USER_REVIEW_USERPREFERENCE_AMSC_001_W3", recovery, StringComparison.Ordinal);
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
