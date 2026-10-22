using System.Diagnostics;
using System.Text;
using System.Text.Json;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Template;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.DependencyInjection;
using Tooba.BuildingBlocks.Observability.Tracing;
using Tooba.BuildingBlocks.Presentation;
using Tooba.BuildingBlocks.Presentation.Errors;
using Tooba.BuildingBlocks.Security;
using Tooba.Fulfillment.Contracts.Shipping;
using Tooba.Shipping.Application.Authoring.Commands;
using Tooba.Shipping.Application.Authoring.Models;
using Tooba.Shipping.Application.Authoring.Ports;
using Tooba.Shipping.Application.Authoring.Queries;
using Tooba.Shipping.Application.Rates.Ports;
using Tooba.Shipping.Contracts.Authoring;
using Tooba.Shipping.Contracts.Errors;
using Tooba.Shipping.Domain.Authoring;
using Tooba.Shipping.Endpoints;
using Tooba.Shipping.Endpoints.Admin;
using Tooba.Shipping.Infrastructure.Authoring;
using Tooba.Shipping.Infrastructure.Persistence;
using Tooba.StoreContext.Contracts.Current;
using Xunit;

namespace Tooba.Shipping.Tests.Authoring;

/// <summary>
/// TB-SHIPPING-ADMIN-DRAFT-TYPE-ASSIGNMENT-197 — the optional owner-local draft type selection through the
/// REAL HTTP delegates, the real owner authorizer, the real body reader, the real MediatR validation
/// pipeline and the real draft directory. Only platform authorization edges, the destination/option seams,
/// the commerce context and the Fulfillment Contracts catalog facts are controlled fixtures.
/// </summary>
[Collection(nameof(Tooba.Shipping.Tests.Observability.ShippingModuleCallTracingCollection))]
public sealed class ShippingAuthoringDraftTypeAssignmentEndpointTests
{
    private const string AdminDrafts = "/v1/admin/shipping-services/authoring-drafts";
    private const string AdminTypes = "/v1/admin/shipping-services/authoring-types";
    private const string SellerDrafts = "/v1/seller/shipping-services/authoring-drafts";
    private const string SellerTypes = "/v1/seller/shipping-services/authoring-types";
    private static readonly Guid Iran = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid Tehran = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid PersianLanguage = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly Guid EnglishLanguage = Guid.Parse("55555555-5555-5555-5555-555555555555");
    private static readonly Guid ArabicLanguage = Guid.Parse("66666666-6666-6666-6666-666666666666");

    [Fact]
    public async Task Active_choices_current_assignment_and_rejected_payloads_are_truthful_on_real_admin_routes()
    {
        using var harness = Harness.Create("type-http-a");
        var draftId = await harness.CreateDraftAsync();
        var path = $"{AdminDrafts}/{draftId}/method-type";

        // EMPTY active catalog is valid, not an error: no fallback success with invented types.
        var (emptyStatus, _, empty) = await harness.SendAsync(HttpMethod.Get, AdminTypes);
        Assert.Equal(StatusCodes.Status200OK, emptyStatus);
        Assert.Empty(empty.GetProperty("types").EnumerateArray());

        // One active and one inactive definition with THREE raw language rows each.
        var active = harness.AddType("post-express", true, 11, 3,
            (PersianLanguage, "پست پیشتاز", "سریع"),
            (EnglishLanguage, "Express", null),
            (ArabicLanguage, "سریع", "وصف"));
        var inactive = harness.AddType("post-economy", false, 12, 5, (PersianLanguage, "اقتصادی", null));

        var (choicesStatus, _, choices) = await harness.SendAsync(HttpMethod.Get, AdminTypes);
        Assert.Equal(StatusCodes.Status200OK, choicesStatus);
        var types = choices.GetProperty("types").EnumerateArray().ToArray();
        var only = Assert.Single(types);
        Assert.Equal(active, only.GetProperty("shippingTypeId").GetGuid());
        Assert.Equal("post-express", only.GetProperty("code").GetString());
        Assert.Equal(11, only.GetProperty("sortOrder").GetInt32());
        Assert.Equal(3, only.GetProperty("revision").GetInt64());
        var translations = only.GetProperty("translations").EnumerateArray().ToArray();
        Assert.Equal(3, translations.Length);
        Assert.Equal(PersianLanguage, translations[0].GetProperty("languageId").GetGuid());
        Assert.Equal("پست پیشتاز", translations[0].GetProperty("name").GetString());
        Assert.Equal("سریع", translations[0].GetProperty("description").GetString());
        Assert.Equal(EnglishLanguage, translations[1].GetProperty("languageId").GetGuid());
        Assert.Equal(JsonValueKind.Null, translations[1].GetProperty("description").ValueKind);

        // The interface culture never selects a content language: switching Accept-Language changes nothing.
        harness.AcceptLanguage = "en";
        var (_, _, english) = await harness.SendAsync(HttpMethod.Get, AdminTypes);
        Assert.Equal("پست پیشتاز", english.GetProperty("types")[0].GetProperty("translations")[0].GetProperty("name").GetString());
        harness.AcceptLanguage = "fa";

        // Absent assignment is explicitly UNCONFIGURED and needs no foreign lookup at all.
        harness.CatalogReads = 0;
        var (currentStatus, _, current) = await harness.SendAsync(HttpMethod.Get, path);
        Assert.Equal(StatusCodes.Status200OK, currentStatus);
        Assert.Equal(JsonValueKind.Null, current.GetProperty("shippingTypeId").ValueKind);
        Assert.Equal((int)ShippingAuthoringTypeResolution.Unconfigured, current.GetProperty("resolution").GetInt32());
        Assert.Equal(0, harness.CatalogReads);

        // Assigning the ACTIVE definition advances the shared root revision once.
        var (assignStatus, _, assigned) = await harness.SendAsync(
            HttpMethod.Put, path, $"{{\"expectedRevision\":1,\"shippingTypeId\":\"{active}\"}}");
        Assert.True(assignStatus == StatusCodes.Status200OK, assigned.ToString());
        Assert.Equal(active, assigned.GetProperty("shippingTypeId").GetGuid());
        Assert.Equal((int)ShippingAuthoringTypeResolution.Active, assigned.GetProperty("resolution").GetInt32());
        Assert.Equal(2, assigned.GetProperty("revision").GetInt64());
        Assert.Equal(active, assigned.GetProperty("shippingType").GetProperty("shippingTypeId").GetGuid());

        // Inactive and unknown ids are refused with a stable code and no mutation.
        var (inactiveStatus, _, inactiveBody) = await harness.SendAsync(
            HttpMethod.Put, path, $"{{\"expectedRevision\":2,\"shippingTypeId\":\"{inactive}\"}}");
        Assert.Equal(StatusCodes.Status400BadRequest, inactiveStatus);
        Assert.Equal(ShippingErrorCodes.AuthoringTypeSelectionUnavailable, inactiveBody.GetProperty("errorCode").GetString());
        var (unknownStatus, _, unknownBody) = await harness.SendAsync(
            HttpMethod.Put, path, $"{{\"expectedRevision\":2,\"shippingTypeId\":\"{Guid.NewGuid()}\"}}");
        Assert.Equal(StatusCodes.Status400BadRequest, unknownStatus);
        Assert.Equal(ShippingErrorCodes.AuthoringTypeSelectionUnavailable, unknownBody.GetProperty("errorCode").GetString());
        Assert.Equal(2, (await harness.ReadDraftAsync(draftId)).Revision);

        // An identical ACTIVE selection is a truthful no-op: revision and timestamp are preserved.
        var before = await harness.ReadDraftAsync(draftId);
        var (noOpStatus, _, noOp) = await harness.SendAsync(
            HttpMethod.Put, path, $"{{\"expectedRevision\":2,\"shippingTypeId\":\"{active}\"}}");
        Assert.Equal(StatusCodes.Status200OK, noOpStatus);
        Assert.Equal(2, noOp.GetProperty("revision").GetInt64());
        var afterNoOp = await harness.ReadDraftAsync(draftId);
        Assert.Equal(before.UpdatedAt, afterNoOp.UpdatedAt);

        // A stale token wins BEFORE any foreign lookup, even for a clearing request.
        harness.CatalogReads = 0;
        var (staleStatus, _, stale) = await harness.SendAsync(
            HttpMethod.Put, path, "{\"expectedRevision\":1,\"shippingTypeId\":null}");
        Assert.Equal(StatusCodes.Status409Conflict, staleStatus);
        Assert.Equal(ShippingErrorCodes.AuthoringDraftRevisionConflict, stale.GetProperty("errorCode").GetString());
        Assert.Equal(0, harness.CatalogReads);

        // Omission is NOT a clear: the key is required by the transport contract.
        var (omittedStatus, _, omitted) = await harness.SendAsync(
            HttpMethod.Put, path, "{\"expectedRevision\":2}");
        Assert.Equal(StatusCodes.Status400BadRequest, omittedStatus);
        Assert.Equal("validation.failed", omitted.GetProperty("errorCode").GetString());
        Assert.Contains("shipping.validation.body_invalid", omitted.GetRawText(), StringComparison.Ordinal);
        Assert.Equal(2, (await harness.ReadDraftAsync(draftId)).Revision);

        // Guid.Empty, a malformed id and a non-positive revision keep stable canonical codes.
        var (emptyGuidStatus, _, emptyGuidBody) = await harness.SendAsync(
            HttpMethod.Put, path, $"{{\"expectedRevision\":2,\"shippingTypeId\":\"{Guid.Empty}\"}}");
        Assert.Equal(StatusCodes.Status400BadRequest, emptyGuidStatus);
        Assert.Contains("shipping.validation.type_selection_invalid", emptyGuidBody.GetRawText(), StringComparison.Ordinal);
        var (malformedStatus, _, malformed) = await harness.SendAsync(
            HttpMethod.Put, path, "{\"expectedRevision\":2,\"shippingTypeId\":\"not-a-guid\"}");
        Assert.Equal(StatusCodes.Status400BadRequest, malformedStatus);
        Assert.False(string.IsNullOrWhiteSpace(malformed.GetProperty("errorCode").GetString()));
        var (revisionStatus, _, revisionBody) = await harness.SendAsync(
            HttpMethod.Put, path, $"{{\"expectedRevision\":0,\"shippingTypeId\":\"{active}\"}}");
        Assert.Equal(StatusCodes.Status400BadRequest, revisionStatus);
        Assert.Contains("shipping.validation.expected_revision_invalid", revisionBody.GetRawText(), StringComparison.Ordinal);

        // Explicit null clears with NO foreign read and advances the same revision once.
        harness.CatalogReads = 0;
        var (clearStatus, _, cleared) = await harness.SendAsync(
            HttpMethod.Put, path, "{\"expectedRevision\":2,\"shippingTypeId\":null}");
        Assert.Equal(StatusCodes.Status200OK, clearStatus);
        Assert.Equal(JsonValueKind.Null, cleared.GetProperty("shippingTypeId").ValueKind);
        Assert.Equal((int)ShippingAuthoringTypeResolution.Unconfigured, cleared.GetProperty("resolution").GetInt32());
        Assert.Equal(3, cleared.GetProperty("revision").GetInt64());
        Assert.Equal(0, harness.CatalogReads);

        // Deactivating the definition AFTER assignment is visible on read and never writes to the draft.
        var assignedAgain = await harness.SendAsync(
            HttpMethod.Put, path, $"{{\"expectedRevision\":3,\"shippingTypeId\":\"{active}\"}}");
        Assert.Equal(StatusCodes.Status200OK, assignedAgain.Status);
        harness.Deactivate(active);
        var stamp = await harness.ReadDraftAsync(draftId);
        var (inactiveReadStatus, _, inactiveRead) = await harness.SendAsync(HttpMethod.Get, path);
        Assert.Equal(StatusCodes.Status200OK, inactiveReadStatus);
        Assert.Equal((int)ShippingAuthoringTypeResolution.Inactive, inactiveRead.GetProperty("resolution").GetInt32());
        Assert.Equal(active, inactiveRead.GetProperty("shippingTypeId").GetGuid());
        Assert.Equal(stamp.Revision, (await harness.ReadDraftAsync(draftId)).Revision);
    }

    [Fact]
    public async Task Deleting_a_referenced_definition_reports_missing_without_clearing_the_draft()
    {
        using var harness = Harness.Create("type-http-b");
        var draftId = await harness.CreateDraftAsync();
        var type = harness.AddType("post-vanished", true, 1, 2, (PersianLanguage, "پیک", null));
        var path = $"{AdminDrafts}/{draftId}/method-type";
        var (assignStatus, _, _) = await harness.SendAsync(
            HttpMethod.Put, path, $"{{\"expectedRevision\":1,\"shippingTypeId\":\"{type}\"}}");
        Assert.Equal(StatusCodes.Status200OK, assignStatus);

        harness.Remove(type);
        var (status, _, body) = await harness.SendAsync(HttpMethod.Get, path);
        Assert.Equal(StatusCodes.Status200OK, status);
        Assert.Equal(type, body.GetProperty("shippingTypeId").GetGuid());
        Assert.Equal((int)ShippingAuthoringTypeResolution.Missing, body.GetProperty("resolution").GetInt32());
        Assert.Equal(JsonValueKind.Null, body.GetProperty("shippingType").ValueKind);
        Assert.Equal(2, (await harness.ReadDraftAsync(draftId)).Revision);

        // A PUT of that now-missing id is a refusal, not an implicit clear.
        var (putStatus, _, putBody) = await harness.SendAsync(
            HttpMethod.Put, path, $"{{\"expectedRevision\":2,\"shippingTypeId\":\"{type}\"}}");
        Assert.Equal(StatusCodes.Status400BadRequest, putStatus);
        Assert.Equal(ShippingErrorCodes.AuthoringTypeSelectionUnavailable, putBody.GetProperty("errorCode").GetString());
        Assert.Equal(2, (await harness.ReadDraftAsync(draftId)).Revision);
    }

    [Fact]
    public async Task Pre_dispatch_denials_foreign_owner_and_seller_paths_never_touch_the_catalog()
    {
        var draftIdText = Guid.NewGuid();

        // A view-only admin cannot PUT: refused before dispatch and before any catalog I/O.
        using (var viewOnly = Harness.Create("type-http-c", adminAllowed: [ShippingAdminPermissions.View]))
        {
            var (status, _, body) = await viewOnly.SendAsync(
                HttpMethod.Put,
                $"{AdminDrafts}/{draftIdText}/method-type",
                $"{{\"expectedRevision\":1,\"shippingTypeId\":\"{Guid.NewGuid()}\"}}");
            Assert.Equal(StatusCodes.Status403Forbidden, status);
            Assert.Equal(0, viewOnly.SentCount);
            Assert.Equal(0, viewOnly.CatalogReads);
            Assert.NotEqual(JsonValueKind.Null, body.ValueKind);
        }

        using var harness = Harness.Create("type-http-d");
        var draftId = await harness.CreateDraftAsync();
        var type = harness.AddType("post-active", true, 1, 1, (PersianLanguage, "پیشتاز", null));
        var path = $"{AdminDrafts}/{draftId}/method-type";

        // A foreign owner gets the same safe not-found and ZERO foreign lookup.
        harness.CatalogReads = 0;
        var (foreignStatus, _, foreign) = await harness.SendAsync(
            HttpMethod.Get, path, tenantId: "store-b");
        Assert.Equal(StatusCodes.Status404NotFound, foreignStatus);
        Assert.Equal(ShippingErrorCodes.AuthoringDraftNotFound, foreign.GetProperty("errorCode").GetString());
        Assert.Equal(0, harness.CatalogReads);

        var (foreignPutStatus, _, foreignPut) = await harness.SendAsync(
            HttpMethod.Put, path, $"{{\"expectedRevision\":1,\"shippingTypeId\":\"{type}\"}}", tenantId: "store-b");
        Assert.Equal(StatusCodes.Status404NotFound, foreignPutStatus);
        Assert.Equal(ShippingErrorCodes.AuthoringDraftNotFound, foreignPut.GetProperty("errorCode").GetString());
        Assert.Equal(0, harness.CatalogReads);
        Assert.Equal(1, (await harness.ReadDraftAsync(draftId)).Revision);

        // The seller audience: granted whole-owner permission succeeds on the SAME use case.
        using (var marketplace = Harness.Create(
                   "type-http-e",
                   edition: ToobaEdition.Marketplace,
                   marketplace: true,
                   tenantId: null,
                   sellerGrants: [
                       new PlatformPermissionGrant(ShippingAdminPermissions.View, PlatformAccessScopeKind.GlobalWithinOwner, null, false),
                       new PlatformPermissionGrant(ShippingAdminPermissions.Manage, PlatformAccessScopeKind.GlobalWithinOwner, null, false),
                   ]))
        {
            var sellerDraft = await marketplace.CreateSellerDraftAsync();
            var sellerType = marketplace.AddType("seller-type", true, 1, 1, (PersianLanguage, "فروشنده", null));
            var (choicesStatus, _, choices) = await marketplace.SendAsync(HttpMethod.Get, SellerTypes);
            Assert.Equal(StatusCodes.Status200OK, choicesStatus);
            Assert.Single(choices.GetProperty("types").EnumerateArray());
            var (okStatus, _, ok) = await marketplace.SendAsync(
                HttpMethod.Put,
                $"{SellerDrafts}/{sellerDraft}/method-type",
                $"{{\"expectedRevision\":1,\"shippingTypeId\":\"{sellerType}\"}}");
            Assert.True(okStatus == StatusCodes.Status200OK, ok.ToString());
            Assert.Equal(sellerType, ok.GetProperty("shippingTypeId").GetGuid());
        }

        // SingleStore seller routes stay canonically unavailable before any seller resolution.
        using var singleStore = Harness.Create("type-http-f");
        var adminDraft = await singleStore.CreateDraftAsync();
        var (unavailable, _, unavailableBody) = await singleStore.SendAsync(
            HttpMethod.Get, $"{SellerDrafts}/{adminDraft}/method-type");
        Assert.Equal(StatusCodes.Status404NotFound, unavailable);
        Assert.Equal(
            ShippingErrorCodes.AuthoringOwnerScopeUnavailable,
            unavailableBody.GetProperty("errorCode").GetString());
        Assert.Equal(0, singleStore.SellerCalls);
        Assert.Equal(0, singleStore.CatalogReads);
    }

    [Fact]
    public async Task Catalog_exceptions_and_cancellation_propagate_instead_of_becoming_empty_or_unconfigured()
    {
        using var harness = Harness.Create("type-http-g");
        var draftId = await harness.CreateDraftAsync();
        harness.FailWith = () => throw new InvalidOperationException("catalog unavailable");

        // DIRECT handler invocation, past HTTP rendering: a dependency failure is never a successful
        // empty list and never an "unconfigured" draft.
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await harness.SendToHandlerAsync(new GetShippingAuthoringTypeChoicesQuery(
                ShippingAuthoringOwnerScope.ForStore("store-a"))));

        var type = Guid.NewGuid();
        await harness.WithContextAsync(async context =>
        {
            var draft = await context.AuthoringDrafts.SingleAsync(candidate => candidate.DraftId == draftId);
            var plan = draft.PreflightTypeAssignment(type);
            draft.ApplyTypeAssignment(plan, DateTimeOffset.UtcNow);
            await context.SaveChangesAsync();
        });

        harness.FailWith = () => throw new OperationCanceledException();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await harness.SendToHandlerAsync(new GetShippingAuthoringDraftTypeAssignmentQuery(
                ShippingAuthoringOwnerScope.ForStore("store-a"), draftId)));
    }

    [Fact]
    public void Every_actual_fulfillment_read_gets_one_child_span_and_early_branches_get_none()
    {
        var reader = new RecordingReader();
        var adapter = new FulfillmentShippingTypeCatalogReader(reader, new ModuleCallTracer());
        var spans = new List<Activity>();

        using var listener = new ActivityListener
        {
            ShouldListenTo = _ => true,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = activity =>
            {
                if (activity.DisplayName.StartsWith("module.Shipping.Fulfillment.", StringComparison.Ordinal))
                {
                    spans.Add(activity);
                }
            },
        };

        var id = Guid.NewGuid();
        using var cancellation = new CancellationTokenSource();
        ActivitySource.AddActivityListener(listener);
        Assert.Null(Activity.Current);
        _ = adapter.GetByIdsAsync([id], cancellation.Token).GetAwaiter().GetResult();
        Assert.Null(Activity.Current);
        _ = adapter.ListActiveAsync(cancellation.Token).GetAwaiter().GetResult();
        Assert.Null(Activity.Current);
        _ = adapter.GetByIdsAsync([], cancellation.Token).GetAwaiter().GetResult();
        listener.Dispose();

        // One span per ACTUAL foreign read (2), a distinct constant operation name per path, and none for
        // the empty request that never calls the target. The exact caller token is forwarded unchanged.
        Assert.Equal(2, reader.Calls);
        Assert.Equal(2, spans.Count);
        Assert.Equal(2, spans.Select(span => span.DisplayName).Distinct(StringComparer.Ordinal).Count());
        Assert.Equal(2, reader.Tokens.Count(token => token == cancellation.Token));
        Assert.Contains(spans, span => span.DisplayName.EndsWith("ListActiveShippingTypes", StringComparison.Ordinal));
        Assert.Contains(spans, span => span.DisplayName.EndsWith("GetShippingTypesByIds", StringComparison.Ordinal));

        // No payload/PII reaches telemetry: neither the requested id nor any definition code/name is a tag
        // value, the span is a real child of no ambient activity, and it is disposed once (ended).
        foreach (var span in spans)
        {
            var tags = string.Join("|", span.Tags.Select(tag => tag.Key + "=" + tag.Value));
            Assert.DoesNotContain(id.ToString("D"), tags, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("post-express", tags, StringComparison.OrdinalIgnoreCase);
            Assert.Null(span.Parent);
            Assert.Equal(ActivityStatusCode.Ok, span.Status);
        }
    }

    [Fact]
    public void A_failed_foreign_read_records_only_the_exception_type_and_rethrows_unchanged()
    {
        var reader = new RecordingReader
        {
            FailWith = () => throw new InvalidOperationException("catalog-secret-post-express"),
        };
        var adapter = new FulfillmentShippingTypeCatalogReader(reader, new ModuleCallTracer());
        var spans = new List<Activity>();

        using var listener = new ActivityListener
        {
            ShouldListenTo = _ => true,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = activity =>
            {
                if (activity.DisplayName.StartsWith("module.Shipping.Fulfillment.", StringComparison.Ordinal))
                {
                    spans.Add(activity);
                }
            },
        };

        ActivitySource.AddActivityListener(listener);
        var thrown = Assert.Throws<InvalidOperationException>(
            () => adapter.GetByIdsAsync([Guid.NewGuid()], CancellationToken.None).GetAwaiter().GetResult());
        listener.Dispose();

        Assert.Equal("catalog-secret-post-express", thrown.Message);
        var span = Assert.Single(spans);
        Assert.Equal(ActivityStatusCode.Error, span.Status);
        var exceptionTag = Assert.Single(span.Tags, tag => tag.Key == TracingTagNames.ExceptionType);
        Assert.Equal(typeof(InvalidOperationException).FullName, exceptionTag.Value);
        Assert.DoesNotContain("catalog-secret-post-express", string.Join("|", span.Tags.Select(tag => tag.Value)), StringComparison.Ordinal);
    }

    private sealed class RecordingReader : IShippingTypeReader
    {
        private readonly List<CancellationToken> _tokens = [];

        public int Calls { get; private set; }

        public IReadOnlyList<CancellationToken> Tokens => _tokens;

        public Action? FailWith { get; init; }

        public Task<IReadOnlyList<ShippingTypeSnapshot>> ListActiveAsync(CancellationToken cancellationToken)
        {
            Calls++;
            _tokens.Add(cancellationToken);
            FailWith?.Invoke();
            return Task.FromResult<IReadOnlyList<ShippingTypeSnapshot>>([]);
        }

        public Task<IReadOnlyList<ShippingTypeSnapshot>> GetByIdsAsync(
            IReadOnlyCollection<Guid> shippingTypeIds,
            CancellationToken cancellationToken)
        {
            Calls++;
            _tokens.Add(cancellationToken);
            FailWith?.Invoke();
            return Task.FromResult<IReadOnlyList<ShippingTypeSnapshot>>([]);
        }
    }

    private sealed class Harness : IDisposable
    {
        private readonly WebApplication _app;
        private readonly IServiceScope _scope;
        private readonly Counter _counter;
        private readonly FakeCatalog _catalog;

        private Harness(WebApplication app, IServiceScope scope, Counter counter, FakeCatalog catalog, int sellerCalls)
        {
            _app = app;
            _scope = scope;
            _counter = counter;
            _catalog = catalog;
            SellerCalls = sellerCalls;
        }

        public string AcceptLanguage { get; set; } = "fa";

        public TenantContext? DefaultTenant { get; set; }

        public int SellerCalls { get; private set; }

        public int SentCount => _counter.Sent;

        public int CatalogReads
        {
            get => _catalog.Reads;
            set => _catalog.Reads = value;
        }

        public Action? FailWith
        {
            get => _catalog.FailWith;
            set => _catalog.FailWith = value;
        }

        public static Harness Create(
            string database,
            ToobaEdition edition = ToobaEdition.SingleStore,
            IReadOnlyList<string>? adminAllowed = null,
            IReadOnlyList<PlatformPermissionGrant>? sellerGrants = null,
            bool marketplace = false,
            string? tenantId = "store-a")
        {
            var counter = new Counter();
            var catalog = new FakeCatalog();
            var tenant = new StubCurrentTenant(marketplace ? null : Tenant(tenantId ?? "store-a"));
            var seller = new RecordingSellerAccess();
            var commerce = new FakeContextSource(edition, "USD");

            var builder = WebApplication.CreateBuilder(new WebApplicationOptions
            {
                EnvironmentName = Environments.Production,
            });
            builder.Logging.ClearProviders();
            builder.Services.AddRouting();
            builder.Services.AddSingleton<IHostEnvironment>(new StubHostEnvironment());
            builder.Services.AddToobaObservabilityFoundation();
            builder.Services.AddShippingEndpointPresentation();
            builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(SentCountingBehavior<,>));
            builder.Services.AddToobaCqrsFoundation(typeof(CreateShippingAuthoringDraftCommand).Assembly);
            builder.Services.AddProblemDetails();
            builder.Services.AddSingleton<IExceptionPresentationService, ExceptionPresentationService>();
            builder.Services.AddDbContext<ShippingDbContext>(options => options.UseInMemoryDatabase(database));
            builder.Services.AddScoped<IShippingAuthoringDraftDirectory, ShippingAuthoringDraftDirectory>();
            builder.Services.AddSingleton<IShippingTypeCatalogReader>(catalog);
            builder.Services.AddSingleton<IShippingAdminAuthorizer>(
                new RecordingAdminAuthorizer(adminAllowed ?? [ShippingAdminPermissions.View, ShippingAdminPermissions.Manage]));
            builder.Services.AddSingleton<ISellerPanelAccess>(seller);
            builder.Services.AddSingleton<IPlatformEffectiveAccessReader>(
                new RecordingEffectiveAccess(sellerGrants ?? [
                    new PlatformPermissionGrant(ShippingAdminPermissions.View, PlatformAccessScopeKind.GlobalWithinOwner, null, false),
                    new PlatformPermissionGrant(ShippingAdminPermissions.Manage, PlatformAccessScopeKind.GlobalWithinOwner, null, false),
                ]));
            builder.Services.AddSingleton<ICurrentEdition>(
                new StubCurrentEdition(new EditionContext(edition, "deployment-test")));
            builder.Services.AddSingleton<ICurrentTenant>(tenant);
            builder.Services.AddSingleton(tenant);
            builder.Services.AddSingleton<IShippingMethodOptionValidator>(new FakeMethodValidator());
            builder.Services.AddSingleton<IShippingAuthoringDestinationResolver>(new FakeResolver());
            builder.Services.AddSingleton<IShippingAuthoringContextSource>(commerce);
            builder.Services.AddSingleton(counter);

            var app = builder.Build();
            app.MapShippingEndpoints();
            var scope = app.Services.CreateScope();
            var harness = new Harness(app, scope, counter, catalog, 0)
            {
                DefaultTenant = marketplace ? null : Tenant(tenantId ?? "store-a"),
            };
            seller.OnCall = () => harness.SellerCalls++;
            return harness;
        }

        public static TenantContext Tenant(string tenantId) =>
            new(
                new TenantId(tenantId),
                TenantStatus.Active,
                new ConnectionReference("store-db"),
                "Test Store",
                null,
                null,
                "store.test",
                null);

        public Guid AddType(string code, bool isActive, int sortOrder, long revision, params (Guid LanguageId, string Name, string? Description)[] translations)
            => _catalog.Add(code, isActive, sortOrder, revision, translations);

        public void Deactivate(Guid id) => _catalog.SetActive(id, false);

        public void Remove(Guid id) => _catalog.Remove(id);

        public async Task<Guid> CreateDraftAsync()
        {
            var (status, _, body) = await SendAsync(HttpMethod.Post, AdminDrafts, CreateBody());
            Assert.Equal(StatusCodes.Status201Created, status);
            return body.GetProperty("draftId").GetGuid();
        }

        public async Task<Guid> CreateSellerDraftAsync()
        {
            var (status, _, body) = await SendAsync(HttpMethod.Post, SellerDrafts, CreateBody());
            Assert.Equal(StatusCodes.Status201Created, status);
            return body.GetProperty("draftId").GetGuid();
        }

        public async Task<(long Revision, DateTimeOffset UpdatedAt)> ReadDraftAsync(Guid draftId)
            => await WithContextAsync(async context =>
            {
                var draft = await context.AuthoringDrafts.AsNoTracking().SingleAsync(item => item.DraftId == draftId);
                return (draft.Revision, draft.UpdatedAt);
            });

        public async Task WithContextAsync(Func<ShippingDbContext, Task> action)
            => await action(_scope.ServiceProvider.GetRequiredService<ShippingDbContext>());

        public async Task SendToHandlerAsync<TResponse>(MediatR.IRequest<TResponse> request)
            => await _scope.ServiceProvider.GetRequiredService<MediatR.IMediator>().Send(request);

        public async Task<T> WithContextAsync<T>(Func<ShippingDbContext, Task<T>> action)
            => await action(_scope.ServiceProvider.GetRequiredService<ShippingDbContext>());

        public async Task<(int Status, string? Location, JsonElement Body)> SendAsync(
            HttpMethod method,
            string path,
            string? json = null,
            string? tenantId = null)
        {
            var current = _scope.ServiceProvider.GetRequiredService<StubCurrentTenant>();
            current.Current = tenantId is null ? DefaultTenant : Tenant(tenantId);

            var endpoint = FindEndpoint(path, method.Method);
            var context = CreateContext(method, path, json);
            var routeValues = new RouteValueDictionary();
            TemplateMatches(endpoint, path, routeValues);
            context.Request.RouteValues = routeValues;
            try
            {
                await endpoint.RequestDelegate!(context);
            }
            catch (Exception exception)
            {
                await _app.Services.GetRequiredService<IExceptionPresentationService>()
                    .WriteAsync(context, exception, CancellationToken.None);
            }

            return (
                context.Response.StatusCode,
                context.Response.Headers.Location.ToString() is { Length: > 0 } location ? location : null,
                ReadBody(context));
        }

        public void Dispose()
        {
            _scope.Dispose();
            _app.DisposeAsync().AsTask().GetAwaiter().GetResult();
        }

        private static string CreateBody()
            => "{\"serviceCode\":\"post\",\"optionCode\":null,\"regionGroups\":["
               + $"{{\"groupId\":null,\"areas\":[{{\"kind\":1,\"countryId\":\"{Iran}\",\"administrativeAreaId\":\"{Tehran}\"}}]}}]}}";

        private RouteEndpoint FindEndpoint(string path, string method) =>
            ((IEndpointRouteBuilder)_app).DataSources
                .SelectMany(dataSource => dataSource.Endpoints)
                .OfType<RouteEndpoint>()
                .Single(endpoint =>
                    TemplateMatches(endpoint, path, new RouteValueDictionary())
                    && (endpoint.Metadata.GetMetadata<IHttpMethodMetadata>()?.HttpMethods.Contains(method) ?? false));

        private static bool TemplateMatches(RouteEndpoint endpoint, string path, RouteValueDictionary values)
        {
            var template = TemplateParser.Parse(endpoint.RoutePattern.RawText!.TrimStart('/'));
            var matcher = new TemplateMatcher(template, new RouteValueDictionary());
            return matcher.TryMatch(new PathString(path), values);
        }

        private DefaultHttpContext CreateContext(HttpMethod method, string path, string? json)
        {
            var context = new DefaultHttpContext { RequestServices = _scope.ServiceProvider };
            context.Request.Method = method.Method;
            context.Request.Path = path;
            context.Request.Headers.AcceptLanguage = AcceptLanguage;
            context.Request.ContentType = "application/json";
            if (json is not null)
            {
                context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(json));
            }

            context.Response.Body = new MemoryStream();
            _app.Services.GetRequiredService<IHttpContextAccessor>().HttpContext = context;
            return context;
        }

        private static JsonElement ReadBody(HttpContext context)
        {
            context.Response.Body.Position = 0;
            if (context.Response.Body.Length == 0)
            {
                return default;
            }

            using var document = JsonDocument.Parse(context.Response.Body);
            return document.RootElement.Clone();
        }
    }

    private sealed class Counter
    {
        public int Sent { get; set; }
    }

    private sealed class SentCountingBehavior<TRequest, TResponse>(Counter counter)
        : IPipelineBehavior<TRequest, TResponse>
        where TRequest : notnull
    {
        public async Task<TResponse> Handle(
            TRequest request,
            RequestHandlerDelegate<TResponse> next,
            CancellationToken cancellationToken)
        {
            if (request is GetShippingAuthoringDraftTypeAssignmentQuery or ReplaceShippingAuthoringDraftTypeAssignmentCommand)
            {
                counter.Sent++;
            }

            return await next();
        }
    }

    /// <summary>Controlled, labelled Fulfillment catalog facts; every read is counted.</summary>
    private sealed class FakeCatalog : IShippingTypeCatalogReader
    {
        private readonly List<ShippingTypeFact> _facts = [];

        public int Reads { get; set; }

        public Action? FailWith { get; set; }

        public Guid Add(string code, bool isActive, int sortOrder, long revision, params (Guid LanguageId, string Name, string? Description)[] translations)
        {
            var id = Guid.NewGuid();
            _facts.Add(new ShippingTypeFact(
                id,
                code,
                isActive,
                sortOrder,
                revision,
                translations.Select(item => new ShippingTypeFactTranslation(item.LanguageId, item.Name, item.Description)).ToArray()));
            return id;
        }

        public void SetActive(Guid id, bool isActive)
        {
            var index = _facts.FindIndex(fact => fact.ShippingTypeId == id);
            _facts[index] = _facts[index] with { IsActive = isActive };
        }

        public void Remove(Guid id) => _facts.RemoveAll(fact => fact.ShippingTypeId == id);

        public Task<IReadOnlyList<ShippingTypeFact>> ListActiveAsync(CancellationToken cancellationToken)
        {
            Reads++;
            FailWith?.Invoke();
            return Task.FromResult<IReadOnlyList<ShippingTypeFact>>(
                _facts.Where(fact => fact.IsActive).OrderBy(fact => fact.SortOrder).ThenBy(fact => fact.ShippingTypeId).ToArray());
        }

        public Task<IReadOnlyList<ShippingTypeFact>> GetByIdsAsync(
            IReadOnlyCollection<Guid> shippingTypeIds,
            CancellationToken cancellationToken)
        {
            Reads++;
            FailWith?.Invoke();
            return Task.FromResult<IReadOnlyList<ShippingTypeFact>>(
                _facts.Where(fact => shippingTypeIds.Contains(fact.ShippingTypeId)).ToArray());
        }
    }

    private sealed class RecordingAdminAuthorizer(IReadOnlyList<string> allowed) : IShippingAdminAuthorizer
    {
        public Task<Guid> RequireAuthorizedAsync(
            HttpContext context,
            string permission,
            CancellationToken cancellationToken)
            => allowed.Contains(permission, StringComparer.Ordinal)
                ? Task.FromResult(Iran)
                : Task.FromException<Guid>(
                    new SemanticException(new SemanticError(FoundationErrorCodes.AdminAuthorizationDenied)));
    }

    private sealed class RecordingSellerAccess : ISellerPanelAccess
    {
        public Action? OnCall { get; set; }

        public Task<(Guid ActorUserId, Guid SellerPartyId)> RequireAuthorizedAsync(
            HttpRequest request,
            CancellationToken cancellationToken)
        {
            OnCall?.Invoke();
            return Task.FromResult((Iran, Tehran));
        }
    }

    private sealed class RecordingEffectiveAccess(IReadOnlyList<PlatformPermissionGrant> grants)
        : IPlatformEffectiveAccessReader
    {
        public Task<IReadOnlyList<PlatformPermissionGrant>> GetEffectivePermissionsAsync(
            Guid userId,
            PlatformAccessOwnerKind ownerKind,
            Guid? ownerScopeId,
            CancellationToken cancellationToken)
            => Task.FromResult(grants);
    }

    private sealed class FakeContextSource(ToobaEdition edition, string? currency) : IShippingAuthoringContextSource
    {
        public Task<ShippingAuthoringContextSourceSnapshot> ReadAsync(CancellationToken cancellationToken)
            => Task.FromResult(new ShippingAuthoringContextSourceSnapshot(
                edition,
                currency,
                [new ShippingAuthoringLanguageSource(PersianLanguage, "fa", "fa-IR", "fa", true, true, "rtl", "فارسی", "فارسی", 0)]));
    }

    private sealed class FakeMethodValidator : IShippingMethodOptionValidator
    {
        public Task<Tooba.BuildingBlocks.Results.Result<bool>> ValidateAsync(
            string serviceCode,
            string? optionCode,
            CancellationToken cancellationToken)
            => Task.FromResult(Tooba.BuildingBlocks.Results.Result<bool>.Success(true));
    }

    private sealed class FakeResolver : IShippingAuthoringDestinationResolver
    {
        public Task<Tooba.BuildingBlocks.Results.Result<IReadOnlyList<ShippingDestinationSelectionPath>>> ResolveAsync(
            IReadOnlyList<ShippingAuthoringSelectedAreaWrite> selections,
            CancellationToken cancellationToken)
            => Task.FromResult(Tooba.BuildingBlocks.Results.Result<IReadOnlyList<ShippingDestinationSelectionPath>>.Success(
                selections.Select(selection => ShippingDestinationSelectionPath.FromChain(
                    [
                        new ShippingDestinationNode(ShippingDestinationNodeKind.Country, selection.CountryId),
                        new ShippingDestinationNode(
                            ShippingDestinationNodeKind.AdministrativeArea,
                            selection.AdministrativeAreaId!.Value),
                    ])).ToArray()));
    }

    private sealed class StubCurrentEdition(EditionContext? current) : ICurrentEdition
    {
        public EditionContext? Current { get; } = current;
    }

    private sealed class StubCurrentTenant(TenantContext? current) : ICurrentTenant
    {
        public TenantContext? Current { get; set; } = current;
    }

    private sealed class StubHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Production;
        public string ApplicationName { get; set; } = "Tooba.Shipping.Tests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } =
            new Microsoft.Extensions.FileProviders.NullFileProvider();
    }
}
