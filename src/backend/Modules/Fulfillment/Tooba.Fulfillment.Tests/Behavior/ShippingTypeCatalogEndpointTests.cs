using System.Text;
using System.Text.Json;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.DependencyInjection;
using Tooba.BuildingBlocks.Presentation;
using Tooba.BuildingBlocks.Presentation.Errors;
using Tooba.BuildingBlocks.Security;
using Tooba.Fulfillment.Application.Shipping;
using Tooba.Fulfillment.Application.Shipping.Commands;
using Tooba.Fulfillment.Application.Shipping.Models;
using Tooba.Fulfillment.Application.Shipping.Queries;
using Tooba.Fulfillment.Application.Shipping.Validators;
using Tooba.Fulfillment.Contracts.Errors;
using Tooba.Fulfillment.Contracts.Shipping;
using Tooba.Fulfillment.Endpoints;
using Tooba.Fulfillment.Endpoints.Admin;
using Tooba.Fulfillment.Infrastructure.Errors;
using Tooba.Fulfillment.Infrastructure.Persistence;
using Tooba.Fulfillment.Infrastructure.Shipping;
using Tooba.Localization.Contracts.Ports;
using Xunit;

namespace Tooba.Fulfillment.Tests.Behavior;

/// <summary>
/// TB-SHIPPING-ADMIN-SHIPPING-TYPE-CATALOG-196-R3 — proof through the REAL route delegates and DI graph.
/// </summary>
/// <remarks>
/// <para>
/// The four shipping-type routes are produced by the production composition
/// (<see cref="FulfillmentEndpointModule.MapFulfillmentEndpoints"/>) on a real
/// <see cref="WebApplication"/> route builder that is built and never started. Every request executes the
/// actual endpoint <see cref="RequestDelegate"/>, the real registered
/// <see cref="IShippingTypeAdminAuthorizer"/>, the real <c>ShippingTypeBodyReader</c>, the real MediatR
/// pipeline (<c>ValidationBehavior</c>), the real handlers, the real <see cref="ShippingTypeStore"/> over an
/// <b>EF InMemory</b> <see cref="FulfillmentDbContext"/>, the real
/// <c>LocalizationShippingTypeLanguageFacts</c> adapter over a controlled <see cref="ILanguageLookup"/>, the
/// real canonical <see cref="ModuleCallTracer"/> registration and the real
/// <see cref="ApiResponseFactory"/> plus Fulfillment error catalog/resources.
/// </para>
/// <para>
/// Only platform seams are controlled fixtures: admin panel access, the authorization engine decision, the
/// resolved edition/tenant, the clock, the id generator and the language registry rows. No module authorizer,
/// language-facts resolution, handler, sender, or result factory is faked. EF InMemory is explicitly NOT
/// PostgreSQL/schema/race proof.
/// </para>
/// </remarks>
public sealed class ShippingTypeCatalogEndpointTests
{
    private const string ListPath = "/v1/admin/shipping-types";
    private const string TypePath = "/v1/admin/shipping-types/{shippingTypeId:guid}";
    private static readonly Guid Actor = Guid.Parse("01900000-0000-7000-8000-00000000ff01");
    private static readonly Guid DefaultLanguage = Guid.Parse("01900000-0000-7000-8000-00000000bb01");
    private static readonly Guid SecondLanguage = Guid.Parse("01900000-0000-7000-8000-00000000bb02");
    private static readonly DateTimeOffset Now = new(2026, 10, 22, 10, 0, 0, TimeSpan.Zero);

    // ---------------------------------------------------------------------------------------------
    // Authorized lifecycle through the real delegates
    // ---------------------------------------------------------------------------------------------

    [Fact]
    public async Task Authorized_lifecycle_create_read_activate_deactivate_noop_and_conflict()
    {
        using var harness = Harness.Create();

        // POST -> inactive, revision 1, canonical 201 + Location with the real generated id.
        var create = await harness.SendAsync(
            "POST",
            ListPath,
            """{"code":"ordinary","sortOrder":0,"translations":[{"languageId":"01900000-0000-7000-8000-00000000bb01","name":"عادی","description":"توضیح"}]}""");

        Assert.Equal(StatusCodes.Status201Created, create.Status);
        var id = create.Body.GetProperty("shippingTypeId").GetGuid();
        Assert.NotEqual(Guid.Empty, id);
        Assert.Equal(create.Body.GetProperty("shippingTypeId").GetGuid().ToString(),
            harness.LastLocation!.TrimEnd('/').Split('/').Last());
        Assert.False(create.Body.GetProperty("isActive").GetBoolean());
        Assert.Equal(1, create.Body.GetProperty("revision").GetInt64());
        Assert.Equal(0, create.Body.GetProperty("sortOrder").GetInt32());
        Assert.Equal("ordinary", create.Body.GetProperty("code").GetString());
        Assert.True(create.Body.GetProperty("hasDefaultLanguageName").GetBoolean());

        // GET list/detail return it through the real query handlers.
        var list = await harness.SendAsync("GET", ListPath);
        Assert.Equal(StatusCodes.Status200OK, list.Status);
        Assert.Equal(2, list.Body.GetArrayLength());
        Assert.Contains(list.Body.EnumerateArray(), item => item.GetProperty("shippingTypeId").GetGuid() == id);

        var detail = await harness.SendAsync("GET", $"{ListPath}/{id}");
        Assert.Equal(StatusCodes.Status200OK, detail.Status);
        Assert.Equal(id, detail.Body.GetProperty("shippingTypeId").GetGuid());

        // PUT activate.
        var activate = await harness.SendAsync("PUT", $"{ListPath}/{id}", UpdateBody(1, isActive: true, sortOrder: 0));
        Assert.Equal(StatusCodes.Status200OK, activate.Status);
        Assert.True(activate.Body.GetProperty("isActive").GetBoolean());
        Assert.Equal(2, activate.Body.GetProperty("revision").GetInt64());

        // Repeating the identical nonempty text must be a true no-op: revision and UpdatedAt preserved.
        var storedUpdatedAt = await harness.StoredUpdatedAtAsync(id);
        var repeat = await harness.SendAsync("PUT", $"{ListPath}/{id}", UpdateBody(2, isActive: true, sortOrder: 0));
        Assert.Equal(StatusCodes.Status200OK, repeat.Status);
        Assert.Equal(2, repeat.Body.GetProperty("revision").GetInt64());
        Assert.Equal(storedUpdatedAt, await harness.StoredUpdatedAtAsync(id));

        // Stale identical body is still a canonical conflict, and nothing was written.
        var stale = await harness.SendAsync("PUT", $"{ListPath}/{id}", UpdateBody(1, isActive: true, sortOrder: 0));
        Assert.Equal(StatusCodes.Status409Conflict, stale.Status);
        Assert.Equal(FulfillmentErrorCodes.ShippingTypeRevisionConflict, ErrorCode(stale.Body));
        Assert.Equal(2, await harness.StoredRevisionAsync(id));

        // Deactivate then reactivate through the real handler/store.
        var deactivate = await harness.SendAsync("PUT", $"{ListPath}/{id}", UpdateBody(2, isActive: false, sortOrder: 0));
        Assert.Equal(StatusCodes.Status200OK, deactivate.Status);
        Assert.False(deactivate.Body.GetProperty("isActive").GetBoolean());

        var reactivate = await harness.SendAsync("PUT", $"{ListPath}/{id}", UpdateBody(3, isActive: true, sortOrder: 0));
        Assert.True(reactivate.Body.GetProperty("isActive").GetBoolean());
        Assert.Equal(4, reactivate.Body.GetProperty("revision").GetInt64());

        // Code stays immutable: a differing code rejects without data loss.
        var rename = await harness.SendAsync(
            "PUT", $"{ListPath}/{id}",
            """{"code":"renamed","sortOrder":0,"isActive":true,"expectedRevision":4,"translations":[]}""");
        Assert.Equal(StatusCodes.Status400BadRequest, rename.Status);
        Assert.Equal(FulfillmentErrorCodes.ShippingTypeCodeInvalid, ErrorCode(rename.Body));

        var afterRename = await harness.SendAsync("GET", $"{ListPath}/{id}");
        Assert.Equal("ordinary", afterRename.Body.GetProperty("code").GetString());
        Assert.Equal(4, afterRename.Body.GetProperty("revision").GetInt64());
    }

    [Fact]
    public async Task Identical_code_in_update_body_is_accepted_as_immutable_identity()
    {
        using var harness = Harness.Create();
        var id = await harness.CreateAsync();

        var response = await harness.SendAsync(
            "PUT", $"{ListPath}/{id}",
            """{"code":"ordinary","sortOrder":7,"isActive":true,"expectedRevision":1,"translations":[]}""");

        Assert.Equal(StatusCodes.Status200OK, response.Status);
        Assert.Equal("ordinary", response.Body.GetProperty("code").GetString());
        Assert.Equal(7, response.Body.GetProperty("sortOrder").GetInt32());
    }

    // ---------------------------------------------------------------------------------------------
    // Independent permissions and trusted installation context
    // ---------------------------------------------------------------------------------------------

    [Fact]
    public async Task View_grant_allows_reads_and_denies_writes_before_dispatch_or_language_read()
    {
        using var harness = Harness.Create(permissions: [ShippingTypeAdminAuthorizer.View]);

        Assert.Equal(StatusCodes.Status200OK, (await harness.SendAsync("GET", ListPath)).Status);
        Assert.Equal(1, harness.Counters.AuthorizationChecks);

        var dispatchedBefore = harness.Counters.Dispatches;
        var languagesBefore = harness.Counters.LanguageLookups;

        var post = await harness.SendAsync("POST", ListPath, CreateBody());
        var put = await harness.SendAsync("PUT", $"{ListPath}/{harness.SeededId}", UpdateBody(1, true, 0));

        Assert.Equal(StatusCodes.Status403Forbidden, post.Status);
        Assert.Equal(StatusCodes.Status403Forbidden, put.Status);
        Assert.Equal(FoundationErrorCodes.AdminAuthorizationDenied, ErrorCode(post.Body));
        Assert.Equal(FoundationErrorCodes.AdminAuthorizationDenied, ErrorCode(put.Body));
        Assert.Equal(dispatchedBefore, harness.Counters.Dispatches);
        Assert.Equal(languagesBefore, harness.Counters.LanguageLookups);
        Assert.Single(await harness.StoredRowsAsync());
    }

    [Fact]
    public async Task Admin_panel_rejection_prevents_authorization_and_business_dispatch()
    {
        // A seller-only actor: the platform admin panel seam refuses the request.
        using var harness = Harness.Create(panel: PanelDecision.RejectSellerOnly);

        foreach (var (method, path) in new[]
                 {
                     ("GET", ListPath),
                     ("GET", $"{ListPath}/{harness.SeededId}"),
                     ("POST", ListPath),
                     ("PUT", $"{ListPath}/{harness.SeededId}"),
                 })
        {
            var response = await harness.SendAsync(method, path, method is "POST" or "PUT" ? CreateBody() : null);

            Assert.Equal(StatusCodes.Status403Forbidden, response.Status);
            Assert.Equal(FoundationErrorCodes.AdminAuthorizationDenied, ErrorCode(response.Body));
        }

        // The authorization engine was never consulted for these four requests.
        Assert.Equal(0, harness.Counters.AuthorizationChecks);
        Assert.Equal(0, harness.Counters.Dispatches);
        Assert.Equal(0, harness.Counters.LanguageLookups);
        Assert.Equal(0, harness.Counters.ReadCalls);
        Assert.Single(await harness.StoredRowsAsync());
    }

    [Theory]
    [InlineData("missing-edition")]
    [InlineData("unknown-edition")]
    [InlineData("single-store-no-tenant")]
    [InlineData("single-store-inactive-tenant")]
    [InlineData("marketplace-leftover-tenant")]
    public async Task Invalid_installation_context_is_authorization_unavailable_with_zero_dispatch(string scenario)
    {
        using var harness = Harness.Create(scenario: scenario);

        var response = await harness.SendAsync("GET", ListPath);

        Assert.Equal(StatusCodes.Status503ServiceUnavailable, response.Status);
        Assert.Equal(FoundationErrorCodes.AdminAuthorizationUnavailable, ErrorCode(response.Body));

        // Context rejects BEFORE the capability check: zero outgoing authorization checks, zero dispatch.
        Assert.Equal(0, harness.Counters.AuthorizationChecks);
        Assert.Equal(0, harness.Counters.Dispatches);
        Assert.Equal(0, harness.Counters.LanguageLookups);
    }

    [Theory]
    [InlineData("single-store-active-tenant", ToobaEdition.SingleStore, "tenant-01")]
    [InlineData("marketplace-no-tenant", ToobaEdition.Marketplace, null)]
    public async Task Valid_context_forwards_the_trusted_call_context(
        string scenario,
        ToobaEdition expectedEdition,
        string? expectedTenant)
    {
        using var harness = Harness.Create(scenario: scenario);

        var response = await harness.SendAsync("GET", ListPath);

        Assert.Equal(StatusCodes.Status200OK, response.Status);
        Assert.Equal(1, harness.Counters.AuthorizationChecks);
        Assert.Equal(expectedEdition, harness.Authz.LastCallContext!.Edition);
        Assert.Equal(expectedTenant, harness.Authz.LastCallContext.TenantId);
        Assert.Equal(ShippingTypeAdminAuthorizer.View, harness.Authz.LastResourceId);
    }

    [Fact]
    public async Task Client_supplied_owner_edition_or_tenant_text_does_not_replace_trusted_context()
    {
        using var harness = Harness.Create(scenario: "single-store-active-tenant");

        var response = await harness.SendAsync(
            "GET",
            ListPath + "?tenantId=attacker&edition=Marketplace&ownerId=999",
            headers: new Dictionary<string, string>
            {
                ["X-Tenant-Id"] = "attacker",
                ["X-Edition"] = "Marketplace",
                ["X-Owner-Id"] = "999",
            });

        Assert.Equal(StatusCodes.Status200OK, response.Status);
        Assert.Equal(ToobaEdition.SingleStore, harness.Authz.LastCallContext!.Edition);
        Assert.Equal("tenant-01", harness.Authz.LastCallContext.TenantId);
    }

    // ---------------------------------------------------------------------------------------------
    // Authorize BEFORE body
    // ---------------------------------------------------------------------------------------------

    [Fact]
    public async Task Unauthorized_malformed_body_is_denied_with_zero_reads_and_zero_dispatch()
    {
        using var harness = Harness.Create(panel: PanelDecision.RejectSellerOnly, readCounting: true);

        var post = await harness.SendAsync("POST", ListPath, "{not-json");
        var put = await harness.SendAsync("PUT", $"{ListPath}/{harness.SeededId}", "{not-json");

        Assert.Equal(StatusCodes.Status403Forbidden, post.Status);
        Assert.Equal(StatusCodes.Status403Forbidden, put.Status);
        Assert.Equal(0, harness.Counters.ReadCalls);
        Assert.Equal(0, harness.Counters.Dispatches);
        Assert.Single(await harness.StoredRowsAsync());
        Assert.Equal(0, harness.Counters.LanguageLookups);
        Assert.Empty(harness.Counters.SenderRequests);
    }

    [Theory]
    [InlineData("")]
    [InlineData("{")]
    [InlineData("[]")]
    [InlineData("null")]
    [InlineData("""{"sortOrder":"ten"}""")]
    [InlineData("""{"translations":{}}""")]
    [InlineData("""{"translations":[{"languageId":"nope"}]}""")]
    [InlineData("""{"isActive":1}""")]
    public async Task Authorized_malformed_body_follows_shipping_type_body_malformed(string body)
    {
        using var harness = Harness.Create();

        var response = await harness.SendAsync("POST", ListPath, body);

        Assert.Equal(StatusCodes.Status400BadRequest, response.Status);
        Assert.Equal(FulfillmentErrorCodes.ShippingTypeBodyMalformed, ErrorCode(response.Body));
        // Body-shape rejection happens before ISender and before any language read.
        Assert.Equal(0, harness.Counters.Dispatches);
        Assert.Equal(0, harness.Counters.LanguageLookups);
        Assert.Single(await harness.StoredRowsAsync());
    }

    // ---------------------------------------------------------------------------------------------
    // Lossless validation matrix (dispatch allowed, handler NOT reached)
    // ---------------------------------------------------------------------------------------------

    [Theory]
    [InlineData("""{"code":"ordinary","translations":[{"languageId":"01900000-0000-7000-8000-00000000bb01","name":"x"}]}""", "sortOrder")]
    [InlineData("""{"code":"ordinary","sortOrder":null,"translations":[{"languageId":"01900000-0000-7000-8000-00000000bb01","name":"x"}]}""", "sortOrder")]
    [InlineData("""{"code":"ordinary","sortOrder":-1,"translations":[{"languageId":"01900000-0000-7000-8000-00000000bb01","name":"x"}]}""", "sortOrder")]
    [InlineData("""{"code":" ordinary","sortOrder":0,"translations":[{"languageId":"01900000-0000-7000-8000-00000000bb01","name":"x"}]}""", "code")]
    [InlineData("""{"code":"ordinary","sortOrder":0}""", "translations")]
    [InlineData("""{"code":"ordinary","sortOrder":0,"translations":null}""", "translations")]
    [InlineData("""{"code":"ordinary","sortOrder":0,"translations":[{"languageId":"01900000-0000-7000-8000-00000000bb01","name":"x"},null]}""", "translations")]
    [InlineData("""{"code":"ordinary","sortOrder":0,"translations":[{"languageId":"01900000-0000-7000-8000-00000000bb01","name":"x"},{"languageId":"01900000-0000-7000-8000-00000000bb01","name":"y"}]}""", "translations")]
    [InlineData("""{"code":"ordinary","sortOrder":0,"translations":[{"languageId":"00000000-0000-0000-0000-000000000000","name":"x"}]}""", "translations")]
    [InlineData("""{"code":"ordinary","sortOrder":0,"translations":[{"languageId":"01900000-0000-7000-8000-00000000bb01","name":"  "}]}""", "translations")]
    public async Task Post_rejections_are_canonical_and_never_reach_the_handler(string body, string _)
    {
        using var harness = Harness.Create();

        var response = await harness.SendAsync("POST", ListPath, body);

        Assert.Equal(StatusCodes.Status400BadRequest, response.Status);
        Assert.Equal(1, harness.Counters.Dispatches);          // dispatched into the pipeline
        Assert.Equal(0, harness.Counters.LanguageLookups);      // handler never ran
        Assert.Single(await harness.StoredRowsAsync());        // nothing new persisted
        Assert.Single(await harness.StoredRowsAsync());         // only the seeded row remains
    }

    [Theory]
    [InlineData("""{"sortOrder":0,"expectedRevision":1,"translations":[]}""")]
    [InlineData("""{"sortOrder":0,"isActive":true,"translations":[]}""")]
    [InlineData("""{"sortOrder":0,"isActive":true,"expectedRevision":1}""")]
    [InlineData("""{"sortOrder":0,"isActive":true,"expectedRevision":1,"translations":null}""")]
    [InlineData("""{"sortOrder":0,"isActive":true,"expectedRevision":0,"translations":[]}""")]
    [InlineData("""{"sortOrder":0,"isActive":true,"expectedRevision":1,"translations":[{"languageId":"01900000-0000-7000-8000-00000000bb01","name":"x"},null]}""")]
    public async Task Put_presence_and_shape_rejections_never_reach_the_handler(string body)
    {
        using var harness = Harness.Create();
        var revisionBefore = await harness.StoredRevisionAsync(harness.SeededId);

        var response = await harness.SendAsync("PUT", $"{ListPath}/{harness.SeededId}", body);

        Assert.Equal(StatusCodes.Status400BadRequest, response.Status);
        Assert.Equal(1, harness.Counters.Dispatches);
        Assert.Equal(0, harness.Counters.LanguageLookups);
        Assert.Equal(revisionBefore, await harness.StoredRevisionAsync(harness.SeededId));
    }

    [Fact]
    public async Task Get_with_empty_guid_is_rejected_by_the_real_query_validator()
    {
        using var harness = Harness.Create();

        var response = await harness.SendAsync("GET", $"{ListPath}/{Guid.Empty}");

        Assert.Equal(StatusCodes.Status400BadRequest, response.Status);
        Assert.Equal(1, harness.Counters.Dispatches);
        Assert.Equal(0, harness.Counters.LanguageLookups);
    }

    [Fact]
    public async Task List_stays_parameterless_and_content_language_never_comes_from_query_or_culture()
    {
        using var harness = Harness.Create();

        // A row carrying both configured languages, so a query/culture-driven filter would be visible.
        var created = await harness.SendAsync(
            "POST", ListPath,
            """{"code":"ordinary","sortOrder":0,"translations":[{"languageId":"01900000-0000-7000-8000-00000000bb01","name":"عادی"},{"languageId":"01900000-0000-7000-8000-00000000bb02","name":"Ordinary"}]}""");
        Assert.Equal(StatusCodes.Status201Created, created.Status);
        var id = created.Body.GetProperty("shippingTypeId").GetGuid();

        var plain = await harness.SendAsync("GET", ListPath);
        var withQuery = await harness.SendAsync(
            "GET",
            ListPath + "?languageId=01900000-0000-7000-8000-00000000bb02&culture=en-US",
            headers: new Dictionary<string, string> { ["Accept-Language"] = "en-US" });

        Assert.Equal(StatusCodes.Status200OK, plain.Status);
        Assert.Equal(StatusCodes.Status200OK, withQuery.Status);

        // Identical payload: no filter applied, no culture-driven projection, raw stored language ids intact.
        Assert.Equal(plain.Body.GetRawText(), withQuery.Body.GetRawText());
        var item = withQuery.Body.EnumerateArray()
            .Single(x => x.GetProperty("shippingTypeId").GetGuid() == id);
        var translations = item.GetProperty("translations");
        Assert.Equal(
            new[] { DefaultLanguage, SecondLanguage }.OrderBy(x => x),
            translations.EnumerateArray().Select(x => x.GetProperty("languageId").GetGuid()).OrderBy(x => x));
    }

    // ---------------------------------------------------------------------------------------------
    // Route -> request -> handler -> validator set equality (capability-scoped classification)
    // ---------------------------------------------------------------------------------------------

    [Fact]
    public void Four_route_classification_is_exactly_equal_to_the_shipped_capability_routes()
    {
        using var harness = Harness.Create();

        var shipped = harness.ShippingTypeRoutes();
        Assert.Equal(ShippingTypeRouteClassification.Expected.Count, shipped.Count);

        // Both directions: nothing missing, nothing extra.
        Assert.Equal(
            ShippingTypeRouteClassification.Expected.OrderBy(x => x, StringComparer.Ordinal),
            shipped.OrderBy(x => x, StringComparer.Ordinal));

        var classification = ShippingTypeRouteClassification.Classify(shipped);
        Assert.Equal(4, classification.Count);
        Assert.Equal(1, classification.Count(x => x.Request == nameof(ListShippingTypesQuery)));
        Assert.DoesNotContain(classification, x => x.Request == nameof(ListShippingTypesQuery) && x.Validator != "NO_VALIDATOR_REQUIRED");
        Assert.Equal("NO_VALIDATOR_REQUIRED", classification.Single(x => x.Request == nameof(ListShippingTypesQuery)).Validator);
        Assert.Equal(nameof(GetShippingTypeQueryValidator), classification.Single(x => x.Request == nameof(GetShippingTypeQuery)).Validator);
        Assert.Equal(nameof(CreateShippingTypeCommandValidator), classification.Single(x => x.Request == nameof(CreateShippingTypeCommand)).Validator);
        Assert.Equal(nameof(UpdateShippingTypeCommandValidator), classification.Single(x => x.Request == nameof(UpdateShippingTypeCommand)).Validator);
    }

    [Fact]
    public void The_three_validated_requests_resolve_their_validators_from_the_real_pipeline()
    {
        using var harness = Harness.Create();

        AssertValidators<GetShippingTypeQuery, GetShippingTypeQueryValidator>(harness, 1);
        AssertValidators<CreateShippingTypeCommand, CreateShippingTypeCommandValidator>(harness, 1);
        AssertValidators<UpdateShippingTypeCommand, UpdateShippingTypeCommandValidator>(harness, 1);

        // List is justified parameterless: no validators, and the request carries no route/query/body input.
        var listValidators = harness.Scope.ServiceProvider
            .GetServices<FluentValidation.IValidator<ListShippingTypesQuery>>()
            .ToArray();
        Assert.Empty(listValidators);
        Assert.Empty(typeof(ListShippingTypesQuery).GetProperties());
    }

    [Fact]
    public void Classification_detects_a_synthetic_extra_route_and_a_dropped_route()
    {
        var withExtra = ShippingTypeRouteClassification.Expected
            .Append("GET /v1/admin/shipping-types/export")
            .ToList();
        var dropped = ShippingTypeRouteClassification.Expected
            .Where(x => !x.StartsWith("PUT ", StringComparison.Ordinal))
            .ToList();

        Assert.Throws<InvalidOperationException>(() => ShippingTypeRouteClassification.Classify(withExtra));
        Assert.Throws<InvalidOperationException>(() => ShippingTypeRouteClassification.Classify(dropped));
    }

    // ---------------------------------------------------------------------------------------------
    // Helpers
    // ---------------------------------------------------------------------------------------------

    private static string CreateBody() =>
        """{"code":"ordinary","sortOrder":0,"translations":[{"languageId":"01900000-0000-7000-8000-00000000bb01","name":"عادی"}]}""";

    private static string UpdateBody(long expectedRevision, bool isActive, int sortOrder) =>
        $$"""{"sortOrder":{{sortOrder}},"isActive":{{(isActive ? "true" : "false")}},"expectedRevision":{{expectedRevision}},"translations":[]}""";

    private static string? ErrorCode(JsonElement body) =>
        body.TryGetProperty("errorCode", out var code) ? code.GetString() : null;

    private static void AssertValidators<TRequest, TValidator>(Harness harness, int expected)
    {
        var validators = harness.Scope.ServiceProvider
            .GetServices<FluentValidation.IValidator<TRequest>>()
            .ToArray();

        Assert.Equal(expected, validators.Length);
        Assert.Contains(validators, validator => validator.GetType() == typeof(TValidator));
    }

    /// <summary>
    /// Capability-scoped, test-owned classification of the four shipping-type routes. It is deliberately
    /// not a production framework or a global manifest: it only knows this capability and fails loudly on an
    /// extra or missing route so a silently dropped classification cannot pass.
    /// </summary>
    private static class ShippingTypeRouteClassification
    {
        internal static readonly IReadOnlyList<string> Expected =
        [
            "GET /v1/admin/shipping-types",
            "GET /v1/admin/shipping-types/{shippingTypeId:guid}",
            "POST /v1/admin/shipping-types",
            "PUT /v1/admin/shipping-types/{shippingTypeId:guid}",
        ];

        internal static IReadOnlyList<(string Route, string Request, string? Validator)> Classify(
            IReadOnlyList<string> routes)
        {
            var classified = new List<(string Route, string Request, string? Validator)>();
            foreach (var route in routes)
            {
                classified.Add(route switch
                {
                    "GET /v1/admin/shipping-types" =>
                        (route, nameof(ListShippingTypesQuery), "NO_VALIDATOR_REQUIRED"),
                    "GET /v1/admin/shipping-types/{shippingTypeId:guid}" =>
                        (route, nameof(GetShippingTypeQuery), nameof(GetShippingTypeQueryValidator)),
                    "POST /v1/admin/shipping-types" =>
                        (route, nameof(CreateShippingTypeCommand), nameof(CreateShippingTypeCommandValidator)),
                    "PUT /v1/admin/shipping-types/{shippingTypeId:guid}" =>
                        (route, nameof(UpdateShippingTypeCommand), nameof(UpdateShippingTypeCommandValidator)),
                    _ => throw new InvalidOperationException($"unclassified shipping-type route: {route}"),
                });
            }

            if (classified.Count != Expected.Count)
            {
                throw new InvalidOperationException(
                    $"capability route count {classified.Count} != expected {Expected.Count}");
            }

            return classified;
        }
    }

    // ---------------------------------------------------------------------------------------------
    // Harness: real composition, controlled platform seams only
    // ---------------------------------------------------------------------------------------------

    private sealed class Harness : IDisposable
    {
        private readonly WebApplication _app;

        private Harness(
            WebApplication app,
            IServiceScope scope,
            IReadOnlyList<RouteEndpoint> endpoints,
            Counters counters,
            CountingAuthorization authz,
            CountingAdminPanel panel)
        {
            _app = app;
            Scope = scope;
            Endpoints = endpoints;
            Counters = counters;
            Authz = authz;
            Panel = panel;
        }

        public IServiceScope Scope { get; }

        public IReadOnlyList<RouteEndpoint> Endpoints { get; }

        public Counters Counters { get; }

        public CountingAuthorization Authz { get; }

        public CountingAdminPanel Panel { get; }

        public string? LastLocation { get; private set; }

        public Guid SeededId { get; private set; }

        public IReadOnlyList<string> ShippingTypeRoutes() => Endpoints
            .Where(endpoint => (endpoint.RoutePattern.RawText ?? string.Empty).StartsWith(
                "/v1/admin/shipping-types", StringComparison.Ordinal))
            .Select(endpoint =>
            {
                var verb = string.Join(',', endpoint.Metadata.GetMetadata<IHttpMethodMetadata>()?.HttpMethods ?? []);
                var pattern = "/" + endpoint.RoutePattern.RawText!.Trim('/');
                return $"{verb} {(pattern == "/" ? pattern : pattern.TrimEnd('/')).Replace("//", "/")}";
            })
            .OrderBy(route => route, StringComparer.Ordinal)
            .ToArray();

        public static Harness Create(
            PanelDecision panel = PanelDecision.Allow,
            IReadOnlyList<string>? permissions = null,
            string scenario = "single-store-active-tenant",
            bool readCounting = false)
        {
            var counters = new Counters();
            var database = $"tb196r3-{Guid.NewGuid():N}";
            var panelSeam = new CountingAdminPanel(panel);
            var authz = new CountingAuthorization(counters, permissions ?? [ShippingTypeAdminAuthorizer.View, ShippingTypeAdminAuthorizer.Manage]);
            var (edition, tenant) = ContextFor(scenario);
            var languages = new CountingLanguageLookup(counters);

            var builder = WebApplication.CreateBuilder(new WebApplicationOptions
            {
                EnvironmentName = Environments.Production,
            });
            builder.Logging.ClearProviders();
            builder.Services.AddRouting();
            builder.Services.AddSingleton<IHostEnvironment>(new StubHostEnvironment());
            builder.Services.AddToobaObservabilityFoundation();
            builder.Services.AddToobaCqrsFoundation(typeof(CreateShippingTypeCommand).Assembly);

            // The Host's own exception envelope so an uncaught endpoint exception is rendered by production.
            builder.Services.AddProblemDetails();
            builder.Services.AddSingleton<IExceptionPresentationService, ExceptionPresentationService>();

            // Production endpoint presentation: real IShippingTypeAdminAuthorizer + Fulfillment error resources.
            builder.Services.AddFulfillmentEndpointPresentation();
            builder.Services.AddSingleton<IErrorCatalogContributor, FulfillmentErrorCatalogContributor>();
            builder.Services.AddSingleton<IAdminPanelAccess>(panelSeam);
            builder.Services.AddSingleton<IAuthorizationService>(authz);
            builder.Services.AddSingleton<ICurrentEdition>(new CurrentEdition(edition));
            builder.Services.AddSingleton<ICurrentTenant>(new CurrentTenant(tenant));

            // The exact four production capability registrations from FulfillmentModule (same types/lifetimes).
            builder.Services.AddDbContext<FulfillmentDbContext>(options =>
                options.UseInMemoryDatabase(database));
            builder.Services.AddScoped<ShippingTypeStore>();
            builder.Services.AddScoped<IShippingTypeStore>(sp => sp.GetRequiredService<ShippingTypeStore>());
            builder.Services.AddScoped<IShippingTypeReader>(sp => sp.GetRequiredService<ShippingTypeStore>());
            builder.Services.AddScoped<IShippingTypeLanguageFacts, LocalizationShippingTypeLanguageFacts>();
            builder.Services.AddSingleton<ILanguageLookup>(languages);

            // Controlled deterministic clock/id only; production implementation types.
            builder.Services.AddSingleton<IClock>(new FixedClock(Now));
            builder.Services.AddSingleton<IIdGenerator, UuidV7IdGenerator>();

            // Count ISender dispatch without replacing the handlers.
            builder.Services.AddScoped<ISender>(sp =>
                new CountingSender(sp.GetRequiredService<IMediator>(), counters));

            var app = builder.Build();
            app.MapFulfillmentEndpoints();

            var endpoints = ((IEndpointRouteBuilder)app).DataSources
                .SelectMany(dataSource => dataSource.Endpoints)
                .OfType<RouteEndpoint>()
                .ToArray();

            var scope = app.Services.CreateScope();
            var harness = new Harness(app, scope, endpoints, counters, authz, panelSeam);

            // One lawful seeded row so GET/PUT routes have a real target from the real store.
            harness.SeededId = harness.SeedAsync().GetAwaiter().GetResult();
            counters.Reset();
            return harness;
        }

        public async Task<(int Status, JsonElement Body)> SendAsync(
            string method,
            string path,
            string? body = null,
            IReadOnlyDictionary<string, string>? headers = null)
        {
            var endpoint = ResolveEndpoint(method, path);
            var context = CreateContext(method, path, headers);
            context.Request.Body = readCountingStream(body);

            await ExecuteAsync(endpoint, context);
            LastLocation = context.Response.Headers.Location.ToString() is { Length: > 0 } location
                ? location
                : null;

            return (context.Response.StatusCode, ReadBody(context));
        }

        public async Task<Guid> CreateAsync()
        {
            var response = await SendAsync("POST", ListPath, CreateBody());
            Assert.Equal(StatusCodes.Status201Created, response.Status);
            return response.Body.GetProperty("shippingTypeId").GetGuid();
        }

        public Task<long> StoredRevisionAsync(Guid id) => StoredAsync(id, entity => entity.Revision);

        public Task<DateTimeOffset> StoredUpdatedAtAsync(Guid id) => StoredAsync(id, entity => entity.UpdatedAt);

        public async Task<IReadOnlyList<Guid>> StoredRowsAsync()
        {
            var db = Scope.ServiceProvider.GetRequiredService<FulfillmentDbContext>();
            return await db.ShippingTypes.AsNoTracking()
                .Select(x => x.ShippingTypeId)
                .ToListAsync();
        }

        public void Dispose()
        {
            Scope.Dispose();
            _app.DisposeAsync().AsTask().GetAwaiter().GetResult();
        }

        private static (ToobaEdition Edition, TenantContext? Tenant) ContextFor(string scenario) => scenario switch
        {
            "single-store-active-tenant" => (ToobaEdition.SingleStore, Tenant(TenantStatus.Active)),
            "single-store-no-tenant" => (ToobaEdition.SingleStore, null),
            "single-store-inactive-tenant" => (ToobaEdition.SingleStore, Tenant(TenantStatus.Disabled)),
            "marketplace-no-tenant" => (ToobaEdition.Marketplace, null),
            "marketplace-leftover-tenant" => (ToobaEdition.Marketplace, Tenant(TenantStatus.Active)),
            "missing-edition" => (ToobaEdition.Unset, null),
            "unknown-edition" => ((ToobaEdition)999, null),
            _ => throw new ArgumentOutOfRangeException(nameof(scenario)),
        };

        private static TenantContext Tenant(TenantStatus status) => new(
            new TenantId("tenant-01"),
            status,
            new ConnectionReference("conn-01"),
            "Store",
            null,
            null,
            "shop.example",
            null);

        private async Task<Guid> SeedAsync()
        {
            var store = Scope.ServiceProvider.GetRequiredService<ShippingTypeStore>();
            return await store.CreateAsync(
                "seeded",
                5,
                [new ShippingTypePreparedTranslation(DefaultLanguage, "نمونه", null)],
                CancellationToken.None);
        }

        private async Task<T> StoredAsync<T>(Guid id, Func<Domain.Aggregates.ShippingType, T> selector)
        {
            var db = Scope.ServiceProvider.GetRequiredService<FulfillmentDbContext>();
            var entity = await db.ShippingTypes.AsNoTracking().SingleAsync(x => x.ShippingTypeId == id);
            return selector(entity);
        }

        private RouteEndpoint ResolveEndpoint(string method, string path)
        {
            var matchPath = path.Split('?')[0];
            return Endpoints
                .Where(endpoint => (endpoint.RoutePattern.RawText ?? string.Empty)
                    .StartsWith("/v1/admin/shipping-types", StringComparison.Ordinal))
                .Single(endpoint =>
                    (endpoint.Metadata.GetMetadata<IHttpMethodMetadata>()?.HttpMethods.Contains(method) ?? false)
                    && TemplateMatches(endpoint.RoutePattern.RawText!, matchPath));
        }

        private static bool TemplateMatches(string pattern, string path)
        {
            var segments = pattern.Trim('/').Split('/');
            var actual = path.Trim('/').Split('/');
            if (segments.Length != actual.Length)
            {
                return false;
            }

            for (var index = 0; index < segments.Length; index++)
            {
                var segment = segments[index];
                if (segment.StartsWith('{') && segment.EndsWith('}'))
                {
                    if (segment.Contains(":guid", StringComparison.Ordinal)
                        && !Guid.TryParse(actual[index], out _))
                    {
                        return false;
                    }

                    continue;
                }

                if (!string.Equals(segment, actual[index], StringComparison.Ordinal))
                {
                    return false;
                }
            }

            return true;
        }

        private DefaultHttpContext CreateContext(
            string method,
            string path,
            IReadOnlyDictionary<string, string>? headers)
        {
            var context = new DefaultHttpContext { RequestServices = Scope.ServiceProvider };
            var split = path.Split('?', 2);
            context.Request.Method = method;
            context.Request.Path = split[0];
            context.Request.QueryString = split.Length == 2 ? new QueryString("?" + split[1]) : QueryString.Empty;
            context.Request.ContentType = "application/json";
            context.Response.Body = new MemoryStream();

            if (headers is not null)
            {
                foreach (var (name, value) in headers)
                {
                    context.Request.Headers[name] = value;
                }
            }

            // Route values as routing would have produced them.
            var id = RouteGuid(split[0]);
            if (id is not null)
            {
                context.Request.RouteValues["shippingTypeId"] = id.Value.ToString();
            }

            _app.Services.GetRequiredService<IHttpContextAccessor>().HttpContext = context;
            return context;
        }

        private Stream readCountingStream(string? body)
        {
            var payload = Encoding.UTF8.GetBytes(body ?? string.Empty);
            return new CountingStream(new MemoryStream(payload), Counters);
        }

        private static Guid? RouteGuid(string path)
        {
            var segments = path.Trim('/').Split('/');
            if (segments.Length < 4)
            {
                return null;
            }

            return Guid.TryParse(segments[^1], out var id) ? id : null;
        }

        private async Task ExecuteAsync(RouteEndpoint endpoint, HttpContext context)
        {
            try
            {
                await endpoint.RequestDelegate!(context);
            }
            catch (Exception exception)
            {
                // Mirrors the Host pipeline: uncaught endpoint exceptions use the real central presenter.
                await _app.Services.GetRequiredService<IExceptionPresentationService>()
                    .WriteAsync(context, exception, CancellationToken.None);
            }
        }

        private static JsonElement ReadBody(HttpContext context)
        {
            context.Response.Body.Position = 0;
            using var document = JsonDocument.Parse(context.Response.Body);
            return document.RootElement.Clone();
        }
    }

    private sealed class CountingStream(Stream inner, Counters counters) : Stream
    {
        public override bool CanRead => true;

        public override bool CanSeek => inner.CanSeek;

        public override bool CanWrite => false;

        public override long Length => inner.Length;

        public override long Position
        {
            get => inner.Position;
            set => inner.Position = value;
        }

        public override void Flush() => inner.Flush();

        public override int Read(byte[] buffer, int offset, int count)
        {
            counters.ReadCalls++;
            return inner.Read(buffer, offset, count);
        }

        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            counters.ReadCalls++;
            return inner.ReadAsync(buffer, cancellationToken);
        }

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            counters.ReadCalls++;
            return inner.ReadAsync(buffer, offset, count, cancellationToken);
        }

        public override long Seek(long offset, SeekOrigin origin) => inner.Seek(offset, origin);

        public override void SetLength(long value) => inner.SetLength(value);

        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }

    private sealed class CountingSender(IMediator mediator, Counters counters) : ISender
    {
        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
        {
            counters.Dispatches++;
            counters.SenderRequests.Add(request.GetType().Name);
            return mediator.Send(request, cancellationToken);
        }

        public Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default)
            where TRequest : IRequest
        {
            counters.Dispatches++;
            counters.SenderRequests.Add(request!.GetType().Name);
            return mediator.Send(request, cancellationToken);
        }

        public Task<object?> Send(object request, CancellationToken cancellationToken = default)
        {
            counters.Dispatches++;
            counters.SenderRequests.Add(request.GetType().Name);
            return mediator.Send(request, cancellationToken);
        }

        public IAsyncEnumerable<TResponse> CreateStream<TResponse>(
            IStreamRequest<TResponse> request,
            CancellationToken cancellationToken = default) => mediator.CreateStream(request, cancellationToken);

        public IAsyncEnumerable<object?> CreateStream(object request, CancellationToken cancellationToken = default) =>
            mediator.CreateStream(request, cancellationToken);
    }

    private sealed class CountingLanguageLookup(Counters counters) : ILanguageLookup
    {
        public Task<IReadOnlyList<LanguageLookupSnapshot>> ListAsync(CancellationToken cancellationToken)
        {
            counters.LanguageLookups++;

            IReadOnlyList<LanguageLookupSnapshot> rows =
            [
                new(DefaultLanguage, "fa", "fa-IR", "fa", IsDefault: true)
                {
                    IsActive = true,
                    Direction = "rtl",
                    DisplayName = "Persian",
                    NativeName = "فارسی",
                    SortOrder = 1,
                },
                new(SecondLanguage, "en", "en-US", "en", IsDefault: false)
                {
                    IsActive = true,
                    Direction = "ltr",
                    DisplayName = "English",
                    NativeName = "English",
                    SortOrder = 2,
                },
            ];

            return Task.FromResult(rows);
        }
    }

    private sealed class CountingAdminPanel(PanelDecision decision) : IAdminPanelAccess
    {
        public Task<Guid> RequireAuthorizedAsync(HttpRequest request, CancellationToken cancellationToken) =>
            decision switch
            {
                PanelDecision.Allow => Task.FromResult(Actor),
                // Admin panel accepts only an admin actor; a seller-only actor is rejected here, before any
                // capability check. ISellerPanelAccess is intentionally not consulted by the capability.
                _ => throw new SemanticException(new SemanticError(FoundationErrorCodes.AdminAuthorizationDenied)),
            };
    }

    private sealed class CountingAuthorization(Counters counters, IReadOnlyList<string> permissions) : IAuthorizationService
    {
        public int Calls { get; private set; }

        public AuthorizationCallContext? LastCallContext { get; private set; }

        public string? LastResourceId { get; private set; }

        public Task<AuthorizationDecision> CanAsync(
            AuthorizationCheck check,
            CancellationToken cancellationToken)
        {
            Calls++;
            counters.AuthorizationChecks++;
            LastCallContext = check.CallContext;
            LastResourceId = check.Resource.Id;

            return Task.FromResult(permissions.Contains(check.Resource.Id, StringComparer.Ordinal)
                ? AuthorizationDecision.Allow()
                : AuthorizationDecision.Deny());
        }
    }

    private sealed class CurrentEdition(ToobaEdition edition) : ICurrentEdition
    {
        public EditionContext? Current { get; } = edition == ToobaEdition.Unset
            ? null
            : new EditionContext(edition, "deployment-01");
    }

    private sealed class CurrentTenant(TenantContext? tenant) : ICurrentTenant
    {
        public TenantContext? Current { get; } = tenant;
    }

    private sealed class Counters
    {
        public int Dispatches { get; set; }

        public int LanguageLookups { get; set; }

        public int ReadCalls { get; set; }

        public int AuthorizationChecks { get; set; }

        public List<string> SenderRequests { get; } = [];

        public void Reset()
        {
            Dispatches = 0;
            LanguageLookups = 0;
            ReadCalls = 0;
            AuthorizationChecks = 0;
            SenderRequests.Clear();
        }
    }

    private enum PanelDecision
    {
        Allow = 1,
        RejectSellerOnly = 2,
    }

    private sealed class StubHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Production;

        public string ApplicationName { get; set; } = "Tooba.Fulfillment.Tests";

        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;

        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
