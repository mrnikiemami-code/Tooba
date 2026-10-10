using System.Globalization;
using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Localization;
using Tooba.BuildingBlocks.Presentation.Errors;
using Tooba.Wishlist.Application.Customer.Commands;
using Tooba.Wishlist.Contracts.Errors;
using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>
/// TB-TMAR-WISHLIST-AMSC-001-W3 — durable ARCH-COMPLETE-002 certification lock for Wishlist.
///
/// Locks the SoT/manifest certification records with the AMSC wave lineage, the module-owned HTTP
/// surface with a Host route count of zero, the exhaustive 4-row HTTP → request → handler → validator
/// provenance set equality (certify §6a hard gate, derived from the endpoint source itself), the
/// canonical API-result/typed-fault/localization/catalog mechanisms, the Contracts-only
/// microservice-extractable boundary, the unchanged Wishlist schema, and the preserved Host final
/// closure checkpoints.
/// </summary>
public sealed class WishlistModuleAmsc001W3CertGuardTests
{
    private const string ModuleRoot = "src/backend/Modules/Wishlist";

    private const string EndpointsFile = "Tooba.Wishlist.Endpoints/Customer/WishlistCustomerEndpoints.cs";
    private const string ModuleFile = "Tooba.Wishlist.Endpoints/WishlistEndpointModule.cs";
    private const string ValidationDir = "Tooba.Wishlist.Application/Validation";
    private const string CodesFile = "Tooba.Wishlist.Contracts/Errors/WishlistErrorCodes.cs";
    private const string ContributorFile = "Tooba.Wishlist.Contracts/Errors/WishlistErrorCatalogContributor.cs";
    private const string ResourceSetFile = "Tooba.Wishlist.Contracts/Errors/WishlistErrorResourceSet.cs";
    private const string ResxDirectory = "Tooba.Wishlist.Contracts/Resources";
    private const string SeamFile = "Tooba.Wishlist.Application/Composition/WishlistOperation.cs";

    private static readonly string[] ProductionProjects =
    [
        "Tooba.Wishlist.Contracts",
        "Tooba.Wishlist.Domain",
        "Tooba.Wishlist.Application",
        "Tooba.Wishlist.Infrastructure",
        "Tooba.Wishlist.Endpoints",
    ];

    private static readonly string[] ValidatorRequired =
    [
        "AddWishlistItemCommand",
        "RemoveWishlistItemCommand",
        "GetWishlistMembershipQuery",
    ];

    private static readonly string[] NoValidatorRequired =
    [
        "ListWishlistPageQuery",
    ];

    /// <summary>The four Wishlist-owned stable machine codes.</summary>
    private static readonly string[] WishlistOwnedCodes =
    [
        "customer.wishlist.product_unavailable",
        "customer.wishlist.product_id_required",
        "customer.wishlist.product_ids_required",
        "wishlist.outbox.unmapped_event_type",
    ];

    /// <summary>Exact expected verb → path → dispatched request map (4 rows).</summary>
    private static readonly (string Verb, string Path, string Request)[] ExpectedRoutes =
    [
        ("Get", "/v1/customer/wishlist", "ListWishlistPageQuery"),
        ("Post", "/v1/customer/wishlist/{productId:guid}", "AddWishlistItemCommand"),
        ("Delete", "/v1/customer/wishlist/{productId:guid}", "RemoveWishlistItemCommand"),
        ("Post", "/v1/customer/wishlist/membership", "GetWishlistMembershipQuery"),
    ];

    // ---------------------------------------------------------------------------------------------
    // 1. Certification truth (manifest + SoT + wave lineage)
    // ---------------------------------------------------------------------------------------------

    [Fact]
    public void Wishlist_is_arch_complete_002_certified_in_manifest_and_sot()
    {
        var root = Repo();

        using var manifest = JsonDocument.Parse(File.ReadAllText(Path.Combine(
            root, "docs/architecture/tmar-module-structure-manifests.json")).Replace("\uFEFF", string.Empty));
        var entries = manifest.RootElement.GetProperty("modules").EnumerateArray()
            .Where(m => string.Equals(m.GetProperty("module").GetString(), "Wishlist", StringComparison.Ordinal))
            .ToArray();
        Assert.Single(entries);
        Assert.True(entries[0].GetProperty("structureCertified").GetBoolean());
        Assert.Equal("ARCH-COMPLETE-002", entries[0].GetProperty("lockVersion").GetString());
        Assert.Equal("WISHLIST_AMSC_001_CERTIFIED", entries[0].GetProperty("certificationState").GetString());
        Assert.Equal("TB-TMAR-WISHLIST-AMSC-001-W3", entries[0].GetProperty("currentCertificationTask").GetString());
        Assert.Equal("COMPLETE_REFERENCE_PATTERN", entries[0].GetProperty("currentVerdict").GetString());
        Assert.Contains(
            "TB-TMAR-WISHLIST-AMSC-001-W3",
            entries[0].GetProperty("certificationNote").GetString()!,
            StringComparison.Ordinal);
        Assert.Equal(5, entries[0].GetProperty("projects").GetArrayLength());
        Assert.Equal("TB-TMAR-WISHLIST-AMSC-001-W2", entries[0].GetProperty("structureAuthorityTask").GetString());
        Assert.Equal("READY_FOR_CERTIFY_CONSUMED_BY_W3", entries[0].GetProperty("structureHandoffState").GetString());

        // Promotion/refresh is in place, not duplicated, and Wishlist is not a pre-cert or uncertified entry.
        Assert.DoesNotContain(
            manifest.RootElement.GetProperty("preCertModules").EnumerateArray(),
            m => string.Equals(m.GetProperty("module").GetString(), "Wishlist", StringComparison.Ordinal));
        Assert.DoesNotContain(
            "Wishlist",
            manifest.RootElement.GetProperty("uncertifiedHttpOwningModules").EnumerateArray()
                .Select(x => x.GetString()!),
            StringComparer.Ordinal);

        using var sot = JsonDocument.Parse(File.ReadAllText(Path.Combine(
            root, "docs/architecture/tmar-current-state.json")).Replace("\uFEFF", string.Empty));
        var w3 = sot.RootElement.GetProperty("wishlistAmsc001W3");
        Assert.Equal("TB-TMAR-WISHLIST-AMSC-001-W3", w3.GetProperty("task").GetString());
        Assert.Equal("TB-TMAR-WISHLIST-AMSC-001-W2", w3.GetProperty("parentTask").GetString());
        Assert.Equal("WISHLIST_AMSC_001_CERTIFIED", w3.GetProperty("state").GetString());
        Assert.Equal("COMPLETE_REFERENCE_PATTERN", w3.GetProperty("verdict").GetString());
        Assert.Equal("ARCH-COMPLETE-002", w3.GetProperty("lockVersion").GetString());
        Assert.True(w3.GetProperty("structureCertified").GetBoolean());
        Assert.Equal("HTTP_OWNING", w3.GetProperty("httpApplicability").GetString());
        Assert.Equal("MODULE_OWNED_4_ROUTES_HOST_ZERO", w3.GetProperty("endpointOwnershipState").GetString());
        Assert.Equal(4, w3.GetProperty("moduleOwnedRouteCount").GetInt32());
        Assert.Equal(0, w3.GetProperty("hostOwnedRouteCount").GetInt32());
        Assert.Equal(4, w3.GetProperty("endpointReachableRequests").GetInt32());
        Assert.Equal("MEDIATR_12_5_ISENDER_4_OF_4", w3.GetProperty("cqrsState").GetString());
        Assert.Equal("EXHAUSTIVE_3_VALIDATOR_REQUIRED_1_NO_VALIDATOR_REQUIRED", w3.GetProperty("validatorMatrixState").GetString());
        Assert.Equal("EXACT", w3.GetProperty("pathNamespaceState").GetString());
        Assert.Equal("ENFORCED", w3.GetProperty("rootAllowlistState").GetString());
        Assert.Equal("NONE", w3.GetProperty("aliasWorkaroundState").GetString());
        Assert.Equal("PROFESSIONAL_SHALLOW", w3.GetProperty("folderGranularityState").GetString());
        Assert.Equal("CANONICAL", w3.GetProperty("solutionExplorerState").GetString());
        Assert.Equal("CLEAN", w3.GetProperty("physicalCopyState").GetString());
        Assert.Equal("ZERO", w3.GetProperty("foreignApplicationInfrastructureDomainEdges").GetString());
        Assert.Equal("ZERO", w3.GetProperty("crossModuleJoinState").GetString());
        Assert.Equal("UNCHANGED", w3.GetProperty("schemaState").GetString());
        Assert.Equal("PRESERVED", w3.GetProperty("behaviorPreservation").GetString());
        Assert.Equal("NONE", w3.GetProperty("guardsWeakened").GetString());
        Assert.Equal("NONE", w3.GetProperty("baselinesWidened").GetString());
        Assert.Equal("HOST_TMAR_EVACUATION_FINAL_CLOSURE_CERTIFIED_PRESERVED", w3.GetProperty("hostFinalClosureState").GetString());
        Assert.Equal("USER_REVIEW_WISHLIST_AMSC_001_W3", w3.GetProperty("workflowStop").GetString());
        Assert.Equal("NONE", w3.GetProperty("automaticNextImplementationTask").GetString());

        var certified = sot.RootElement.GetProperty("structureLock").GetProperty("certifiedModules")
            .EnumerateArray().Select(x => x.GetString()).ToArray();
        Assert.Equal(1, certified.Count(x => x == "Wishlist"));
        Assert.Equal(32, certified.Length);

        // Wave lineage: W2 records its own commit; W0/W1 record their own commit through the Bridge
        // Result only, so their commits are pinned from the child wave's recorded starting head.
        var w0 = sot.RootElement.GetProperty("wishlistAmsc001W0");
        Assert.Equal("ANALYZE_COMPLETE", w0.GetProperty("state").GetString());
        Assert.Equal("READY_TO_MIGRATE", w0.GetProperty("verdict").GetString());
        Assert.Equal("8502b91a77b3f67ff1703df94d990f9537489c12", w0.GetProperty("startingHead").GetString());

        var w1 = sot.RootElement.GetProperty("wishlistAmsc001W1");
        Assert.Equal("MIGRATE_COMPLETE", w1.GetProperty("state").GetString());
        Assert.Equal("e80045ca711a25972858991aa80b8831a50fae1c", w1.GetProperty("startingHead").GetString());

        var w2 = sot.RootElement.GetProperty("wishlistAmsc001W2");
        Assert.Equal("READY_FOR_CERTIFY", w2.GetProperty("structureState").GetString());
        Assert.Equal("d7fa18f7", w2.GetProperty("parentCommit").GetString());
        Assert.Equal("d7fa18f7830b6749df0edc29ccd5ee1366f2cf6c", w2.GetProperty("startingHead").GetString());

        Assert.Equal("03871df68d3204214680afec9aae7992dc481b2e", w3.GetProperty("startingHead").GetString());
        Assert.Equal("TB-TMAR-WISHLIST-AMSC-001-W2", w3.GetProperty("currentStructureAuthority").GetString());
        Assert.Equal("READY_FOR_CERTIFY_CONSUMED_BY_W3", w3.GetProperty("currentStructureState").GetString());
        Assert.Equal("COMPLETE_REFERENCE_PATTERN", w3.GetProperty("verdict").GetString());
        Assert.Equal(
            "03871df68d3204214680afec9aae7992dc481b2e",
            entries[0].GetProperty("structureAuthorityCommit").GetString());
    }

    // ---------------------------------------------------------------------------------------------
    // 2. Endpoint ownership
    // ---------------------------------------------------------------------------------------------

    [Fact]
    public void Wishlist_owns_its_http_surface_with_host_route_count_zero()
    {
        var root = Repo();

        var routeCount = Regex.Matches(
            File.ReadAllText(Path.Combine(root, ModuleRoot, EndpointsFile)),
            @"group\.Map(Get|Post|Put|Patch|Delete)\(").Count;
        Assert.Equal(4, routeCount);

        var module = File.ReadAllText(Path.Combine(root, ModuleRoot, ModuleFile));
        Assert.Contains("MapWishlistModuleEndpoints", module, StringComparison.Ordinal);
        Assert.Contains("app.MapGroup(\"/v1/customer/wishlist\")", module, StringComparison.Ordinal);
        Assert.Contains("WishlistCustomerEndpoints.Map(group)", module, StringComparison.Ordinal);

        // Host owns zero Wishlist routes and no Wishlist production folder.
        var hostRoot = Path.Combine(root, "src/backend/Host/Tooba.Host");
        Assert.False(Directory.Exists(Path.Combine(hostRoot, "Wishlist")));
        var hostWishlistRoutes = Directory.EnumerateFiles(hostRoot, "*.cs", SearchOption.AllDirectories)
            .Where(p => !p.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                        && !p.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .Sum(p => Regex.Matches(File.ReadAllText(p), @"/v1/customer/wishlist").Count);
        Assert.Equal(0, hostWishlistRoutes);
    }

    // ---------------------------------------------------------------------------------------------
    // 3. Certify §6a hard blocker — independent input-provenance / set-equality gate
    // ---------------------------------------------------------------------------------------------

    [Fact]
    public void Shipped_routes_and_actual_dispatched_requests_match_the_classified_matrix_exactly()
    {
        var root = Repo();
        var derived = DeriveDispatches(root);

        var actualMap = derived
            .Select(d => (d.Verb, d.Path, d.Request))
            .OrderBy(x => x.Path, StringComparer.Ordinal)
            .ThenBy(x => x.Verb, StringComparer.Ordinal)
            .ToArray();
        var expectedMap = ExpectedRoutes
            .OrderBy(x => x.Path, StringComparer.Ordinal)
            .ThenBy(x => x.Verb, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(4, derived.Count);
        Assert.Equal(expectedMap.Length, actualMap.Length);
        for (var i = 0; i < expectedMap.Length; i++)
        {
            Assert.Equal(expectedMap[i].Verb, actualMap[i].Verb);
            Assert.Equal(expectedMap[i].Path, actualMap[i].Path);
            Assert.Equal(expectedMap[i].Request, actualMap[i].Request);
        }

        // Every shipped route dispatches through exactly one ISender.Send call site.
        Assert.Equal(4, derived.Count(d => d.SendCount == 1));
    }

    [Fact]
    public void Dispatched_request_set_equals_the_3_required_plus_1_exempt_partition_with_no_orphan_or_duplicate()
    {
        var root = Repo();
        var derived = DeriveDispatches(root);

        var dispatched = derived.Select(d => d.Request).ToArray();
        var classified = ValidatorRequired.Concat(NoValidatorRequired).ToArray();

        Assert.Equal(
            classified.OrderBy(x => x, StringComparer.Ordinal).ToArray(),
            dispatched.Distinct(StringComparer.Ordinal).OrderBy(x => x, StringComparer.Ordinal).ToArray());

        Assert.Equal(dispatched.Length, dispatched.Distinct(StringComparer.Ordinal).Count());
        Assert.Empty(dispatched.Except(classified, StringComparer.Ordinal));
        Assert.Empty(classified.Except(dispatched, StringComparer.Ordinal));
        Assert.Empty(ValidatorRequired.Intersect(NoValidatorRequired, StringComparer.Ordinal));
        Assert.Equal(4, ValidatorRequired.Length + NoValidatorRequired.Length);

        // One-to-one: 4 routes → 4 sends → 4 actual requests → 4 matching handlers.
        var applicationRoot = Path.Combine(root, ModuleRoot, "Tooba.Wishlist.Application");
        foreach (var request in dispatched)
        {
            var declared = Directory.EnumerateFiles(applicationRoot, "*.cs", SearchOption.AllDirectories)
                .Count(p => File.ReadAllText(p).Contains($"record {request}", StringComparison.Ordinal));
            Assert.Equal(1, declared);

            var handlers = Directory.EnumerateFiles(applicationRoot, "*.cs", SearchOption.AllDirectories)
                .Count(p => File.ReadAllText(p).Contains($"IRequestHandler<{request},", StringComparison.Ordinal));
            Assert.Equal(1, handlers);
        }
    }

    [Fact]
    public void Validator_targets_derived_from_concrete_definitions_equal_the_three_required_requests()
    {
        var root = Repo();
        var validationRoot = Path.Combine(root, ModuleRoot, ValidationDir);
        var validators = string.Join("\n", Directory
            .EnumerateFiles(validationRoot, "*.cs", SearchOption.TopDirectoryOnly)
            .Select(File.ReadAllText));

        var targets = Regex.Matches(validators, @"class\s+\w+Validator\s*:\s*AbstractValidator<(\w+)>")
            .Select(m => m.Groups[1].Value)
            .ToArray();

        Assert.Equal(
            ValidatorRequired.OrderBy(x => x, StringComparer.Ordinal).ToArray(),
            targets.Distinct(StringComparer.Ordinal).OrderBy(x => x, StringComparer.Ordinal).ToArray());

        foreach (var exempt in NoValidatorRequired)
        {
            Assert.DoesNotContain($"AbstractValidator<{exempt}>", validators, StringComparison.Ordinal);
        }

        // Validators emit stable machine codes only — never localized prose.
        Assert.DoesNotContain("WithMessage(", validators, StringComparison.Ordinal);
        Assert.Contains("WishlistErrorCodes.", validators, StringComparison.Ordinal);
        Assert.DoesNotContain("\"customer.wishlist.", validators, StringComparison.Ordinal);
    }

    [Fact]
    public void Validators_are_discoverable_through_the_real_cqrs_registration()
    {
        // Non-circular proof: the three transport validators are resolved from a real container built by
        // the canonical foundation registration for the Wishlist Application assembly — not by reflecting
        // over the validator source file.
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddToobaCqrsFoundation(typeof(AddWishlistItemCommand).Assembly);
        using var provider = services.BuildServiceProvider();

        foreach (var required in ValidatorRequired)
        {
            var validatorType = typeof(IValidator<>).MakeGenericType(
                typeof(AddWishlistItemCommand).Assembly.GetTypes().Single(t => t.Name == required));
            Assert.NotNull(provider.GetService(validatorType));
        }

        foreach (var exempt in NoValidatorRequired)
        {
            var validatorType = typeof(IValidator<>).MakeGenericType(
                typeof(AddWishlistItemCommand).Assembly.GetTypes().Single(t => t.Name == exempt));
            Assert.Null(provider.GetService(validatorType));
        }

        // The canonical validation behavior is installed exactly once in the pipeline.
        var behaviors = provider.GetServices<MediatR.IPipelineBehavior<
            AddWishlistItemCommand,
            Tooba.BuildingBlocks.Results.Result<Tooba.Wishlist.Application.Models.WishlistAddResult>>>()
            .ToArray();
        Assert.Equal(1, behaviors.Count(b => b.GetType().IsGenericType
            && b.GetType().GetGenericTypeDefinition() == typeof(ValidationBehavior<,>)));
    }

    [Fact]
    public void Exempt_requests_rest_on_route_guid_constraints_and_server_derived_actors_only()
    {
        var root = Repo();
        var endpoints = File.ReadAllText(Path.Combine(root, ModuleRoot, EndpointsFile));

        // Every route that carries an identifier binds it with the :guid constraint, so a malformed
        // value is rejected by routing before any handler runs.
        foreach (Match route in Regex.Matches(endpoints, @"group\.Map(?:Get|Post|Put|Patch|Delete)\(""(?<path>[^""]*)"""))
        {
            var idSegments = Regex.Matches(route.Groups["path"].Value, @"\{([^}]+)\}")
                .Select(m => m.Groups[1].Value).ToArray();
            Assert.All(idSegments, seg => Assert.EndsWith(":guid", seg, StringComparison.Ordinal));
        }

        // Actors are server-derived from the module resolver seam, never from a client body value.
        Assert.Contains("actorResolver.ResolveActor(httpContext)", endpoints, StringComparison.Ordinal);
        Assert.Contains("new ListWishlistPageQuery(actor.Value)", endpoints, StringComparison.Ordinal);
        Assert.Contains("new AddWishlistItemCommand(actor.Value, productId)", endpoints, StringComparison.Ordinal);
        Assert.Contains("new RemoveWishlistItemCommand(actor.Value, productId)", endpoints, StringComparison.Ordinal);
        Assert.Contains("new GetWishlistMembershipQuery(actor.Value, productIds)", endpoints, StringComparison.Ordinal);

        // The exempt ListWishlistPageQuery binds no client-controlled transport shape at all: its only
        // argument is the server-derived actor and the route carries no parameter.
        Assert.DoesNotContain("ListWishlistPageQuery(", endpoints.Replace("new ListWishlistPageQuery(actor.Value)", string.Empty), StringComparison.Ordinal);

        // The resolver is a neutral seam over the canonical authenticated principal; the Dev/Testing
        // actor header lives only there and never in the endpoint file.
        var resolver = File.ReadAllText(Path.Combine(root, ModuleRoot, "Tooba.Wishlist.Endpoints/Customer/WishlistCustomerActorResolver.cs"));
        Assert.Contains("ICurrentAuthenticatedUser", resolver, StringComparison.Ordinal);
        Assert.Contains("StorefrontGuestActor.ActorId", resolver, StringComparison.Ordinal);
        Assert.DoesNotContain("X-Tooba-Dev-Actor-User-Id", endpoints, StringComparison.Ordinal);
    }

    [Fact]
    public void Replacing_one_dispatched_request_while_preserving_the_send_count_fails_the_set_equality_gate()
    {
        var root = Repo();
        var endpoints = File.ReadAllText(Path.Combine(root, ModuleRoot, EndpointsFile));

        // The mutation: swap the list dispatch for a request that is already part of the matrix. The
        // Send count stays 4 and the *set* of dispatched request types stays identical, so a count-only
        // or set-only guard cannot see the corruption. The exact route→request map can.
        var mutated = endpoints.Replace(
            "new ListWishlistPageQuery(actor.Value)",
            "new AddWishlistItemCommand(actor.Value, productId)",
            StringComparison.Ordinal);
        Assert.NotEqual(endpoints, mutated);

        Assert.Equal(
            Regex.Matches(endpoints, @"sender\.Send\(").Count,
            Regex.Matches(mutated, @"sender\.Send\(").Count);

        var mutatedDispatch = ParseDispatchedRequest(mutated, "ListAsync", EndpointsFile);
        Assert.Equal("AddWishlistItemCommand", mutatedDispatch);

        var derived = ParseRoutes(ModuleGroupPrefix(root), EndpointsFile, mutated).ToArray();
        var mapping = derived
            .Select(r => (r.Path, Request: ParseDispatchedRequest(mutated, r.Handler, EndpointsFile)))
            .ToArray();
        var expected = ExpectedRoutes
            .Select(x => (x.Path, x.Request))
            .ToArray();

        Assert.NotEqual(
            expected.OrderBy(x => x.Path, StringComparer.Ordinal).ToArray(),
            mapping.OrderBy(x => x.Path, StringComparer.Ordinal).ToArray());
    }

    [Fact]
    public void An_untraceable_dispatch_fails_closed()
    {
        var root = Repo();
        var endpoints = File.ReadAllText(Path.Combine(root, ModuleRoot, EndpointsFile));

        Assert.Throws<InvalidOperationException>(
            () => ParseDispatchedRequest(
                endpoints.Replace("new RemoveWishlistItemCommand(", "BuildCommand(", StringComparison.Ordinal),
                "RemoveAsync",
                EndpointsFile));

        Assert.Throws<InvalidOperationException>(
            () => ParseDispatchedRequest(
                "private static async Task<IResult> NothingAsync() { return null; }",
                "NothingAsync",
                EndpointsFile));
    }

    // ---------------------------------------------------------------------------------------------
    // 4. Canonical API result / typed fault / localization / catalog
    // ---------------------------------------------------------------------------------------------

    [Fact]
    public void Wishlist_api_results_typed_fault_and_catalog_are_canonical()
    {
        var root = Repo();
        var production = ProductionSources(root).ToArray();
        var joined = string.Join("\n", production.Select(File.ReadAllText));

        // No message-text fault classification anywhere in production.
        Assert.DoesNotContain(".Message.Contains(", joined, StringComparison.Ordinal);
        Assert.DoesNotContain(".Message.StartsWith(", joined, StringComparison.Ordinal);
        Assert.DoesNotContain(".Message ==", joined, StringComparison.Ordinal);
        Assert.DoesNotContain("WishlistExceptionMapper", joined, StringComparison.Ordinal);
        Assert.DoesNotContain("TryMapExact", joined, StringComparison.Ordinal);

        // Typed-fault seam classifies by typed code only, over both typed mechanisms.
        var seam = File.ReadAllText(Path.Combine(root, ModuleRoot, SeamFile));
        Assert.Contains("catch (ContractOperationException ex) when (WishlistErrorCodes.IsKnown(ex.Code))", seam, StringComparison.Ordinal);
        Assert.Contains("catch (SemanticException ex)", seam, StringComparison.Ordinal);
        Assert.DoesNotContain("ex.Message", seam, StringComparison.Ordinal);

        // Canonical API-result mapping on the success and error paths; no ad-hoc results, no endpoint
        // catch-and-map, no local ProblemDetails builder, no raw Results.Json.
        var endpoints = File.ReadAllText(Path.Combine(root, ModuleRoot, EndpointsFile));
        Assert.DoesNotContain("Results.", endpoints, StringComparison.Ordinal);
        Assert.DoesNotContain("ProblemDetails", endpoints, StringComparison.Ordinal);
        Assert.DoesNotContain("catch (", endpoints, StringComparison.Ordinal);
        Assert.Equal(4, Regex.Matches(endpoints, @"api\.From\(").Count);
        Assert.Equal(1, Regex.Matches(endpoints, @"api\.Created\(").Count);
        Assert.Equal(4, Regex.Matches(endpoints, @"api\.FromFailure\(").Count);

        // Single canonical stable-code home with the declared-code guard: exactly the four
        // Wishlist-owned codes are known, the Foundation-owned session code is consumable but not known.
        Assert.Equal(
            WishlistOwnedCodes.OrderBy(x => x, StringComparer.Ordinal).ToArray(),
            DeclaredCodes().Where(WishlistErrorCodes.IsKnown).OrderBy(x => x, StringComparer.Ordinal).ToArray());
        Assert.Equal(5, DeclaredCodes().Length);
        foreach (var code in WishlistOwnedCodes)
        {
            Assert.True(WishlistErrorCodes.IsKnown(code), code);
        }

        Assert.False(WishlistErrorCodes.IsKnown(WishlistErrorCodes.SessionRequired));
        Assert.False(WishlistErrorCodes.IsKnown(null));

        // The composed catalog is constructible, unique, and the Wishlist-owned keyspaces resolve to
        // exactly the HTTP-reachable descriptor set (one descriptor per client-observable code, and the
        // Foundation-owned session code is never re-registered by Wishlist).
        var catalog = new ErrorDefinitionCatalog(
        [
            new FoundationErrorCatalogContributor(),
            new WishlistErrorCatalogContributor(),
        ]);
        var wishlistRegistered = catalog.RegisteredCodes
            .Where(c => c.StartsWith("customer.wishlist.", StringComparison.Ordinal)
                        || c.StartsWith("wishlist.", StringComparison.Ordinal))
            .OrderBy(c => c, StringComparer.Ordinal).ToArray();
        Assert.Equal(
            WishlistOwnedCodes.OrderBy(c => c, StringComparer.Ordinal).ToArray(),
            wishlistRegistered);
        Assert.True(catalog.TryGet("customer.session.required", out _));
        Assert.DoesNotContain("customer.session.required", wishlistRegistered, StringComparer.Ordinal);

        // Bilingual resources: 4 EN + 4 FA keys, one per declared module code, all resolving through the
        // canonical composed localizer with real Persian text.
        var declared = WishlistOwnedCodes;
        foreach (var resxName in new[] { "WishlistErrors.resx", "WishlistErrors.fa.resx" })
        {
            var resx = File.ReadAllText(Path.Combine(root, ModuleRoot, ResxDirectory, resxName));
            var keys = Regex.Matches(resx, @"data name=""([^""]+)""")
                .Select(m => m.Groups[1].Value).OrderBy(c => c, StringComparer.Ordinal).ToArray();
            Assert.Equal(declared.OrderBy(c => c, StringComparer.Ordinal).ToArray(), keys);
        }

        var localizer = new ResourceErrorMessageLocalizer(
            [new FoundationErrorResourceSet(), new WishlistErrorResourceSet()],
            Array.Empty<IErrorMessageContributor>());
        var cultureEn = CultureInfo.GetCultureInfo("en");
        var cultureFa = CultureInfo.GetCultureInfo("fa");
        foreach (var code in declared)
        {
            var titleEn = localizer.Localize(code, cultureEn, new Dictionary<string, string?>(), "fallback");
            var titleFa = localizer.Localize(code, cultureFa, new Dictionary<string, string?>(), "fallback");
            Assert.NotEqual("fallback", titleEn);
            Assert.NotEqual("fallback", titleFa);
            Assert.True(titleFa.Any(ch => ch is >= '\u0600' and <= '\u06ff'), "expected Persian for " + code);
        }

        // The bilingual logical names are explicit and locked (not left to SDK convention).
        var contractsCsproj = File.ReadAllText(Path.Combine(root, ModuleRoot, "Tooba.Wishlist.Contracts/Tooba.Wishlist.Contracts.csproj"));
        Assert.Contains("Tooba.Wishlist.Contracts.Resources.WishlistErrors.resources", contractsCsproj, StringComparison.Ordinal);
        Assert.Contains("Tooba.Wishlist.Contracts.Resources.WishlistErrors.fa.resources", contractsCsproj, StringComparison.Ordinal);

        // Registered exactly once by the module composition root.
        var module = File.ReadAllText(Path.Combine(root, ModuleRoot, "Tooba.Wishlist.Infrastructure/WishlistModule.cs"));
        Assert.Equal(1, Regex.Matches(module, @"AddSingleton<IErrorCatalogContributor").Count);
        Assert.Equal(1, Regex.Matches(module, @"AddSingleton<IErrorResourceSet").Count);
        Assert.Contains("WishlistErrorCatalogContributor", module, StringComparison.Ordinal);
        Assert.Contains("WishlistErrorResourceSet", module, StringComparison.Ordinal);

        // No ad-hoc logging / telemetry / correlation mechanism.
        Assert.DoesNotContain("Console.WriteLine", joined, StringComparison.Ordinal);
        Assert.DoesNotContain("Debug.WriteLine", joined, StringComparison.Ordinal);
        Assert.DoesNotContain("new ActivitySource(", joined, StringComparison.Ordinal);
        Assert.DoesNotContain("traceparent", joined, StringComparison.Ordinal);
    }

    [Fact]
    public void Wishlist_owned_codes_are_single_owner_descriptors_and_uncatalogued_transport_codes_stay_out()
    {
        var root = Repo();
        var contributor = File.ReadAllText(Path.Combine(root, ModuleRoot, ContributorFile));

        // The contributor registers exactly the four Wishlist-owned codes and never the Foundation code.
        Assert.Equal(4, Regex.Matches(contributor, @"D\(WishlistErrorCodes\.").Count);
        Assert.DoesNotContain("SessionRequired", contributor, StringComparison.Ordinal);
        Assert.DoesNotContain("FoundationErrorCodes", contributor, StringComparison.Ordinal);

        var descriptors = new WishlistErrorCatalogContributor().Contribute().ToArray();
        Assert.Equal(4, descriptors.Length);
        Assert.All(descriptors, d => Assert.Equal(d.Code, d.LocalizationKey));
        Assert.Equal(
            WishlistOwnedCodes.OrderBy(x => x, StringComparer.Ordinal).ToArray(),
            descriptors.Select(d => d.Code).OrderBy(x => x, StringComparer.Ordinal).ToArray());

        // The resource set owns only the module's own keyspace and resolves the Foundation code to null.
        var resourceSet = new WishlistErrorResourceSet();
        Assert.True(resourceSet.Owns(WishlistErrorCodes.ProductUnavailable));
        Assert.True(resourceSet.Owns(WishlistErrorCodes.OutboxUnmappedEventType));
        Assert.False(resourceSet.Owns(WishlistErrorCodes.SessionRequired));
        Assert.False(resourceSet.Owns("order.not_found"));
        Assert.Null(resourceSet.GetString(WishlistErrorCodes.SessionRequired, CultureInfo.GetCultureInfo("en")));
    }

    // ---------------------------------------------------------------------------------------------
    // 5. Contracts-only microservice boundary + unchanged schema
    // ---------------------------------------------------------------------------------------------

    [Fact]
    public void Wishlist_is_contracts_only_and_microservice_extractable()
    {
        var root = Repo();

        foreach (var project in ProductionProjects)
        {
            var csproj = XDocument.Load(Path.Combine(root, ModuleRoot, project, project + ".csproj"));
            var refs = csproj.Descendants("ProjectReference")
                .Select(x => (string?)x.Attribute("Include") ?? string.Empty)
                .ToArray();

            foreach (var reference in refs)
            {
                var normalized = reference.Replace('\\', '/');
                if (normalized.Contains("Tooba.Wishlist.", StringComparison.Ordinal)
                    || normalized.Contains("Tooba.BuildingBlocks", StringComparison.Ordinal)
                    || normalized.Contains("Tooba.Persistence", StringComparison.Ordinal)
                    || normalized.Contains("Tooba.ModuleContracts", StringComparison.Ordinal))
                {
                    continue;
                }

                // Every remaining edge must be a foreign module *.Contracts project.
                Assert.Contains("Tooba.", normalized, StringComparison.Ordinal);
                Assert.Contains(".Contracts/", normalized, StringComparison.Ordinal);
                Assert.DoesNotContain(".Application/", normalized, StringComparison.Ordinal);
                Assert.DoesNotContain(".Infrastructure/", normalized, StringComparison.Ordinal);
                Assert.DoesNotContain(".Domain/", normalized, StringComparison.Ordinal);
                Assert.DoesNotContain(".Endpoints/", normalized, StringComparison.Ordinal);
            }
        }

        // The only foreign edges are the Contracts-only Catalog seams plus the Order.Fulfillment session seam.
        Assert.Contains(ProjectRefs(root, "Tooba.Wishlist.Application"), r => r.Contains("Catalog.Contracts", StringComparison.Ordinal));
        var infrastructureRefs = ProjectRefs(root, "Tooba.Wishlist.Infrastructure");
        Assert.Contains(infrastructureRefs, r => r.Contains("Catalog.Contracts", StringComparison.Ordinal));
        Assert.Contains(infrastructureRefs, r => r.Contains("Order.Contracts", StringComparison.Ordinal));
        Assert.Contains(ProjectRefs(root, "Tooba.Wishlist.Endpoints"), r => r.Contains("Order.Contracts", StringComparison.Ordinal));

        // Endpoints must never reach Infrastructure or Host.
        var endpointsRefs = ProjectRefs(root, "Tooba.Wishlist.Endpoints");
        Assert.Contains(endpointsRefs, r => r.Contains("Wishlist.Application", StringComparison.Ordinal));
        Assert.DoesNotContain(endpointsRefs, r => r.Contains("Wishlist.Infrastructure", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(endpointsRefs, r => r.Contains("Tooba.Host", StringComparison.OrdinalIgnoreCase));

        // Domain depends only on BuildingBlocks + Wishlist.Contracts.
        var domainRefs = ProjectRefs(root, "Tooba.Wishlist.Domain");
        Assert.Contains(domainRefs, r => r.Contains("Tooba.Wishlist.Contracts", StringComparison.Ordinal));
        Assert.DoesNotContain(domainRefs, r => r.Contains("Wishlist.Application", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(domainRefs, r => r.Contains("Wishlist.Infrastructure", StringComparison.OrdinalIgnoreCase));

        // Zero foreign persistence reach-through and zero cross-module join.
        var joined = string.Join("\n", ProductionSources(root).Select(File.ReadAllText));
        foreach (var foreignContext in new[]
                 {
                     "CatalogDbContext", "OrderDbContext", "NotificationDbContext", "IdentityDbContext",
                     "AccessControlDbContext", "PaymentDbContext", "WalletDbContext", "CartDbContext",
                     "InventoryDbContext",
                 })
        {
            Assert.DoesNotContain(foreignContext, joined, StringComparison.Ordinal);
        }

        foreach (var foreign in new[]
                 {
                     "Catalog.Application", "Catalog.Infrastructure", "Catalog.Domain",
                     "Order.Application", "Order.Infrastructure", "Order.Domain",
                     "Notification.Application", "Notification.Infrastructure", "Notification.Domain",
                     "Identity.Application", "Identity.Infrastructure", "Identity.Domain",
                     "Payment.Application", "Payment.Infrastructure", "Payment.Domain",
                 })
        {
            Assert.DoesNotContain($"using Tooba.{foreign}", joined, StringComparison.Ordinal);
        }

        Assert.DoesNotContain("TypeForwardedTo", joined, StringComparison.Ordinal);

        // Own schema and unchanged migration set.
        var context = File.ReadAllText(Path.Combine(root, ModuleRoot, "Tooba.Wishlist.Infrastructure/Persistence/WishlistDbContext.cs"));
        Assert.Contains("public const string Schema = \"wishlist\"", context, StringComparison.Ordinal);
        Assert.Contains("HasDefaultSchema(Schema)", context, StringComparison.Ordinal);
        Assert.Contains("ToTable(\"wishlist_items\")", context, StringComparison.Ordinal);

        var migrations = Directory.EnumerateFiles(
                Path.Combine(root, ModuleRoot, "Tooba.Wishlist.Infrastructure/Persistence/Migrations"), "*.cs", SearchOption.TopDirectoryOnly)
            .Select(Path.GetFileName).OrderBy(n => n, StringComparer.Ordinal).ToArray();
        Assert.Equal(
            ["20260825162434_InitialWishlist.Designer.cs", "20260825162434_InitialWishlist.cs", "WishlistDbContextModelSnapshot.cs"],
            migrations);
    }

    // ---------------------------------------------------------------------------------------------
    // 6. Wave evidence + preserved Host closure
    // ---------------------------------------------------------------------------------------------

    [Fact]
    public void Wishlist_wave_evidence_and_host_closure_are_preserved()
    {
        var root = Repo();

        foreach (var wave in new[] { "W0", "W1", "W2", "W3" })
        {
            Assert.True(Directory.Exists(Path.Combine(
                root, "docs/architecture/evidence", $"TB-TMAR-WISHLIST-AMSC-001-{wave}")),
                $"evidence directory for {wave} is required");
        }

        var w3Evidence = File.ReadAllText(Path.Combine(
            root, "docs/architecture/evidence/TB-TMAR-WISHLIST-AMSC-001-W3/certification.md"));
        Assert.Contains("ARCH-COMPLETE-002", w3Evidence, StringComparison.Ordinal);
        Assert.Contains("COMPLETE_REFERENCE_PATTERN", w3Evidence, StringComparison.Ordinal);
        Assert.Contains("STRUCTURE_CERTIFIED", w3Evidence, StringComparison.Ordinal);

        // Repository-global Host root checkpoint must remain untouched by this module-local task.
        using var sot = JsonDocument.Parse(File.ReadAllText(Path.Combine(
            root, "docs/architecture/tmar-current-state.json")).Replace("\uFEFF", string.Empty));
        Assert.Equal("TB-TMAR-HOST-ROOT-FINAL-CERT-001", sot.RootElement.GetProperty("lastAcceptedTask").GetString());
        Assert.Equal("HOST_ROOT_FINAL_CERTIFIED", sot.RootElement.GetProperty("currentHostCheckpoint").GetString());
        Assert.Equal("NONE", sot.RootElement.GetProperty("automaticNextImplementationTask").GetString());

        // Master Recovery records the Wishlist certification checkpoint.
        var recovery = File.ReadAllText(Path.Combine(root, "docs/architecture/TOOBA-TMAR-MASTER-RECOVERY.md"));
        Assert.Contains("Wishlist AMSC module recovery checkpoint W3 (authoritative, module-local)", recovery, StringComparison.Ordinal);
        Assert.Contains("TB-TMAR-WISHLIST-AMSC-001-W3", recovery, StringComparison.Ordinal);
        Assert.Contains("USER_REVIEW_WISHLIST_AMSC_001_W3", recovery, StringComparison.Ordinal);
    }

    // ---------------------------------------------------------------------------------------------
    // Deterministic, fail-closed source parser
    // ---------------------------------------------------------------------------------------------

    private sealed record RouteEntry(string Verb, string Path, string Handler);

    private sealed record Dispatch(string Verb, string Path, string Handler, string Request, int SendCount);

    private static IReadOnlyList<Dispatch> DeriveDispatches(string root)
    {
        var source = File.ReadAllText(Path.Combine(root, ModuleRoot, EndpointsFile));
        var prefix = ModuleGroupPrefix(root);
        var dispatches = new List<Dispatch>();
        foreach (var route in ParseRoutes(prefix, EndpointsFile, source))
        {
            var body = ExtractHandlerBody(source, route.Handler);
            var sendCount = Regex.Matches(body, @"sender\.Send\(").Count;
            var request = ParseDispatchedRequest(source, route.Handler, EndpointsFile);
            dispatches.Add(new Dispatch(route.Verb, route.Path, route.Handler, request, sendCount));
        }

        return dispatches;
    }

    /// <summary>Reads the module-owned route-group prefix from the composition entry.</summary>
    private static string ModuleGroupPrefix(string root)
    {
        var module = File.ReadAllText(Path.Combine(root, ModuleRoot, ModuleFile));
        var match = Regex.Match(module, @"MapGroup\(""(?<prefix>[^""]+)""\)");
        if (!match.Success)
        {
            throw new InvalidOperationException("route-group prefix could not be derived");
        }

        return match.Groups["prefix"].Value;
    }

    private static IEnumerable<RouteEntry> ParseRoutes(string prefix, string file, string source)
    {
        var matches = Regex.Matches(
            source,
            @"group\.Map(?<verb>Get|Post|Put|Patch|Delete)\(""(?<path>[^""]*)""\s*,\s*(?<handler>[A-Za-z_][A-Za-z0-9_]*)\)");

        if (matches.Count == 0)
        {
            throw new InvalidOperationException($"no route mappings parsed from {file}");
        }

        foreach (Match match in matches)
        {
            yield return new RouteEntry(
                match.Groups["verb"].Value,
                prefix + match.Groups["path"].Value,
                match.Groups["handler"].Value);
        }
    }

    /// <summary>
    /// Derives the request type actually passed to <c>ISender.Send</c> inside <paramref name="handler"/>.
    /// Supports both <c>Send(new T(...))</c> and <c>Send(variable)</c> by tracing the local construction.
    /// Fails closed (throws) whenever the dispatch cannot be determined with certainty.
    /// </summary>
    private static string ParseDispatchedRequest(string source, string handler, string file)
    {
        var body = ExtractHandlerBody(source, handler);

        var sendIndex = body.IndexOf("sender.Send(", StringComparison.Ordinal);
        if (sendIndex < 0)
        {
            throw new InvalidOperationException(
                $"{file}:{handler} has no ISender.Send call; dispatch provenance cannot be derived");
        }

        var argument = body[(sendIndex + "sender.Send(".Length)..].TrimStart();

        // Case A — inline construction: Send(new T(...), ...)
        if (argument.StartsWith("new ", StringComparison.Ordinal))
        {
            return ReadTypeName(argument["new ".Length..], file, handler);
        }

        // Case B — variable dispatch: trace the local construction in the same handler body.
        var identifier = ReadIdentifier(argument);
        if (identifier.Length == 0)
        {
            throw new InvalidOperationException(
                $"{file}:{handler} dispatches an unrecognised expression: '{argument[..Math.Min(60, argument.Length)]}'");
        }

        var construction = Regex.Match(
            body,
            $@"(?:var|[A-Za-z_][A-Za-z0-9_<>.,\s]*?)\s+{Regex.Escape(identifier)}\s*=\s*new\s+(?<type>[A-Za-z_][A-Za-z0-9_.]*)",
            RegexOptions.Singleline);
        if (!construction.Success)
        {
            throw new InvalidOperationException(
                $"{file}:{handler} dispatches variable '{identifier}' whose construction cannot be traced; failing closed");
        }

        return construction.Groups["type"].Value;
    }

    private static string ReadTypeName(string tail, string file, string handler)
    {
        var match = Regex.Match(tail, @"^(?<type>[A-Za-z_][A-Za-z0-9_.]*)");
        if (!match.Success)
        {
            throw new InvalidOperationException($"{file}:{handler} has an unreadable constructed request type");
        }

        var type = match.Groups["type"].Value;
        return type.Contains('.') ? type[(type.LastIndexOf('.') + 1)..] : type;
    }

    private static string ReadIdentifier(string tail)
    {
        var match = Regex.Match(tail, @"^(?<id>[A-Za-z_][A-Za-z0-9_]*)");
        return match.Success ? match.Groups["id"].Value : string.Empty;
    }

    /// <summary>Extracts the brace-balanced body of a handler method by its declared name.</summary>
    private static string ExtractHandlerBody(string source, string handler)
    {
        var signature = Regex.Match(
            source,
            $@"[A-Za-z_][A-Za-z0-9_<>.,\s]*?\s{Regex.Escape(handler)}\s*\(");
        if (!signature.Success)
        {
            throw new InvalidOperationException($"handler '{handler}' not found");
        }

        var open = source.IndexOf('{', signature.Index + signature.Length);
        if (open < 0)
        {
            throw new InvalidOperationException($"handler '{handler}' has no body");
        }

        var depth = 0;
        for (var i = open; i < source.Length; i++)
        {
            switch (source[i])
            {
                case '{':
                    depth++;
                    break;
                case '}':
                    depth--;
                    if (depth == 0)
                    {
                        return source[open..(i + 1)];
                    }

                    break;
            }
        }

        throw new InvalidOperationException($"handler '{handler}' body is not brace-balanced");
    }

    private static string[] DeclaredCodes() =>
        typeof(WishlistErrorCodes)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(f => f.IsLiteral && f.FieldType == typeof(string))
            .Select(f => (string)f.GetRawConstantValue()!)
            .OrderBy(c => c, StringComparer.Ordinal)
            .ToArray();

    private static IEnumerable<string> ProductionSources(string root) =>
        ProductionProjects
            .SelectMany(p => Directory.EnumerateFiles(
                Path.Combine(root, ModuleRoot, p), "*.cs", SearchOption.AllDirectories))
            .Where(p => !p.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                        && !p.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                        && !p.Contains($"{Path.DirectorySeparatorChar}Migrations{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                        && !Path.GetFileName(p).EndsWith("ModelSnapshot.cs", StringComparison.OrdinalIgnoreCase));

    private static IReadOnlyList<string> ProjectRefs(string root, string project) =>
        XDocument.Load(Path.Combine(root, ModuleRoot, project, project + ".csproj"))
            .Descendants("ProjectReference")
            .Select(x => (string?)x.Attribute("Include") ?? string.Empty)
            .Where(x => x.Length > 0)
            .ToArray();

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
