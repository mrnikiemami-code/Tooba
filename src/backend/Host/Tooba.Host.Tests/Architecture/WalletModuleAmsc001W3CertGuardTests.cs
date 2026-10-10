using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Localization;
using Tooba.BuildingBlocks.Presentation.Errors;
using Tooba.Wallet.Application.Customer.Commands;
using Tooba.Wallet.Contracts.Errors;
using Tooba.Wallet.Endpoints.Admin;
using Tooba.Wallet.Endpoints.Contracts.Errors;
using Tooba.Wallet.Endpoints.Contracts.Errors.Resources;
using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>
/// TB-TMAR-WALLET-AMSC-001-W3 — durable ARCH-COMPLETE-002 certification lock for Wallet.
///
/// Locks the SoT/manifest certification records with the AMSC wave lineage, the module-owned HTTP
/// surface with a Host route count of zero, the exhaustive 11-row HTTP → request → handler →
/// validator provenance set equality (certify §6a hard gate, derived from the endpoint source itself),
/// the canonical API-result/typed-fault/localization/catalog mechanisms, the Contracts-only
/// microservice-extractable boundary, the unchanged Wallet schema, and the preserved Host final
/// closure checkpoints.
/// </summary>
public sealed class WalletModuleAmsc001W3CertGuardTests
{
    private const string ModuleRoot = "src/backend/Modules/Wallet";

    private const string CustomerFile = "Tooba.Wallet.Endpoints/Customer/WalletCustomerEndpoints.cs";
    private const string AdminFile = "Tooba.Wallet.Endpoints/Admin/WalletAdminEndpoints.cs";
    private const string ModuleFile = "Tooba.Wallet.Endpoints/WalletEndpointModule.cs";
    private const string ValidatorsFile = "Tooba.Wallet.Application/Validation/WalletRequestValidators.cs";
    private const string ValidationCodesFile = "Tooba.Wallet.Application/Validation/WalletValidationCodes.cs";
    private const string CodesFile = "Tooba.Wallet.Contracts/Errors/WalletErrorCodes.cs";
    private const string ContributorFile = "Tooba.Wallet.Endpoints/Contracts/Errors/WalletErrorCatalogContributor.cs";
    private const string ResourceSetFile = "Tooba.Wallet.Endpoints/Contracts/Errors/Resources/WalletErrorResources.cs";
    private const string ResxDirectory = "Tooba.Wallet.Endpoints/Contracts/Errors/Resources";

    private static readonly string[] ProductionProjects =
    [
        "Tooba.Wallet.Contracts",
        "Tooba.Wallet.Domain",
        "Tooba.Wallet.Application",
        "Tooba.Wallet.Infrastructure",
        "Tooba.Wallet.Endpoints",
    ];

    private static readonly string[] ValidatorRequired =
    [
        "RedeemCustomerGiftCardCommand",
        "IssueAdminGiftCardCommand",
        "ListAdminGiftCardsQuery",
        "AdjustAdminWalletCommand",
    ];

    private static readonly string[] NoValidatorRequired =
    [
        "GetCustomerWalletSummaryQuery", "ListCustomerWalletLedgerQuery", "GetAdminGiftCardQuery",
        "RevokeAdminGiftCardCommand", "GetAdminWalletQuery", "ListAdminWalletLedgerQuery",
        "GetWalletDemoPreviewQuery",
    ];

    /// <summary>Exact expected audience → verb → path → dispatched request map (11 rows).</summary>
    private static readonly (string Audience, string Verb, string Path, string Request)[] ExpectedRoutes =
    [
        ("customer", "Get", "/v1/customer/wallet", "GetCustomerWalletSummaryQuery"),
        ("customer", "Get", "/v1/customer/wallet/ledger", "ListCustomerWalletLedgerQuery"),
        ("customer", "Post", "/v1/customer/wallet/gift-cards/redeem", "RedeemCustomerGiftCardCommand"),
        ("admin", "Get", "/v1/admin/gift-cards", "ListAdminGiftCardsQuery"),
        ("admin", "Post", "/v1/admin/gift-cards", "IssueAdminGiftCardCommand"),
        ("admin", "Get", "/v1/admin/gift-cards/{cardId:guid}", "GetAdminGiftCardQuery"),
        ("admin", "Post", "/v1/admin/gift-cards/{cardId:guid}/revoke", "RevokeAdminGiftCardCommand"),
        ("admin", "Get", "/v1/admin/wallets/{customerActorUserId:guid}", "GetAdminWalletQuery"),
        ("admin", "Get", "/v1/admin/wallets/{customerActorUserId:guid}/ledger", "ListAdminWalletLedgerQuery"),
        ("admin", "Post", "/v1/admin/wallets/{customerActorUserId:guid}/adjustments", "AdjustAdminWalletCommand"),
        ("admin", "Get", "/v1/admin/wallet/demo-preview", "GetWalletDemoPreviewQuery"),
    ];

    // ---------------------------------------------------------------------------------------------
    // 1. Certification truth (manifest + SoT + wave lineage)
    // ---------------------------------------------------------------------------------------------

    [Fact]
    public void Wallet_is_arch_complete_002_certified_in_manifest_and_sot()
    {
        var root = Repo();

        using var manifest = JsonDocument.Parse(File.ReadAllText(Path.Combine(
            root, "docs/architecture/tmar-module-structure-manifests.json")).Replace("\uFEFF", string.Empty));
        var entries = manifest.RootElement.GetProperty("modules").EnumerateArray()
            .Where(m => string.Equals(m.GetProperty("module").GetString(), "Wallet", StringComparison.Ordinal))
            .ToArray();
        Assert.Single(entries);
        Assert.True(entries[0].GetProperty("structureCertified").GetBoolean());
        Assert.Equal("ARCH-COMPLETE-002", entries[0].GetProperty("lockVersion").GetString());
        Assert.Contains(
            "TB-TMAR-WALLET-AMSC-001-W3",
            entries[0].GetProperty("certificationNote").GetString()!,
            StringComparison.Ordinal);
        Assert.Equal(5, entries[0].GetProperty("projects").GetArrayLength());

        // Promotion is a move, not a copy.
        Assert.DoesNotContain(
            manifest.RootElement.GetProperty("preCertModules").EnumerateArray(),
            m => string.Equals(m.GetProperty("module").GetString(), "Wallet", StringComparison.Ordinal));
        Assert.DoesNotContain(
            "Wallet",
            manifest.RootElement.GetProperty("uncertifiedHttpOwningModules").EnumerateArray()
                .Select(x => x.GetString()!),
            StringComparer.Ordinal);

        using var sot = JsonDocument.Parse(File.ReadAllText(Path.Combine(
            root, "docs/architecture/tmar-current-state.json")).Replace("\uFEFF", string.Empty));
        var w3 = sot.RootElement.GetProperty("walletAmsc001W3");
        Assert.Equal("TB-TMAR-WALLET-AMSC-001-W3", w3.GetProperty("task").GetString());
        Assert.Equal("TB-TMAR-WALLET-AMSC-001-W2", w3.GetProperty("parentTask").GetString());
        Assert.Equal("WALLET_AMSC_001_CERTIFIED", w3.GetProperty("state").GetString());
        Assert.Equal("COMPLETE_REFERENCE_PATTERN", w3.GetProperty("verdict").GetString());
        Assert.Equal("ARCH-COMPLETE-002", w3.GetProperty("lockVersion").GetString());
        Assert.True(w3.GetProperty("structureCertified").GetBoolean());
        Assert.Equal("HTTP_OWNING", w3.GetProperty("httpApplicability").GetString());
        Assert.Equal("MODULE_OWNED_11_ROUTES_HOST_ZERO", w3.GetProperty("endpointOwnershipState").GetString());
        Assert.Equal(11, w3.GetProperty("moduleOwnedRouteCount").GetInt32());
        Assert.Equal(0, w3.GetProperty("hostOwnedRouteCount").GetInt32());
        Assert.Equal(11, w3.GetProperty("endpointReachableRequests").GetInt32());
        Assert.Equal("MEDIATR_12_5_ISENDER_11_OF_11", w3.GetProperty("cqrsState").GetString());
        Assert.Equal("EXHAUSTIVE_4_VALIDATOR_REQUIRED_7_NO_VALIDATOR_REQUIRED", w3.GetProperty("validatorMatrixState").GetString());
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
        Assert.Equal("USER_REVIEW_WALLET_AMSC_001_W3", w3.GetProperty("workflowStop").GetString());
        Assert.Equal("NONE", w3.GetProperty("automaticNextImplementationTask").GetString());

        var certified = sot.RootElement.GetProperty("structureLock").GetProperty("certifiedModules")
            .EnumerateArray().Select(x => x.GetString()).ToArray();
        Assert.Equal(1, certified.Count(x => x == "Wallet"));
        Assert.Equal(32, certified.Length);

        // Wave lineage: every wave starts from its parent wave's commit, so the chain is verifiable
        // end to end. W0/W1 record their own commit through the Bridge Result only (no
        // self-referential placeholder is written into the tree), so their commits are pinned from
        // the child wave's recorded starting head instead.
        var w0 = sot.RootElement.GetProperty("walletAmsc001W0");
        Assert.Equal("ANALYZE_COMPLETE", w0.GetProperty("state").GetString());
        Assert.Equal("READY_TO_MIGRATE", w0.GetProperty("verdict").GetString());
        Assert.Equal("4f875042176c2c1f249acffeaad795a2971dd310", w0.GetProperty("startingHead").GetString());

        var w1 = sot.RootElement.GetProperty("walletAmsc001W1");
        Assert.Equal("MIGRATE_COMPLETE", w1.GetProperty("state").GetString());
        Assert.Equal("7ab117fb", w1.GetProperty("startingHead").GetString());

        var w2 = sot.RootElement.GetProperty("walletAmsc001W2");
        Assert.Equal("READY_FOR_CERTIFY", w2.GetProperty("structureState").GetString());
        Assert.Equal("c802fe81", w2.GetProperty("parentCommit").GetString());
        Assert.Equal("779cd2f4", w2.GetProperty("commit").GetString());

        Assert.Equal("779cd2f4f08cbc5bd90741384d8cef7d4a049e96", w3.GetProperty("startingHead").GetString());
        Assert.Equal("TB-TMAR-WALLET-AMSC-001-W2", w3.GetProperty("currentStructureAuthority").GetString());
        Assert.Equal("READY_FOR_CERTIFY_CONSUMED_BY_W3", w3.GetProperty("currentStructureState").GetString());
    }

    // ---------------------------------------------------------------------------------------------
    // 2. Endpoint ownership
    // ---------------------------------------------------------------------------------------------

    [Fact]
    public void Wallet_owns_its_http_surface_with_host_route_count_zero()
    {
        var root = Repo();

        var routeCount = new[] { CustomerFile, AdminFile }
            .Sum(f => Regex.Matches(
                File.ReadAllText(Path.Combine(root, ModuleRoot, f)),
                @"app\.Map(Get|Post|Put|Patch|Delete)\(").Count);
        Assert.Equal(11, routeCount);

        var module = File.ReadAllText(Path.Combine(root, ModuleRoot, ModuleFile));
        Assert.Contains("MapWalletEndpoints", module, StringComparison.Ordinal);
        Assert.Contains("WalletCustomerEndpoints.Map(app)", module, StringComparison.Ordinal);
        Assert.Contains("WalletAdminEndpoints.Map(app)", module, StringComparison.Ordinal);

        // Host owns zero Wallet routes and no Wallet production folder.
        var hostRoot = Path.Combine(root, "src/backend/Host/Tooba.Host");
        Assert.False(Directory.Exists(Path.Combine(hostRoot, "Wallet")));
        var hostWalletRoutes = Directory.EnumerateFiles(hostRoot, "*.cs", SearchOption.AllDirectories)
            .Where(p => !p.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                        && !p.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .Sum(p => Regex.Matches(File.ReadAllText(p), @"/v1/(customer/wallet|admin/gift-cards|admin/wallets|admin/wallet/)").Count);
        Assert.Equal(0, hostWalletRoutes);
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
            .Select(d => (d.Audience, d.Verb, d.Path, d.Request))
            .OrderBy(x => x.Path, StringComparer.Ordinal)
            .ThenBy(x => x.Verb, StringComparer.Ordinal)
            .ToArray();
        var expectedMap = ExpectedRoutes
            .OrderBy(x => x.Path, StringComparer.Ordinal)
            .ThenBy(x => x.Verb, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(11, derived.Count);
        Assert.Equal(expectedMap.Length, actualMap.Length);
        for (var i = 0; i < expectedMap.Length; i++)
        {
            Assert.Equal(expectedMap[i].Audience, actualMap[i].Audience);
            Assert.Equal(expectedMap[i].Verb, actualMap[i].Verb);
            Assert.Equal(expectedMap[i].Path, actualMap[i].Path);
            Assert.Equal(expectedMap[i].Request, actualMap[i].Request);
        }

        // Every shipped route dispatches through exactly one ISender.Send call site.
        Assert.Equal(11, derived.Count(d => d.SendCount == 1));
    }

    [Fact]
    public void Dispatched_request_set_equals_the_4_required_plus_7_exempt_partition_with_no_orphan_or_duplicate()
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
        Assert.Equal(11, ValidatorRequired.Length + NoValidatorRequired.Length);

        // One-to-one: 11 routes → 11 sends → 11 actual requests → 11 matching handlers.
        var applicationRoot = Path.Combine(root, ModuleRoot, "Tooba.Wallet.Application");
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
    public void Validator_targets_derived_from_concrete_definitions_equal_the_four_required_requests()
    {
        var root = Repo();
        var validators = File.ReadAllText(Path.Combine(root, ModuleRoot, ValidatorsFile));

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
        Assert.Contains("WalletValidationCodes.", validators, StringComparison.Ordinal);
    }

    [Fact]
    public void Validators_are_discoverable_through_the_real_cqrs_registration()
    {
        // Non-circular proof: the four transport validators are resolved from a real container built by
        // the canonical foundation registration for the Wallet Application assembly — not by reflecting
        // over the validator source file.
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddToobaCqrsFoundation(typeof(RedeemCustomerGiftCardCommand).Assembly);
        using var provider = services.BuildServiceProvider();

        foreach (var required in ValidatorRequired)
        {
            var validatorType = typeof(IValidator<>).MakeGenericType(
                typeof(RedeemCustomerGiftCardCommand).Assembly.GetTypes()
                    .Single(t => t.Name == required));
            Assert.NotNull(provider.GetService(validatorType));
        }

        foreach (var exempt in NoValidatorRequired)
        {
            var validatorType = typeof(IValidator<>).MakeGenericType(
                typeof(RedeemCustomerGiftCardCommand).Assembly.GetTypes()
                    .Single(t => t.Name == exempt));
            Assert.Null(provider.GetService(validatorType));
        }

        // The canonical validation behavior is installed exactly once in the pipeline.
        var behaviors = provider.GetServices<MediatR.IPipelineBehavior<RedeemCustomerGiftCardCommand, Tooba.BuildingBlocks.Results.Result<Tooba.Wallet.Application.Customer.Models.GiftCardRedeemResultDto>>>()
            .ToArray();
        Assert.Equal(1, behaviors.Count(b => b.GetType().IsGenericType
            && b.GetType().GetGenericTypeDefinition() == typeof(ValidationBehavior<,>)));
    }

    [Fact]
    public void Exempt_requests_rest_on_route_guid_constraints_and_server_derived_actors_only()
    {
        var root = Repo();
        var customer = File.ReadAllText(Path.Combine(root, ModuleRoot, CustomerFile));
        var admin = File.ReadAllText(Path.Combine(root, ModuleRoot, AdminFile));

        // Every route that carries an identifier binds it with the :guid constraint, so a malformed
        // value is rejected by routing before any handler runs.
        foreach (var file in new[] { customer, admin })
        {
            foreach (Match route in Regex.Matches(file, @"app\.Map(?:Get|Post|Put|Patch|Delete)\(""(?<path>[^""]+)"""))
            {
                var idSegments = Regex.Matches(route.Groups["path"].Value, @"\{([^}]+)\}")
                    .Select(m => m.Groups[1].Value).ToArray();
                Assert.All(idSegments, seg => Assert.EndsWith(":guid", seg, StringComparison.Ordinal));
            }
        }

        // Actors/parties are server-derived from the authorizer seam, never from a client body value.
        Assert.Contains("authorizer.TryResolveActor(context)", customer, StringComparison.Ordinal);
        Assert.Contains("new GetCustomerWalletSummaryQuery(actor.Value)", customer, StringComparison.Ordinal);
        Assert.Contains("new ListCustomerWalletLedgerQuery(actor.Value, page, pageSize)", customer, StringComparison.Ordinal);
        Assert.Contains("await authorizer.RequireAuthorizedAsync(context, \"giftcard.view\", cancellationToken)", admin, StringComparison.Ordinal);
        Assert.Contains("await authorizer.RequireAuthorizedAsync(context, \"wallet.adjust\", cancellationToken)", admin, StringComparison.Ordinal);
        Assert.Contains("new ListAdminGiftCardsQuery(status, q, page, pageSize)", admin, StringComparison.Ordinal);
        Assert.Contains("new GetAdminWalletQuery(customerActorUserId)", admin, StringComparison.Ordinal);
        Assert.Contains("new GetWalletDemoPreviewQuery()", admin, StringComparison.Ordinal);

        // The demo-preview exemption is additionally gated on the Development environment.
        var demoHandler = ExtractHandlerBody(admin, "DemoPreviewAsync");
        Assert.Contains("environment.IsDevelopment()", demoHandler, StringComparison.Ordinal);
        Assert.Contains("Results.NotFound()", demoHandler, StringComparison.Ordinal);

        // The Development/Testing-only actor header lives in the module-owned customer authorizer and
        // never in the admin endpoint file.
        Assert.DoesNotContain("X-Tooba-Dev-Actor-User-Id", admin, StringComparison.Ordinal);
    }

    [Fact]
    public void Replacing_one_dispatched_request_while_preserving_the_send_count_fails_the_set_equality_gate()
    {
        var root = Repo();
        var customer = File.ReadAllText(Path.Combine(root, ModuleRoot, CustomerFile));

        // The mutation: swap the customer summary dispatch for a request that is already part of the
        // matrix. The Send count stays 3 and the *set* of dispatched request types stays identical, so a
        // count-only or set-only guard cannot see the corruption. The exact route→request map can.
        var mutated = customer.Replace(
            "new GetCustomerWalletSummaryQuery(actor.Value)",
            "new ListCustomerWalletLedgerQuery(actor.Value, 1, 20)",
            StringComparison.Ordinal);
        Assert.NotEqual(customer, mutated);

        Assert.Equal(
            Regex.Matches(customer, @"sender\.Send\(").Count,
            Regex.Matches(mutated, @"sender\.Send\(").Count);

        var mutatedDispatch = ParseDispatchedRequest(mutated, "SummaryAsync", CustomerFile);
        Assert.Equal("ListCustomerWalletLedgerQuery", mutatedDispatch);

        var derived = ParseRoutes("customer", CustomerFile, mutated).ToArray();
        var mapping = derived
            .Select(r => (r.Path, Request: ParseDispatchedRequest(mutated, r.Handler, CustomerFile)))
            .ToArray();
        var expected = ExpectedRoutes.Where(x => x.Audience == "customer")
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
        var customer = File.ReadAllText(Path.Combine(root, ModuleRoot, CustomerFile));

        Assert.Throws<InvalidOperationException>(
            () => ParseDispatchedRequest(
                customer.Replace("new ListCustomerWalletLedgerQuery(", "BuildQuery(", StringComparison.Ordinal),
                "LedgerAsync",
                CustomerFile));

        Assert.Throws<InvalidOperationException>(
            () => ParseDispatchedRequest(
                "private static async Task<IResult> NothingAsync() { return null; }",
                "NothingAsync",
                CustomerFile));
    }

    // ---------------------------------------------------------------------------------------------
    // 4. Canonical API result / typed fault / localization / catalog
    // ---------------------------------------------------------------------------------------------

    [Fact]
    public void Wallet_api_results_typed_fault_and_catalog_are_canonical()
    {
        var root = Repo();
        var production = ProductionSources(root).ToArray();
        var joined = string.Join("\n", production.Select(File.ReadAllText));

        // No message-text fault classification anywhere in production.
        Assert.DoesNotContain(".Message.Contains(", joined, StringComparison.Ordinal);
        Assert.DoesNotContain(".Message.StartsWith(", joined, StringComparison.Ordinal);
        Assert.DoesNotContain(".Message ==", joined, StringComparison.Ordinal);
        Assert.DoesNotContain("WalletExceptionMapper", joined, StringComparison.Ordinal);
        Assert.DoesNotContain("TryMapExact", joined, StringComparison.Ordinal);
        Assert.False(Directory.Exists(Path.Combine(root, ModuleRoot, "Tooba.Wallet.Application", "Errors")));

        // Typed-fault seam classifies by typed code only.
        var seam = File.ReadAllText(Path.Combine(
            root, ModuleRoot, "Tooba.Wallet.Application/Composition/WalletOperation.cs"));
        Assert.Contains("catch (ContractOperationException ex) when (WalletErrorCodes.IsKnown(ex.Code))", seam, StringComparison.Ordinal);
        Assert.Contains("WalletErrorCodes.IsHttpReachable", seam, StringComparison.Ordinal);
        Assert.DoesNotContain("ex.Message", seam, StringComparison.Ordinal);

        // Canonical API-result mapping on the success and error paths; the single Development-gated
        // Results.NotFound() remains the locked demo-preview precondition shape.
        var endpoints = new[] { CustomerFile, AdminFile }
            .Select(f => File.ReadAllText(Path.Combine(root, ModuleRoot, f)))
            .ToArray();
        var endpointJoined = string.Join("\n", endpoints);
        Assert.DoesNotContain("Results.BadRequest", endpointJoined, StringComparison.Ordinal);
        Assert.DoesNotContain("Results.Problem", endpointJoined, StringComparison.Ordinal);
        Assert.DoesNotContain("ProblemDetails", endpointJoined, StringComparison.Ordinal);
        Assert.DoesNotContain("Results.Json", endpointJoined, StringComparison.Ordinal);
        Assert.Equal(1, Regex.Matches(endpointJoined, @"Results\.NotFound\(\)").Count);
        Assert.DoesNotContain("catch (", endpointJoined, StringComparison.Ordinal);
        Assert.Equal(11, Regex.Matches(endpointJoined, @"api\.From\(").Count);

        // Single canonical stable-code home with the declared reachability split preserved.
        Assert.Equal(10, WalletErrorCodes.HttpReachable.Count);
        Assert.Equal(40, WalletErrorCodes.DomainInvariants.Count);
        Assert.Equal(50, WalletErrorCodes.HttpReachable.Count + WalletErrorCodes.DomainInvariants.Count);

        var contributor = File.ReadAllText(Path.Combine(root, ModuleRoot, ContributorFile));
        Assert.Equal(10, Regex.Matches(contributor, @"D\(Wallet(ErrorCodes|AdminAuthorizationCodes)\.").Count);
        Assert.DoesNotContain("WalletErrorCodes.GiftCardNotFound", contributor, StringComparison.Ordinal);
        Assert.DoesNotContain("WalletErrorCodes.BalanceInsufficient", contributor, StringComparison.Ordinal);
        Assert.DoesNotContain("WalletErrorCodes.OutboxUnmappedEventType", contributor, StringComparison.Ordinal);

        // The composed catalog is constructible, unique, and the Wallet-owned keyspaces resolve to
        // exactly the HTTP-reachable descriptor set (one descriptor per client-observable code, and
        // no domain-only invariant is ever catalogued).
        var catalog = new ErrorDefinitionCatalog(
        [
            new FoundationErrorCatalogContributor(),
            new WalletErrorCatalogContributor(),
        ]);
        var walletRegistered = catalog.RegisteredCodes
            .Where(c => c.StartsWith("wallet.", StringComparison.Ordinal)
                        || c.StartsWith("giftcard.", StringComparison.Ordinal))
            .OrderBy(c => c, StringComparer.Ordinal).ToArray();
        Assert.Equal(
            WalletErrorCodes.HttpReachable.OrderBy(c => c, StringComparer.Ordinal).ToArray(),
            walletRegistered);
        Assert.All(
            WalletErrorCodes.DomainInvariants,
            code => Assert.False(catalog.TryGet(code, out _), $"invariant must not be catalogued: {code}"));
        Assert.DoesNotContain("customer.session.required", walletRegistered, StringComparer.Ordinal);
        Assert.DoesNotContain("admin.authorization.denied", walletRegistered, StringComparer.Ordinal);

        // Bilingual resources: 50 EN + 50 FA keys, one per declared code, all resolving through the
        // canonical composed localizer with real Persian text.
        var declared = DeclaredCodes();
        Assert.Equal(50, declared.Length);

        foreach (var culture in new[] { "WalletErrors.resx", "WalletErrors.fa.resx" })
        {
            var resx = File.ReadAllText(Path.Combine(root, ModuleRoot, ResxDirectory, culture));
            var keys = Regex.Matches(resx, @"data name=""([^""]+)""")
                .Select(m => m.Groups[1].Value).OrderBy(c => c, StringComparer.Ordinal).ToArray();
            Assert.Equal(declared, keys);
        }

        var localizer = new ResourceErrorMessageLocalizer(
            [new FoundationErrorResourceSet(), new WalletErrorResourceSet()],
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

        // Registered exactly once by the module endpoint presentation entry.
        var endpointModule = File.ReadAllText(Path.Combine(root, ModuleRoot, ModuleFile));
        Assert.Equal(1, Regex.Matches(endpointModule, @"AddSingleton<IErrorResourceSet").Count);
        Assert.Equal(1, Regex.Matches(endpointModule, @"AddSingleton<IErrorCatalogContributor").Count);
        Assert.Contains("WalletErrorResourceSet", endpointModule, StringComparison.Ordinal);

        // No ad-hoc logging / telemetry / correlation mechanism.
        Assert.DoesNotContain("Console.WriteLine", joined, StringComparison.Ordinal);
        Assert.DoesNotContain("Debug.WriteLine", joined, StringComparison.Ordinal);
        Assert.DoesNotContain("new ActivitySource(", joined, StringComparison.Ordinal);
        Assert.DoesNotContain("traceparent", joined, StringComparison.Ordinal);
    }

    [Fact]
    public void Wallet_transport_validation_codes_are_machine_stable_and_uncatalogued()
    {
        var root = Repo();
        var codes = File.ReadAllText(Path.Combine(root, ModuleRoot, ValidationCodesFile));
        var values = Regex.Matches(codes, @"""([^""]+)""").Select(m => m.Groups[1].Value).ToArray();
        Assert.NotEmpty(values);
        Assert.All(values, code => Assert.StartsWith("wallet.validation.", code, StringComparison.Ordinal));

        // Validation codes are transport identities: they reach the client only through the canonical
        // validation.failed envelope, never as their own catalog descriptor.
        var catalog = new ErrorDefinitionCatalog(
        [
            new FoundationErrorCatalogContributor(),
            new WalletErrorCatalogContributor(),
        ]);
        Assert.All(values, code => Assert.False(catalog.TryGet(code, out _)));
    }

    // ---------------------------------------------------------------------------------------------
    // 5. Contracts-only microservice boundary + unchanged schema
    // ---------------------------------------------------------------------------------------------

    [Fact]
    public void Wallet_is_contracts_only_and_microservice_extractable()
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
                if (reference.Contains("Tooba.Wallet.", StringComparison.Ordinal)
                    || reference.Contains("Tooba.BuildingBlocks", StringComparison.Ordinal)
                    || reference.Contains("Tooba.Persistence", StringComparison.Ordinal)
                    || reference.Contains("Tooba.ModuleContracts", StringComparison.Ordinal))
                {
                    continue;
                }

                // Every remaining edge must be a foreign module *.Contracts project.
                Assert.Contains("Tooba.", reference, StringComparison.Ordinal);
                Assert.Contains(".Contracts/", reference.Replace('\\', '/'), StringComparison.Ordinal);
                Assert.DoesNotContain(".Application/", reference.Replace('\\', '/'), StringComparison.Ordinal);
                Assert.DoesNotContain(".Infrastructure/", reference.Replace('\\', '/'), StringComparison.Ordinal);
                Assert.DoesNotContain(".Domain/", reference.Replace('\\', '/'), StringComparison.Ordinal);
                Assert.DoesNotContain(".Endpoints/", reference.Replace('\\', '/'), StringComparison.Ordinal);
            }
        }

        // The single legal foreign edge remains Notification.Contracts.
        var infrastructureRefs = ProjectRefs(root, "Tooba.Wallet.Infrastructure");
        Assert.Contains(infrastructureRefs, r => r.Contains("Notification.Contracts", StringComparison.Ordinal));

        // Endpoints must never reach Infrastructure or Host.
        var endpointsRefs = ProjectRefs(root, "Tooba.Wallet.Endpoints");
        Assert.Contains(endpointsRefs, r => r.Contains("Wallet.Application", StringComparison.Ordinal));
        Assert.DoesNotContain(endpointsRefs, r => r.Contains("Wallet.Infrastructure", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(endpointsRefs, r => r.Contains("Tooba.Host", StringComparison.OrdinalIgnoreCase));

        // Domain depends only on BuildingBlocks + Wallet.Contracts.
        var domainRefs = ProjectRefs(root, "Tooba.Wallet.Domain");
        Assert.Contains(domainRefs, r => r.Contains("Tooba.Wallet.Contracts", StringComparison.Ordinal));
        Assert.DoesNotContain(domainRefs, r => r.Contains("Wallet.Application", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(domainRefs, r => r.Contains("Wallet.Infrastructure", StringComparison.OrdinalIgnoreCase));

        // Zero foreign persistence reach-through and zero cross-module join.
        var joined = string.Join("\n", ProductionSources(root).Select(File.ReadAllText));
        foreach (var foreignContext in new[]
                 {
                     "NotificationDbContext", "OrderDbContext", "PaymentDbContext",
                     "InventoryDbContext", "PartyDbContext", "CatalogDbContext", "AccessControlDbContext",
                 })
        {
            Assert.DoesNotContain(foreignContext, joined, StringComparison.Ordinal);
        }

        foreach (var foreign in new[]
                 {
                     "Notification.Application", "Notification.Infrastructure", "Notification.Domain",
                     "Order.Application", "Order.Infrastructure", "Order.Domain",
                     "Payment.Application", "Payment.Infrastructure", "Payment.Domain",
                     "Catalog.Application", "Catalog.Infrastructure", "Catalog.Domain",
                     "AccessControl.Application", "AccessControl.Infrastructure", "AccessControl.Domain",
                 })
        {
            Assert.DoesNotContain($"using Tooba.{foreign}", joined, StringComparison.Ordinal);
        }

        Assert.DoesNotContain("TypeForwardedTo", joined, StringComparison.Ordinal);

        // Own schema and unchanged migration set.
        var context = File.ReadAllText(Path.Combine(
            root, ModuleRoot, "Tooba.Wallet.Infrastructure/Persistence/WalletDbContext.cs"));
        Assert.Contains("public const string Schema = \"wallet\"", context, StringComparison.Ordinal);
        Assert.Contains("HasDefaultSchema(Schema)", context, StringComparison.Ordinal);
        Assert.Contains("ToTable(\"wallet_accounts\")", context, StringComparison.Ordinal);
        Assert.Contains("ToTable(\"wallet_ledger_entries\")", context, StringComparison.Ordinal);
        Assert.Contains("ToTable(\"gift_cards\")", context, StringComparison.Ordinal);
        Assert.Contains("ToTable(\"gift_card_redemptions\")", context, StringComparison.Ordinal);

        var migrations = Directory.EnumerateFiles(
                Path.Combine(root, ModuleRoot, "Tooba.Wallet.Infrastructure/Persistence/Migrations"), "*.cs", SearchOption.TopDirectoryOnly)
            .Select(Path.GetFileName).OrderBy(n => n, StringComparer.Ordinal).ToArray();
        Assert.Equal(["20260827180000_InitialWallet.cs"], migrations);
    }

    // ---------------------------------------------------------------------------------------------
    // 6. Wave evidence + preserved Host closure
    // ---------------------------------------------------------------------------------------------

    [Fact]
    public void Wallet_wave_evidence_and_host_closure_are_preserved()
    {
        var root = Repo();

        foreach (var wave in new[] { "W0", "W1", "W2", "W3" })
        {
            Assert.True(Directory.Exists(Path.Combine(
                root, "docs/architecture/evidence", $"TB-TMAR-WALLET-AMSC-001-{wave}")),
                $"evidence directory for {wave} is required");
        }

        var w3Evidence = File.ReadAllText(Path.Combine(
            root, "docs/architecture/evidence/TB-TMAR-WALLET-AMSC-001-W3/certification.md"));
        Assert.Contains("ARCH-COMPLETE-002", w3Evidence, StringComparison.Ordinal);
        Assert.Contains("COMPLETE_REFERENCE_PATTERN", w3Evidence, StringComparison.Ordinal);
        Assert.Contains("STRUCTURE_CERTIFIED", w3Evidence, StringComparison.Ordinal);

        // Repository-global Host root checkpoint must remain untouched by this module-local task.
        using var sot = JsonDocument.Parse(File.ReadAllText(Path.Combine(
            root, "docs/architecture/tmar-current-state.json")).Replace("\uFEFF", string.Empty));
        Assert.Equal("TB-TMAR-HOST-ROOT-FINAL-CERT-001", sot.RootElement.GetProperty("lastAcceptedTask").GetString());
        Assert.Equal("NONE", sot.RootElement.GetProperty("automaticNextImplementationTask").GetString());

        // Master Recovery records the Wallet certification checkpoint.
        var recovery = File.ReadAllText(Path.Combine(root, "docs/architecture/TOOBA-TMAR-MASTER-RECOVERY.md"));
        Assert.Contains("Wallet AMSC module recovery checkpoint W3 (authoritative, module-local)", recovery, StringComparison.Ordinal);
        Assert.Contains("TB-TMAR-WALLET-AMSC-001-W3", recovery, StringComparison.Ordinal);
        Assert.Contains("USER_REVIEW_WALLET_AMSC_001_W3", recovery, StringComparison.Ordinal);
    }

    // ---------------------------------------------------------------------------------------------
    // Deterministic, fail-closed source parser
    // ---------------------------------------------------------------------------------------------

    private sealed record RouteEntry(string Audience, string Verb, string Path, string Handler);

    private sealed record Dispatch(string Audience, string Verb, string Path, string Handler, string Request, int SendCount);

    private static IReadOnlyList<Dispatch> DeriveDispatches(string root)
    {
        var audiences = new (string Audience, string File)[]
        {
            ("customer", CustomerFile),
            ("admin", AdminFile),
        };

        var dispatches = new List<Dispatch>();
        foreach (var (audience, file) in audiences)
        {
            var source = File.ReadAllText(Path.Combine(root, ModuleRoot, file));
            foreach (var route in ParseRoutes(audience, file, source))
            {
                var body = ExtractHandlerBody(source, route.Handler);
                var sendCount = Regex.Matches(body, @"sender\.Send\(").Count;
                var request = ParseDispatchedRequest(source, route.Handler, file);
                dispatches.Add(new Dispatch(
                    audience, route.Verb, route.Path, route.Handler, request, sendCount));
            }
        }

        return dispatches;
    }

    private static IEnumerable<RouteEntry> ParseRoutes(string audience, string file, string source)
    {
        var matches = Regex.Matches(
            source,
            @"app\.Map(?<verb>Get|Post|Put|Patch|Delete)\(""(?<path>[^""]+)""\s*,\s*(?<handler>[A-Za-z_][A-Za-z0-9_]*)\)");

        if (matches.Count == 0)
        {
            throw new InvalidOperationException($"no route mappings parsed from {file}");
        }

        foreach (Match match in matches)
        {
            yield return new RouteEntry(
                audience,
                match.Groups["verb"].Value,
                match.Groups["path"].Value,
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
        typeof(WalletErrorCodes)
            .GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
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
