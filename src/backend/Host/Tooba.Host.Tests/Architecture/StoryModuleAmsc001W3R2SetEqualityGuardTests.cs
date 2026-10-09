using System.Reflection;
using System.Text.RegularExpressions;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.Story.Application.Stories.Commands.Admin;
using Tooba.Story.Application.Stories.Commands.Seller;
using Tooba.Story.Application.Stories.Models;
using Tooba.Story.Application.Stories.Ports;
using Tooba.Story.Application.Stories.Queries.Admin;
using Tooba.Story.Application.Stories.Queries.Seller;
using Tooba.Story.Application.Stories.Queries.Storefront;
using Tooba.Story.Application.Stories.Validators;
using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>
/// TB-TMAR-STORY-AMSC-001-W3-R2 — durable bounded HTTP → route → handler → dispatched request
/// set-equality lock (<c>BOUNDED_DURABLE_ROUTE_REQUEST_SET_EQUALITY_PROOF</c>, Certify skill §6a hard gate).
/// <para>
/// The historical Story guards prove three independent counts (25 routes, 25 <c>ISender.Send</c> call
/// sites, 16 validators) and the older <c>StoryModuleAmcW5ValidatorGuardTests</c> compares the <b>set</b>
/// of newly constructed request names against a 25-row manifest. Counts and a name set are not set
/// equality: a dispatch can be replaced with another request that already belongs to the same 25-type set
/// while every route count, send count and set membership stays identical. This guard therefore derives the
/// <b>actual</b> dispatched request type of every shipped route from the endpoint source itself (supporting
/// both <c>Send(new T(...))</c> and <c>Send(variable)</c> by tracing the local construction), fails closed
/// whenever a route, handler or dispatch cannot be determined with certainty, and asserts exact equality of
/// the full audience + verb + path + handler → request map.
/// </para>
/// <para>
/// The parser is a pure function over source text, so the in-memory negative mutation proof below can
/// exercise it against a deliberately corrupted copy without touching or committing any production file.
/// This wave adds a test/evidence lock only: <b>zero</b> production change under
/// <c>src/backend/Modules/Story</c> or <c>src/backend/Host/Tooba.Host</c>.
/// </para>
/// </summary>
public sealed class StoryModuleAmsc001W3R2SetEqualityGuardTests
{
    private const string ModuleRoot = "src/backend/Modules/Story";

    private const string ModuleFile = "Tooba.Story.Endpoints/StoryEndpointModule.cs";
    private const string StorefrontFile = "Tooba.Story.Endpoints/Storefront/StoryStorefrontEndpoints.cs";
    private const string SellerFile = "Tooba.Story.Endpoints/Seller/StorySellerEndpoints.cs";
    private const string AdminFile = "Tooba.Story.Endpoints/Admin/StoryAdminEndpoints.cs";
    private const string ValidatorsFile = "Tooba.Story.Application/Stories/Validators/StoryValidators.cs";
    private const string StorefrontQueryFile =
        "Tooba.Story.Application/Stories/Queries/Storefront/GetPublicStoriesQuery.cs";

    private const int ExpectedRouteCount = 25;
    private const int ExpectedValidatorRequiredCount = 16;
    private const int ExpectedExemptCount = 9;

    private static readonly HashSet<string> SupportedVerbs =
        new(["Get", "Post", "Put", "Patch", "Delete"], StringComparer.Ordinal);

    private static readonly string[] ValidatorRequired =
    [
        "ListAdminStoriesQuery",
        "QueryAdminStoryGridQuery",
        "CreateAdminStoryCommand",
        "UpdateAdminStoryCommand",
        "ReorderAdminStoriesCommand",
        "ScheduleAdminStoryCommand",
        "RejectAdminStoryCommand",
        "AddAdminStoryItemCommand",
        "UpdateAdminStoryItemCommand",
        "ReorderAdminStoryItemsCommand",
        "CreateSellerStoryDraftCommand",
        "UpdateSellerStoryCommand",
        "AddSellerStoryItemCommand",
        "UpdateSellerStoryItemCommand",
        "ReorderSellerStoryItemsCommand",
        "GetPublicStoriesQuery",
    ];

    private static readonly string[] NoValidatorRequired =
    [
        "GetAdminStoryQuery",
        "EnableAdminStoryCommand",
        "DisableAdminStoryCommand",
        "ApproveAdminStoryCommand",
        "RemoveAdminStoryItemCommand",
        "ListSellerStoriesQuery",
        "GetSellerStoryQuery",
        "SubmitSellerStoryCommand",
        "RemoveSellerStoryItemCommand",
    ];

    /// <summary>The exact expected audience → verb → full route → handler → dispatched request map (25 rows).</summary>
    private static readonly (string Audience, string Verb, string Path, string Handler, string Request)[] ExpectedRoutes =
    [
        ("storefront", "Get", "/v1/storefront/stories", "GetPublicStoriesAsync", "GetPublicStoriesQuery"),
        ("seller", "Get", "/v1/seller/stories", "SellerListAsync", "ListSellerStoriesQuery"),
        ("seller", "Get", "/v1/seller/stories/{id:guid}", "SellerGetAsync", "GetSellerStoryQuery"),
        ("seller", "Post", "/v1/seller/stories", "SellerCreateAsync", "CreateSellerStoryDraftCommand"),
        ("seller", "Put", "/v1/seller/stories/{id:guid}", "SellerUpdateAsync", "UpdateSellerStoryCommand"),
        ("seller", "Post", "/v1/seller/stories/{id:guid}/submit", "SellerSubmitAsync", "SubmitSellerStoryCommand"),
        ("seller", "Post", "/v1/seller/stories/{id:guid}/items", "SellerAddItemAsync", "AddSellerStoryItemCommand"),
        ("seller", "Put", "/v1/seller/stories/{id:guid}/items/{itemId:guid}", "SellerUpdateItemAsync", "UpdateSellerStoryItemCommand"),
        ("seller", "Delete", "/v1/seller/stories/{id:guid}/items/{itemId:guid}", "SellerRemoveItemAsync", "RemoveSellerStoryItemCommand"),
        ("seller", "Put", "/v1/seller/stories/{id:guid}/items/reorder", "SellerReorderItemsAsync", "ReorderSellerStoryItemsCommand"),
        ("admin", "Get", "/v1/admin/stories", "AdminListAsync", "ListAdminStoriesQuery"),
        ("admin", "Post", "/v1/admin/stories/query", "AdminQueryGridAsync", "QueryAdminStoryGridQuery"),
        ("admin", "Get", "/v1/admin/stories/{id:guid}", "AdminGetAsync", "GetAdminStoryQuery"),
        ("admin", "Post", "/v1/admin/stories", "AdminCreateAsync", "CreateAdminStoryCommand"),
        ("admin", "Put", "/v1/admin/stories/{id:guid}", "AdminUpdateAsync", "UpdateAdminStoryCommand"),
        ("admin", "Put", "/v1/admin/stories/reorder", "AdminReorderAsync", "ReorderAdminStoriesCommand"),
        ("admin", "Post", "/v1/admin/stories/{id:guid}/enable", "AdminEnableAsync", "EnableAdminStoryCommand"),
        ("admin", "Post", "/v1/admin/stories/{id:guid}/disable", "AdminDisableAsync", "DisableAdminStoryCommand"),
        ("admin", "Post", "/v1/admin/stories/{id:guid}/schedule", "AdminScheduleAsync", "ScheduleAdminStoryCommand"),
        ("admin", "Post", "/v1/admin/stories/{id:guid}/approve", "AdminApproveAsync", "ApproveAdminStoryCommand"),
        ("admin", "Post", "/v1/admin/stories/{id:guid}/reject", "AdminRejectAsync", "RejectAdminStoryCommand"),
        ("admin", "Post", "/v1/admin/stories/{id:guid}/items", "AdminAddItemAsync", "AddAdminStoryItemCommand"),
        ("admin", "Put", "/v1/admin/stories/{id:guid}/items/{itemId:guid}", "AdminUpdateItemAsync", "UpdateAdminStoryItemCommand"),
        ("admin", "Delete", "/v1/admin/stories/{id:guid}/items/{itemId:guid}", "AdminRemoveItemAsync", "RemoveAdminStoryItemCommand"),
        ("admin", "Put", "/v1/admin/stories/{id:guid}/items/reorder", "AdminReorderItemsAsync", "ReorderAdminStoryItemsCommand"),
    ];

    private static readonly (string Request, Type Validator)[] RequiredValidators =
    [
        ("ListAdminStoriesQuery", typeof(ListAdminStoriesQueryValidator)),
        ("QueryAdminStoryGridQuery", typeof(QueryAdminStoryGridQueryValidator)),
        ("CreateAdminStoryCommand", typeof(CreateAdminStoryCommandValidator)),
        ("UpdateAdminStoryCommand", typeof(UpdateAdminStoryCommandValidator)),
        ("ReorderAdminStoriesCommand", typeof(ReorderAdminStoriesCommandValidator)),
        ("ScheduleAdminStoryCommand", typeof(ScheduleAdminStoryCommandValidator)),
        ("RejectAdminStoryCommand", typeof(RejectAdminStoryCommandValidator)),
        ("AddAdminStoryItemCommand", typeof(AddAdminStoryItemCommandValidator)),
        ("UpdateAdminStoryItemCommand", typeof(UpdateAdminStoryItemCommandValidator)),
        ("ReorderAdminStoryItemsCommand", typeof(ReorderAdminStoryItemsCommandValidator)),
        ("CreateSellerStoryDraftCommand", typeof(CreateSellerStoryDraftCommandValidator)),
        ("UpdateSellerStoryCommand", typeof(UpdateSellerStoryCommandValidator)),
        ("AddSellerStoryItemCommand", typeof(AddSellerStoryItemCommandValidator)),
        ("UpdateSellerStoryItemCommand", typeof(UpdateSellerStoryItemCommandValidator)),
        ("ReorderSellerStoryItemsCommand", typeof(ReorderSellerStoryItemsCommandValidator)),
        ("GetPublicStoriesQuery", typeof(GetPublicStoriesQueryValidator)),
    ];

    // ---------------------------------------------------------------------------------------------
    // 1. Exact route → dispatched request map, re-derived from disk
    // ---------------------------------------------------------------------------------------------

    [Fact]
    public void Shipped_routes_and_actual_dispatched_requests_match_the_exact_25_row_map()
    {
        var root = Repo();

        var derived = DeriveDispatches(root);

        Assert.Equal(ExpectedRouteCount, derived.Count);
        Assert.Equal(ExpectedRouteCount, ExpectedRoutes.Length);

        // The full map, not just the request set: this is the assertion a within-set swap must fail.
        var actual = derived
            .Select(d => (d.Audience, d.Verb, d.Path, d.Handler, d.Request))
            .OrderBy(x => x.Path, StringComparer.Ordinal)
            .ThenBy(x => x.Verb, StringComparer.Ordinal)
            .ThenBy(x => x.Audience, StringComparer.Ordinal)
            .ToArray();
        var expected = ExpectedRoutes
            .OrderBy(x => x.Path, StringComparer.Ordinal)
            .ThenBy(x => x.Verb, StringComparer.Ordinal)
            .ThenBy(x => x.Audience, StringComparer.Ordinal)
            .ToArray();

        for (var i = 0; i < expected.Length; i++)
        {
            Assert.Equal(expected[i].Audience, actual[i].Audience);
            Assert.Equal(expected[i].Verb, actual[i].Verb);
            Assert.Equal(expected[i].Path, actual[i].Path);
            Assert.Equal(expected[i].Handler, actual[i].Handler);
            Assert.Equal(expected[i].Request, actual[i].Request);
        }

        // 25 routes → 25 distinct actual request types → exactly one handler each.
        Assert.Equal(ExpectedRouteCount, derived.Select(d => d.Request).Distinct(StringComparer.Ordinal).Count());
        Assert.Equal(
            ExpectedRouteCount,
            derived.Select(d => (d.Verb, d.Path)).Distinct().Count());

        var applicationAssembly = typeof(IStoryDirectory).Assembly;
        foreach (var request in derived.Select(d => d.Request).Distinct(StringComparer.Ordinal))
        {
            var requestType = applicationAssembly.GetTypes()
                .Single(t => string.Equals(t.Name, request, StringComparison.Ordinal));

            var handlers = applicationAssembly.GetTypes()
                .Where(t => t.GetInterfaces().Any(i =>
                    i.IsGenericType
                    && i.GetGenericTypeDefinition() == typeof(IRequestHandler<,>)
                    && i.GetGenericArguments()[0] == requestType))
                .ToArray();
            Assert.Single(handlers);

            var declared = Directory.EnumerateFiles(
                    Path.Combine(root, ModuleRoot, "Tooba.Story.Application"), "*.cs", SearchOption.AllDirectories)
                .Count(p => File.ReadAllText(p).Contains($"record {request}", StringComparison.Ordinal));
            Assert.Equal(1, declared);
        }
    }

    [Fact]
    public void Dispatched_request_set_equals_the_16_required_plus_9_exempt_partition_with_no_orphan_duplicate_or_unmapped_route()
    {
        var root = Repo();
        var derived = DeriveDispatches(root);

        var dispatched = derived.Select(d => d.Request).ToArray();
        var classified = ValidatorRequired.Concat(NoValidatorRequired).ToArray();

        // Exact set equality derived from endpoint code — never adopted from the SoT or a static manifest.
        Assert.Equal(
            classified.OrderBy(x => x, StringComparer.Ordinal).ToArray(),
            dispatched.Distinct(StringComparer.Ordinal).OrderBy(x => x, StringComparer.Ordinal).ToArray());

        // No duplicate, orphan or unclassified dispatch, and no phantom classification.
        Assert.Equal(dispatched.Length, dispatched.Distinct(StringComparer.Ordinal).Count());
        Assert.Empty(dispatched.Except(classified, StringComparer.Ordinal));
        Assert.Empty(classified.Except(dispatched, StringComparer.Ordinal));

        // The partition is a real partition: disjoint halves, exact union.
        Assert.Empty(ValidatorRequired.Intersect(NoValidatorRequired, StringComparer.Ordinal));
        Assert.Equal(ExpectedValidatorRequiredCount, ValidatorRequired.Length);
        Assert.Equal(ExpectedExemptCount, NoValidatorRequired.Length);
        Assert.Equal(ExpectedRouteCount, ValidatorRequired.Length + NoValidatorRequired.Length);

        // The route surface is owned by the module composition entry, and each handler dispatches once.
        var module = File.ReadAllText(Path.Combine(root, ModuleRoot, ModuleFile));
        Assert.Contains("StoryStorefrontEndpoints.Map(app);", module, StringComparison.Ordinal);
        Assert.Contains("StorySellerEndpoints.Map(app);", module, StringComparison.Ordinal);
        Assert.Contains("StoryAdminEndpoints.Map(app);", module, StringComparison.Ordinal);
        Assert.Empty(Regex.Matches(module, @"\.Map(?:Get|Post|Put|Patch|Delete)\("));

        Assert.Equal(ExpectedRouteCount, derived.Count(d => d.SendCount == 1));
    }

    // ---------------------------------------------------------------------------------------------
    // 2. Concrete validator targets + real DI registration
    // ---------------------------------------------------------------------------------------------

    [Fact]
    public void Concrete_validator_targets_from_disk_equal_the_sixteen_required_requests()
    {
        var root = Repo();
        var validators = File.ReadAllText(Path.Combine(root, ModuleRoot, ValidatorsFile));

        var targets = Regex.Matches(validators, @"class\s+\w+Validator\s*:\s*AbstractValidator<(\w+)>")
            .Select(m => m.Groups[1].Value)
            .ToArray();

        Assert.Equal(ExpectedValidatorRequiredCount, targets.Length);
        Assert.Equal(
            ValidatorRequired.OrderBy(x => x, StringComparer.Ordinal).ToArray(),
            targets.Distinct(StringComparer.Ordinal).OrderBy(x => x, StringComparer.Ordinal).ToArray());

        // Exactly one concrete validator class per required request.
        Assert.Equal(targets.Length, targets.Distinct(StringComparer.Ordinal).Count());

        // No validator exists for an exempt request, and validators emit stable machine codes only.
        foreach (var exempt in NoValidatorRequired)
        {
            Assert.DoesNotContain($"AbstractValidator<{exempt}>", validators, StringComparison.Ordinal);
        }

        Assert.DoesNotContain("WithMessage(", validators, StringComparison.Ordinal);

        // The declared validator map matches the classified partition one-to-one.
        Assert.Equal(
            ValidatorRequired.OrderBy(x => x, StringComparer.Ordinal).ToArray(),
            RequiredValidators.Select(x => x.Request).OrderBy(x => x, StringComparer.Ordinal).ToArray());
    }

    [Fact]
    public void Validators_are_discoverable_through_the_real_cqrs_registration()
    {
        // Non-circular proof: the transport validators are resolved from a real container built by the
        // canonical foundation registration for the Story Application assembly — not by reflecting over
        // the validator source file.
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddToobaCqrsFoundation(typeof(IStoryDirectory).Assembly);
        using var provider = services.BuildServiceProvider();

        var applicationAssembly = typeof(IStoryDirectory).Assembly;
        Type RequestType(string name) => applicationAssembly.GetTypes()
            .Single(t => string.Equals(t.Name, name, StringComparison.Ordinal));

        foreach (var (request, validatorType) in RequiredValidators)
        {
            var registered = provider.GetService(typeof(IValidator<>).MakeGenericType(RequestType(request)));
            Assert.NotNull(registered);
            Assert.Equal(validatorType, registered!.GetType());
        }

        foreach (var exempt in NoValidatorRequired)
        {
            Assert.True(
                provider.GetService(typeof(IValidator<>).MakeGenericType(RequestType(exempt))) is null,
                $"{exempt} is NO_VALIDATOR_REQUIRED but a validator is registered");
        }

        // Every classified request is a real MediatR request.
        foreach (var name in ValidatorRequired.Concat(NoValidatorRequired))
        {
            Assert.True(RequestType(name).IsAssignableTo(typeof(IBaseRequest)), name);
        }

        // The canonical validation behavior is installed exactly once in the pipeline.
        var behaviors = provider
            .GetServices<IPipelineBehavior<ListAdminStoriesQuery, Result<IReadOnlyList<AdminStorySnapshot>>>>()
            .ToArray();
        Assert.Equal(
            1,
            behaviors.Count(b => b.GetType().IsGenericType
                && b.GetType().GetGenericTypeDefinition() == typeof(ValidationBehavior<,>)));
    }

    // ---------------------------------------------------------------------------------------------
    // 3. Non-circular provenance re-check for the nine exemptions
    // ---------------------------------------------------------------------------------------------

    [Fact]
    public void Exempt_requests_rest_on_guid_route_constraints_server_derived_actors_and_no_client_input()
    {
        var root = Repo();
        var storefront = File.ReadAllText(Path.Combine(root, ModuleRoot, StorefrontFile));
        var seller = File.ReadAllText(Path.Combine(root, ModuleRoot, SellerFile));
        var admin = File.ReadAllText(Path.Combine(root, ModuleRoot, AdminFile));

        // (a) Every identifier route segment in the module carries the :guid constraint, so a malformed
        //     value is rejected by routing before any handler runs.
        foreach (var file in new[] { storefront, seller, admin })
        {
            foreach (Match route in Regex.Matches(file, @"Map(?:Get|Post|Put|Patch|Delete)\(\s*""(?<path>[^""]+)"""))
            {
                var segments = Regex.Matches(route.Groups["path"].Value, @"\{([^}]+)\}")
                    .Select(m => m.Groups[1].Value)
                    .ToArray();
                Assert.All(segments, segment => Assert.EndsWith(":guid", segment, StringComparison.Ordinal));
            }
        }

        // (b) The tenant id is resolved from the platform tenant seam, fail-closed, never from client text.
        var httpErrors = File.ReadAllText(Path.Combine(root, ModuleRoot, "Tooba.Story.Endpoints/Errors/StoryHttpErrors.cs"));
        Assert.Contains("public static Result<Guid> ResolveTenantId(ICurrentTenant tenant)", httpErrors, StringComparison.Ordinal);
        Assert.Contains("Result.Success(StoryPresentationComposer.RequireTenantId(tenant))", httpErrors, StringComparison.Ordinal);
        foreach (var file in new[] { seller, admin })
        {
            Assert.Contains("var tenantResult = StoryHttpErrors.ResolveTenantId(tenant);", file, StringComparison.Ordinal);
            Assert.Contains("if (tenantResult.IsFailure)", file, StringComparison.Ordinal);
        }

        // (c) Seller/admin actor and sellerPartyId are derived from the authorization seam, never from a body.
        Assert.Contains("await auth.RequireAuthorizedAsync(http, cancellationToken)", seller, StringComparison.Ordinal);
        Assert.Contains("var (actorUserId, sellerPartyId) = await auth.RequireAuthorizedAsync(http, cancellationToken);", seller, StringComparison.Ordinal);
        Assert.Contains("var actorUserId = await auth.RequireAuthorizedAsync(http, cancellationToken);", admin, StringComparison.Ordinal);

        // (d) No client-readable identity shortcut exists in any endpoint file.
        foreach (var file in new[] { storefront, seller, admin })
        {
            Assert.DoesNotContain("X-Tooba-Dev-Actor-User-Id", file, StringComparison.Ordinal);
            Assert.DoesNotContain("HttpContext.Request.Headers", file, StringComparison.Ordinal);
            Assert.DoesNotContain("http.Request.Headers", file, StringComparison.Ordinal);
        }

        // (e) Every exempt request is constructed only from route-bound identifiers and server-derived
        //     values — never from a client-bound body. This is the non-circular input-provenance reason.
        var allowedArguments = new HashSet<string>(StringComparer.Ordinal)
        {
            "tenantResult.Value", "tenantId", "sellerPartyId", "actor", "actorUserId", "id", "itemId",
        };

        foreach (var exempt in NoValidatorRequired)
        {
            var arguments = ConstructedArguments(storefront, exempt)
                .Concat(ConstructedArguments(seller, exempt))
                .Concat(ConstructedArguments(admin, exempt))
                .ToArray();

            Assert.NotEmpty(arguments);
            foreach (var argument in arguments)
            {
                Assert.Contains(argument, allowedArguments);
                Assert.DoesNotContain("body", argument, StringComparison.Ordinal);
                Assert.DoesNotContain("Input", argument, StringComparison.Ordinal);
            }
        }
    }

    [Fact]
    public void Caller_controlled_locale_and_market_are_not_exempted_and_are_transport_validated()
    {
        var root = Repo();

        // GetPublicStoriesQuery carries two caller-controlled optional strings; optionality must never be
        // treated as evidence of safety, so the request is VALIDATOR_REQUIRED, not exempt.
        Assert.Contains("GetPublicStoriesQuery", ValidatorRequired);
        Assert.DoesNotContain("GetPublicStoriesQuery", NoValidatorRequired);

        var query = File.ReadAllText(Path.Combine(root, ModuleRoot, StorefrontQueryFile));
        Assert.Contains("public sealed record GetPublicStoriesQuery(Guid TenantId, string? Locale, string? Market)", query, StringComparison.Ordinal);

        var storefront = File.ReadAllText(Path.Combine(root, ModuleRoot, StorefrontFile));
        Assert.Contains("string? locale = null,", storefront, StringComparison.Ordinal);
        Assert.Contains("string? market = null,", storefront, StringComparison.Ordinal);
        Assert.Contains("new GetPublicStoriesQuery(tenantResult.Value, locale, market)", storefront, StringComparison.Ordinal);

        // And a concrete transport-shape validator with shape-only rules is registered for it.
        var validators = File.ReadAllText(Path.Combine(root, ModuleRoot, ValidatorsFile));
        var publicValidator = Regex.Match(
            validators,
            @"class\s+GetPublicStoriesQueryValidator\s*:\s*AbstractValidator<GetPublicStoriesQuery>\s*\{(?<body>.*?)\n\}",
            RegexOptions.Singleline);
        Assert.True(publicValidator.Success, "GetPublicStoriesQueryValidator not found");
        Assert.Contains("RuleFor(x => x.Locale)", publicValidator.Groups["body"].Value, StringComparison.Ordinal);
        Assert.Contains("RuleFor(x => x.Market)", publicValidator.Groups["body"].Value, StringComparison.Ordinal);
        Assert.Contains("StoryValidationCodes.LocaleInvalid", publicValidator.Groups["body"].Value, StringComparison.Ordinal);
        Assert.Contains("StoryValidationCodes.MarketInvalid", publicValidator.Groups["body"].Value, StringComparison.Ordinal);
        Assert.Contains("StoryRules.LocaleMaxLength", publicValidator.Groups["body"].Value, StringComparison.Ordinal);
        Assert.Contains("StoryRules.MarketMaxLength", publicValidator.Groups["body"].Value, StringComparison.Ordinal);
    }

    // ---------------------------------------------------------------------------------------------
    // 4. Route/dispatch syntax is fully accounted for and Host owns no Story route
    // ---------------------------------------------------------------------------------------------

    [Fact]
    public void Every_route_dispatches_through_exactly_one_sender_send_and_no_host_owned_story_route_exists()
    {
        var root = Repo();
        var derived = DeriveDispatches(root);

        Assert.Equal(ExpectedRouteCount, derived.Count);
        Assert.All(derived, d => Assert.Equal(1, d.SendCount));

        // Every route registration in the three endpoint files was recognised by the parser; unfamiliar
        // mapping syntax fails closed inside ParseRoutes rather than being silently ignored.
        foreach (var file in new[] { StorefrontFile, SellerFile, AdminFile })
        {
            var source = File.ReadAllText(Path.Combine(root, ModuleRoot, file));
            var mapCalls = Regex.Matches(source, @"\.Map(?:Get|Post|Put|Patch|Delete)\(").Count;
            Assert.Equal(mapCalls, ParseRoutes(source, file).Count());
        }

        // No endpoint reaches persistence or a directory directly.
        foreach (var file in new[] { StorefrontFile, SellerFile, AdminFile })
        {
            var source = File.ReadAllText(Path.Combine(root, ModuleRoot, file));
            Assert.Contains("ISender sender", source, StringComparison.Ordinal);
            Assert.DoesNotContain("DbContext", source, StringComparison.Ordinal);
            Assert.DoesNotContain("DbSet", source, StringComparison.Ordinal);
            Assert.DoesNotContain("IStoryDirectory", source, StringComparison.Ordinal);
        }

        // Host maps the module surface exactly once and owns no Story route of its own.
        var hostRoot = Path.Combine(root, "src", "backend", "Host", "Tooba.Host");
        var program = File.ReadAllText(Path.Combine(hostRoot, "Program.cs"));
        Assert.Contains("app.MapStoryModuleEndpoints();", program, StringComparison.Ordinal);
        Assert.Equal(1, Regex.Matches(program, @"MapStoryModuleEndpoints\(\)").Count);

        var hostSources = Directory.EnumerateFiles(hostRoot, "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                && !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .Select(File.ReadAllText)
            .ToArray();
        foreach (var source in hostSources)
        {
            Assert.Empty(Regex.Matches(source, @"\.Map(?:Get|Post|Put|Patch|Delete)\(\s*""[^""]*stories"));
        }

        Assert.False(Directory.Exists(Path.Combine(hostRoot, "Story")));
    }

    // ---------------------------------------------------------------------------------------------
    // 5. Mutation negative proof (in memory; no production file is modified or committed)
    // ---------------------------------------------------------------------------------------------

    [Fact]
    public void Replacing_one_dispatched_request_while_preserving_counts_and_type_set_fails_the_exact_map_gate()
    {
        var root = Repo();
        var admin = File.ReadAllText(Path.Combine(root, ModuleRoot, AdminFile));

        // Mutation A — route remap within the 25-type set: point the reorder route at an existing handler
        // whose dispatch is already a member of the 25-type set. The route count, the sender.Send count and
        // the *set* of constructed request names are all preserved, so the historical count-only and
        // set-only Story guards cannot see the corruption. The exact route → request map can.
        var remapped = admin.Replace(
            "admin.MapPut(\"/reorder\", AdminReorderAsync);",
            "admin.MapPut(\"/reorder\", AdminUpdateAsync);",
            StringComparison.Ordinal);
        Assert.NotEqual(admin, remapped);

        // Legacy route-count and send-count checks still pass.
        Assert.Equal(
            Regex.Matches(admin, @"\.Map(?:Get|Post|Put|Patch|Delete)\(").Count,
            Regex.Matches(remapped, @"\.Map(?:Get|Post|Put|Patch|Delete)\(").Count);
        Assert.Equal(
            Regex.Matches(admin, @"sender\.Send\(").Count,
            Regex.Matches(remapped, @"sender\.Send\(").Count);

        // Legacy set check still passes: no construction was added or removed, so the distinct set of
        // constructed request names is byte-for-byte identical.
        var legacyNamesBefore = LegacyConstructedRequestNames(admin).Distinct(StringComparer.Ordinal).OrderBy(x => x, StringComparer.Ordinal).ToArray();
        var legacyNamesAfter = LegacyConstructedRequestNames(remapped).Distinct(StringComparer.Ordinal).OrderBy(x => x, StringComparer.Ordinal).ToArray();
        Assert.Equal(legacyNamesBefore, legacyNamesAfter);
        Assert.Equal(15, legacyNamesBefore.Length);

        // The corrupted route now dispatches the wrong (still in-set) request, and its original handler is
        // orphaned — a route/handler shape the exact map rejects.
        Assert.Equal("UpdateAdminStoryCommand", ParseDispatchedRequest(remapped, "AdminUpdateAsync", AdminFile));
        var remappedRows = ParseRoutes(remapped, AdminFile)
            .Select(r => (Path: r.Path, Request: ParseDispatchedRequest(remapped, r.Handler, AdminFile)))
            .OrderBy(x => x.Path, StringComparer.Ordinal)
            .ToArray();
        var expectedAdminRows = ExpectedRoutes
            .Where(x => x.Audience == "admin")
            .Select(x => (Path: x.Path["/v1/admin/stories".Length..], Request: x.Request))
            .OrderBy(x => x.Path, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(expectedAdminRows.Length, remappedRows.Length);
        Assert.NotEqual(
            expectedAdminRows.Select(x => x.Request).ToArray(),
            remappedRows.Select(x => x.Request).ToArray());
        Assert.Equal(
            "UpdateAdminStoryCommand",
            remappedRows.Single(x => x.Path == "/reorder").Request);
        Assert.Contains("private static Task<IResult> AdminReorderAsync(", remapped, StringComparison.Ordinal);
        Assert.DoesNotContain("AdminReorderAsync);", remapped, StringComparison.Ordinal);

        // Mutation B — dispatch replacement. Route count and send count are still preserved, but the
        // constructed-name *set* now differs, so even the historical set guard catches this one while the
        // new exact map catches both.
        var seller = File.ReadAllText(Path.Combine(root, ModuleRoot, SellerFile));
        var swapped = seller.Replace(
            "new ListSellerStoriesQuery(tenantResult.Value, sellerPartyId)",
            "new GetSellerStoryQuery(tenantResult.Value, sellerPartyId, id)",
            StringComparison.Ordinal);
        Assert.NotEqual(seller, swapped);
        Assert.Equal(
            Regex.Matches(seller, @"sender\.Send\(").Count,
            Regex.Matches(swapped, @"sender\.Send\(").Count);
        Assert.NotEqual(
            LegacyConstructedRequestNames(seller).Distinct(StringComparer.Ordinal).OrderBy(x => x, StringComparer.Ordinal).ToArray(),
            LegacyConstructedRequestNames(swapped).Distinct(StringComparer.Ordinal).OrderBy(x => x, StringComparer.Ordinal).ToArray());
        Assert.Equal("GetSellerStoryQuery", ParseDispatchedRequest(swapped, "SellerListAsync", SellerFile));
        Assert.NotEqual(
            ExpectedRoutes.Where(x => x.Audience == "seller").Select(x => x.Request).OrderBy(x => x, StringComparer.Ordinal).ToArray(),
            ParseRoutes(swapped, SellerFile)
                .Select(r => ParseDispatchedRequest(swapped, r.Handler, SellerFile))
                .OrderBy(x => x, StringComparer.Ordinal)
                .ToArray());

        // The shipped source itself is untouched and still passes the exact map.
        var shipped = DeriveDispatches(root);
        Assert.Equal(ExpectedRouteCount, shipped.Count);
        Assert.Equal(ExpectedRouteCount, shipped.Select(d => d.Request).Distinct(StringComparer.Ordinal).Count());
        Assert.Contains(shipped, d => d.Request == "ListSellerStoriesQuery" && d.Path == "/v1/seller/stories");
        Assert.Contains(shipped, d => d.Request == "ReorderAdminStoriesCommand" && d.Path == "/v1/admin/stories/reorder");
    }

    [Fact]
    public void Untraceable_variable_dispatch_multi_dispatch_and_orphan_handler_fail_closed()
    {
        var root = Repo();
        var seller = File.ReadAllText(Path.Combine(root, ModuleRoot, SellerFile));

        // Every shipped Story dispatch is an inline construction read directly from the Send argument.
        Assert.DoesNotContain("var command = new", seller, StringComparison.Ordinal);
        Assert.DoesNotContain("var query = new", seller, StringComparison.Ordinal);

        // A variable dispatch (the other supported shape) is traced to its local construction.
        const string variableSource = """
            private static async Task<IResult> VariableAsync(ISender sender)
            {
                var command = new ReorderSellerStoryItemsCommand(Guid.Empty, Guid.Empty, Guid.Empty, Array.Empty<Guid>());
                return await sender.Send(command, CancellationToken.None);
            }
            """;
        Assert.Equal(
            "ReorderSellerStoryItemsCommand",
            ParseDispatchedRequest(variableSource, "VariableAsync", "synthetic"));

        // ...but an untraceable variable dispatch must throw rather than silently yield an unknown request.
        var untraceable = variableSource.Replace(
            "new ReorderSellerStoryItemsCommand(", "BuildReorderCommand(", StringComparison.Ordinal);
        Assert.NotEqual(variableSource, untraceable);
        Assert.Throws<InvalidOperationException>(
            () => ParseDispatchedRequest(untraceable, "VariableAsync", "synthetic"));

        // An unrecognised Send expression fails closed.
        Assert.Throws<InvalidOperationException>(
            () => ParseDispatchedRequest(
                "private static async Task<IResult> FactoryAsync(ISender sender) { return await sender.Send(Build(), CancellationToken.None); }",
                "FactoryAsync",
                "synthetic"));

        // A handler with no ISender.Send at all fails closed.
        Assert.Throws<InvalidOperationException>(
            () => ParseDispatchedRequest(
                "private static async Task<IResult> NothingAsync() { return null; }",
                "NothingAsync",
                "synthetic"));

        // A multi-dispatch handler fails closed instead of silently picking one dispatch.
        var multiDispatch = Assert.Throws<InvalidOperationException>(
            () => ParseDispatchedRequest(
                """
                private static async Task<IResult> TwiceAsync(ISender sender)
                {
                    await sender.Send(new GetSellerStoryQuery(Guid.Empty, Guid.Empty, Guid.Empty), CancellationToken.None);
                    return await sender.Send(new ListSellerStoriesQuery(Guid.Empty, Guid.Empty), CancellationToken.None);
                }
                """,
                "TwiceAsync",
                "synthetic"));
        Assert.Contains("2 ISender.Send", multiDispatch.Message, StringComparison.Ordinal);

        // Unfamiliar route-registration syntax fails closed rather than being ignored.
        Assert.Throws<InvalidOperationException>(
            () => ParseRoutes(
                """
                public static void Map(IEndpointRouteBuilder app)
                {
                    var group = app.MapGroup("/v1/seller/stories");
                    group.MapGet("", SellerListAsync);
                    group.MapMethods("/legacy", new[] { "GET" }, SellerLegacyAsync);
                }
                """,
                "synthetic").Count());
    }

    // ---------------------------------------------------------------------------------------------
    // 6. SoT truth, certification preservation and evidence
    // ---------------------------------------------------------------------------------------------

    [Fact]
    public void Sot_records_the_w3r2_set_equality_proof_without_touching_the_certified_truth()
    {
        var root = Repo();

        using var sot = System.Text.Json.JsonDocument.Parse(File.ReadAllText(Path.Combine(
            root, "docs/architecture/tmar-current-state.json")));

        var r2 = sot.RootElement.GetProperty("storyAmsc001W3R2");
        Assert.Equal("TB-TMAR-STORY-AMSC-001-W3-R2", r2.GetProperty("task").GetString());
        Assert.Equal("TB-TMAR-STORY-AMSC-001-W3-R1", r2.GetProperty("parentTask").GetString());
        Assert.Equal("BOUNDED_DURABLE_ROUTE_REQUEST_SET_EQUALITY_PROOF", r2.GetProperty("mode").GetString());
        Assert.Equal("tooba-architecture-certify", r2.GetProperty("skill").GetString());
        Assert.Equal("47284caad070b28ae79e803946dd19aed4b1678c", r2.GetProperty("startingHead").GetString());
        Assert.Equal("STORY_AMSC_001_W3_R2_SET_EQUALITY_PROVEN", r2.GetProperty("state").GetString());
        Assert.Equal("COMPLETE_REFERENCE_PATTERN", r2.GetProperty("verdict").GetString());
        Assert.Equal("ARCH-COMPLETE-002", r2.GetProperty("lockVersion").GetString());
        Assert.True(r2.GetProperty("structureCertified").GetBoolean());
        Assert.False(r2.GetProperty("productionCodeChanged").GetBoolean());
        Assert.Equal("TEST_EVIDENCE_ONLY_ZERO_PRODUCTION_CHANGE", r2.GetProperty("productionScopeState").GetString());
        Assert.Equal("EXACT_25_ROUTES_25_SENDS_25_REQUESTS_25_HANDLERS", r2.GetProperty("routeRequestExactMapState").GetString());
        Assert.Equal(
            "EXHAUSTIVE_16_VALIDATOR_REQUIRED_9_NO_VALIDATOR_REQUIRED_CONCRETE_ABSTRACTVALIDATOR_DI_VERIFIED",
            r2.GetProperty("requestValidatorMatrixState").GetString());
        Assert.Equal(ExpectedValidatorRequiredCount, r2.GetProperty("concreteValidatorClassCount").GetInt32());
        Assert.Equal(
            "NON_CIRCULAR_ALL_NINE_ROUTE_GUID_SERVER_DERIVED_ACTOR_NO_CLIENT_INPUT",
            r2.GetProperty("exemptionProvenanceState").GetString());
        Assert.Equal(
            "IN_MEMORY_WITHIN_SET_SWAP_PRESERVES_ROUTE_AND_SEND_COUNTS_AND_TYPE_SET_EXACT_MAP_FAILS",
            r2.GetProperty("mutationNegativeGuardState").GetString());
        Assert.Equal("NOT_TOUCHED", r2.GetProperty("manifestState").GetString());
        Assert.Equal("UNCHANGED", r2.GetProperty("schemaMigrationState").GetString());
        Assert.Equal("NONE", r2.GetProperty("guardsWeakened").GetString());
        Assert.Equal("NONE", r2.GetProperty("baselinesWidened").GetString());
        Assert.Equal("PRESERVED", r2.GetProperty("globalHostCheckpointState").GetString());
        Assert.Equal("USER_REVIEW_STORY_AMSC_001_W3_R2", r2.GetProperty("workflowStop").GetString());
        Assert.Equal("NONE", r2.GetProperty("automaticNextImplementationTask").GetString());
        Assert.Equal(
            "REPORTED_VIA_BRIDGE_RESULT_ONLY_NO_SELF_REFERENTIAL_PLACEHOLDER",
            r2.GetProperty("commitState").GetString());
        Assert.False(
            r2.TryGetProperty("commit", out _),
            "the W3-R2 block must not carry a self-referential commit placeholder");
        Assert.Equal(
            "docs/architecture/evidence/TB-TMAR-STORY-AMSC-001-W3-R2/route-request-set-equality.md",
            r2.GetProperty("evidence").GetString());

        // The recorded guard fact count is the real, independently reflected [Fact] count.
        var facts = typeof(StoryModuleAmsc001W3R2SetEqualityGuardTests)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Count(m => m.GetCustomAttributes(typeof(FactAttribute), false).Length > 0);
        Assert.Equal(
            $"StoryModuleAmsc001W3R2SetEqualityGuardTests ({facts} facts)",
            r2.GetProperty("guardsAdded").GetString());

        // The W3 certification truth is preserved unchanged by this test-only wave.
        var w3 = sot.RootElement.GetProperty("storyAmsc001W3");
        Assert.Equal("STORY_AMSC_001_CERTIFIED", w3.GetProperty("state").GetString());
        Assert.Equal("COMPLETE_REFERENCE_PATTERN", w3.GetProperty("verdict").GetString());
        Assert.Equal("ARCH-COMPLETE-002", w3.GetProperty("lockVersion").GetString());
        Assert.Equal("CERTIFIED", w3.GetProperty("structureState").GetString());
        Assert.Equal("HTTP_OWNING", w3.GetProperty("httpApplicability").GetString());
        Assert.Equal("MODULE_OWNED", w3.GetProperty("endpointOwnershipState").GetString());
        Assert.Equal(0, w3.GetProperty("hostOwnedRouteCount").GetInt32());
        Assert.Equal(ExpectedRouteCount, w3.GetProperty("endpointReachableRequests").GetInt32());
        Assert.Equal(ExpectedRouteCount, w3.GetProperty("totalMediatRRequests").GetInt32());
        Assert.Equal(0, w3.GetProperty("unmappedEndpointReachableRequests").GetInt32());
        Assert.Equal(
            "EXHAUSTIVE_16_VALIDATOR_REQUIRED_9_NO_VALIDATOR_REQUIRED",
            w3.GetProperty("validatorCoverageState").GetString());
        Assert.Equal("ZERO", w3.GetProperty("foreignModuleLayerCoupling").GetString());
        Assert.Equal("NONE", w3.GetProperty("crossModuleJoinState").GetString());
        Assert.True(w3.GetProperty("microserviceExtractable").GetBoolean());

        // The W3-R1 recovery record is not rewritten.
        var r1 = sot.RootElement.GetProperty("storyAmsc001W3R1");
        Assert.Equal("STORY_AMSC_001_RECOVERY_RECONCILED", r1.GetProperty("state").GetString());
        Assert.Equal("39ab324e9c57e342516202bdd8df95953dda6439", r1.GetProperty("certifiedCommit").GetString());

        // Historical AMC-001 lineage is preserved verbatim.
        foreach (var key in new[]
                 {
                     "storyModuleAmc001", "storyModuleAmc001W1", "storyModuleAmc001W2Cert",
                     "storyModuleAmc001W3", "storyModuleAmc001W4", "storyModuleAmc001W5",
                     "storyModuleAmc001W6Cert",
                 })
        {
            Assert.True(sot.RootElement.TryGetProperty(key, out _), $"historical AMC record {key} is missing");
        }

        // Promotion is intact and unique.
        var certified = sot.RootElement.GetProperty("structureLock").GetProperty("certifiedModules")
            .EnumerateArray().Select(x => x.GetString()).ToArray();
        Assert.Equal(1, certified.Count(x => x == "Story"));

        // Repository-global Host root checkpoint must remain untouched by this module-local task.
        Assert.Equal("TB-TMAR-HOST-ROOT-FINAL-CERT-001", sot.RootElement.GetProperty("lastAcceptedTask").GetString());
        Assert.Equal("HOST_ROOT_FINAL_CERTIFIED", sot.RootElement.GetProperty("currentHostCheckpoint").GetString());
        Assert.Equal("USER_REVIEW_HOST_ROOT_FINAL_CERT_001", sot.RootElement.GetProperty("workflowStop").GetString());
        Assert.Equal("NONE", sot.RootElement.GetProperty("automaticNextImplementationTask").GetString());

        // The W3-R2 evidence wave exists with the recorded proof.
        var evidence = Path.Combine(
            root, "docs/architecture/evidence/TB-TMAR-STORY-AMSC-001-W3-R2/route-request-set-equality.md");
        Assert.True(File.Exists(evidence));
        var text = File.ReadAllText(evidence);
        Assert.Contains("BOUNDED_DURABLE_ROUTE_REQUEST_SET_EQUALITY_PROOF", text, StringComparison.Ordinal);
        Assert.Contains("ARCH-COMPLETE-002", text, StringComparison.Ordinal);
        Assert.Contains("USER_REVIEW_STORY_AMSC_001_W3_R2", text, StringComparison.Ordinal);

        // Module-local Master Recovery checkpoint records the wave.
        var recovery = File.ReadAllText(Path.Combine(root, "docs", "architecture", "TOOBA-TMAR-MASTER-RECOVERY.md"));
        Assert.Contains("Story AMSC W3-R2 route-to-dispatch set-equality proof", recovery, StringComparison.Ordinal);
        Assert.Contains("USER_REVIEW_STORY_AMSC_001_W3_R2", recovery, StringComparison.Ordinal);
    }

    // ---------------------------------------------------------------------------------------------
    // Deterministic, fail-closed source parser (pure function over source text)
    // ---------------------------------------------------------------------------------------------

    private sealed record Dispatch(string Audience, string Verb, string Path, string Handler, string Request, int SendCount);

    private static IReadOnlyList<Dispatch> DeriveDispatches(string root)
    {
        var audiences = new (string Audience, string File)[]
        {
            ("storefront", StorefrontFile),
            ("seller", SellerFile),
            ("admin", AdminFile),
        };

        var dispatches = new List<Dispatch>();
        foreach (var (audience, file) in audiences)
        {
            var source = File.ReadAllText(Path.Combine(root, ModuleRoot, file));
            var prefix = DerivePrefix(source);
            foreach (var route in ParseRoutes(source, file))
            {
                var body = ExtractMemberBody(source, route.Handler);
                var sendCount = Regex.Matches(body, @"sender\.Send\(").Count;
                var request = ParseDispatchedRequest(source, route.Handler, file);
                dispatches.Add(new Dispatch(
                    audience, route.Verb, prefix + route.Path, route.Handler, request, sendCount));
            }
        }

        return dispatches;
    }

    /// <summary>Derives the active audience prefix from the file itself (empty for a direct app.Map surface).</summary>
    private static string DerivePrefix(string source)
    {
        var declarations = Regex.Matches(source, @"(\w+)\s*=\s*app\.MapGroup\(\s*""([^""]+)""\s*\)");
        if (declarations.Count == 0)
        {
            return string.Empty;
        }

        if (declarations.Count != 1)
        {
            throw new InvalidOperationException(
                $"expected exactly one MapGroup declaration but found {declarations.Count}");
        }

        return declarations[0].Groups[2].Value;
    }

    private static IEnumerable<(string Verb, string Path, string Handler)> ParseRoutes(string source, string file)
    {
        // Fail closed on any route-registration-looking call the parser does not understand, so unfamiliar
        // mapping syntax (for example MapMethods) can never be silently ignored.
        foreach (Match call in Regex.Matches(source, @"\.Map(?<name>[A-Za-z]+)\("))
        {
            var name = call.Groups["name"].Value;
            if (string.Equals(name, "Group", StringComparison.Ordinal)
                || SupportedVerbs.Contains(name))
            {
                continue;
            }

            throw new InvalidOperationException(
                $"{file} uses unsupported route registration '.Map{name}('; failing closed");
        }

        var mapCalls = Regex.Matches(source, @"\.Map(?:Get|Post|Put|Patch|Delete)\(").Count;
        var matches = Regex.Matches(
            source,
            @"[A-Za-z_][A-Za-z0-9_]*\.Map(?<verb>Get|Post|Put|Patch|Delete)\(\s*""(?<path>[^""]*)""\s*,\s*(?<handler>[A-Za-z_][A-Za-z0-9_]*)\s*\)");

        if (matches.Count == 0)
        {
            throw new InvalidOperationException($"no route mappings parsed from {file}");
        }

        if (matches.Count != mapCalls)
        {
            throw new InvalidOperationException(
                $"{file} contains {mapCalls - matches.Count} route registration(s) with unfamiliar syntax; failing closed");
        }

        foreach (Match match in matches)
        {
            yield return (match.Groups["verb"].Value, match.Groups["path"].Value, match.Groups["handler"].Value);
        }
    }

    /// <summary>
    /// Derives the request type actually passed to <c>ISender.Send</c> inside <paramref name="handler"/>.
    /// Supports both <c>Send(new T(...))</c> and <c>Send(variable)</c> by tracing the local construction.
    /// Fails closed (throws) whenever the dispatch cannot be determined with certainty, is absent, or is
    /// dispatched more than once.
    /// </summary>
    private static string ParseDispatchedRequest(string source, string handler, string file)
    {
        var body = ExtractMemberBody(source, handler);

        var sends = Regex.Matches(body, @"sender\.Send\(");
        if (sends.Count == 0)
        {
            throw new InvalidOperationException(
                $"{file}:{handler} has no ISender.Send call; dispatch provenance cannot be derived");
        }

        if (sends.Count > 1)
        {
            throw new InvalidOperationException(
                $"{file}:{handler} has {sends.Count} ISender.Send call sites; a route must dispatch exactly once");
        }

        var argument = body[(sends[0].Index + sends[0].Length)..].TrimStart();

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
                $"{file}:{handler} dispatches an unrecognised expression: '{Truncate(argument)}'");
        }

        var construction = Regex.Match(
            body,
            $@"(?:var|[A-Za-z_][A-Za-z0-9_<>.,\s]*?)\s{Regex.Escape(identifier)}\s*=\s*new\s+(?<type>[A-Za-z_][A-Za-z0-9_.]*)",
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

    /// <summary>Extracts a member body (brace-balanced block or expression-bodied arrow) by declared name.</summary>
    private static string ExtractMemberBody(string source, string member)
    {
        foreach (Match candidate in Regex.Matches(source, $@"\b{Regex.Escape(member)}\s*\("))
        {
            var index = candidate.Index + candidate.Length;
            var depth = 1;
            while (index < source.Length && depth > 0)
            {
                if (source[index] == '(')
                {
                    depth++;
                }
                else if (source[index] == ')')
                {
                    depth--;
                }

                index++;
            }

            if (depth != 0)
            {
                continue;
            }

            var cursor = index;
            while (cursor < source.Length && char.IsWhiteSpace(source[cursor]))
            {
                cursor++;
            }

            if (cursor < source.Length && source[cursor] == '{')
            {
                var braces = 0;
                for (var i = cursor; i < source.Length; i++)
                {
                    if (source[i] == '{')
                    {
                        braces++;
                    }
                    else if (source[i] == '}')
                    {
                        braces--;
                        if (braces == 0)
                        {
                            return source[cursor..(i + 1)];
                        }
                    }
                }

                continue;
            }

            if (cursor + 1 < source.Length && source[cursor] == '=' && source[cursor + 1] == '>')
            {
                var nesting = 0;
                for (var i = cursor + 2; i < source.Length; i++)
                {
                    var current = source[i];
                    if (current is '(' or '{' or '[')
                    {
                        nesting++;
                    }
                    else if (current is ')' or '}' or ']')
                    {
                        nesting--;
                    }
                    else if (current == ';' && nesting == 0)
                    {
                        return source[cursor..(i + 1)];
                    }
                }
            }
        }

        throw new InvalidOperationException($"member '{member}' body could not be extracted; failing closed");
    }

    /// <summary>The legacy name-set extraction used by the historical Story validator guard.</summary>
    private static string[] LegacyConstructedRequestNames(string source) =>
        Regex.Matches(source, @"\bnew\s+([A-Za-z0-9_]+(?:Command|Query))\b")
            .Select(m => m.Groups[1].Value)
            .ToArray();

    private static string[] ConstructedArguments(string source, string request)
    {
        var match = Regex.Match(source, $@"new\s+{Regex.Escape(request)}\s*\((?<args>[^)]*)\)");
        if (!match.Success)
        {
            return [];
        }

        return match.Groups["args"].Value
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToArray();
    }

    private static string Truncate(string value) =>
        value.Length <= 60 ? value : value[..60];

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
