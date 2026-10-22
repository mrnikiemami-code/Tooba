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
using Tooba.BuildingBlocks.Presentation;
using Tooba.BuildingBlocks.Presentation.Errors;
using Tooba.BuildingBlocks.Results;
using Tooba.BuildingBlocks.Security;
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
/// TB-SHIPPING-ADMIN-METHOD-MODE-DRAFTS-194 — two independent method-root draft selections (freight
/// payment timing and calculation mode), executed through the REAL HTTP delegates.
/// </summary>
/// <remarks>
/// <para>
/// Every fact composes the production entry (<see cref="ShippingEndpointModule"/>), the real
/// <see cref="ShippingAuthoringOwnerAuthorizer"/>, the real body reader, the real MediatR validation
/// pipeline, the real method-settings query/command handlers and the real
/// <see cref="ShippingAuthoringDraftDirectory"/> over the real <see cref="ShippingDbContext"/>. Only the
/// platform authorization edges, the destination/option seams and the commerce context source are
/// controlled fixtures.
/// </para>
/// <para>
/// <b>Dispatch vs execution.</b> <see cref="Harness.SentCount"/> counts real <c>ISender</c> dispatch and
/// <see cref="Harness.HandlerReached"/> counts arrival past validation, for the method-settings requests
/// only. A denied request asserts both are zero; a rejected payload may dispatch but must never reach a
/// handler.
/// </para>
/// </remarks>
[Collection(nameof(Tooba.Shipping.Tests.Observability.ShippingModuleCallTracingCollection))]
public sealed class ShippingAuthoringMethodSettingsEndpointTests
{
    private const string AdminDrafts = "/v1/admin/shipping-services/authoring-drafts";
    private const string SellerDrafts = "/v1/seller/shipping-services/authoring-drafts";
    private static readonly Guid Iran = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid Tehran = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid Isfahan = Guid.Parse("44444444-4444-4444-4444-444444444444");
    private static readonly Guid PersianLanguage = Guid.Parse("33333333-3333-3333-3333-333333333333");

    // Wire values: freight 0=Prepaid, 1=CollectOnDelivery; calculation 0=Manual, 1=CarrierApi, 2=Free.
    private const int Prepaid = 0;
    private const int CollectOnDelivery = 1;
    private const int Manual = 0;
    private const int CarrierApi = 1;
    private const int Free = 2;

    [Fact]
    public async Task Unconfigured_draft_has_no_defaults_and_all_six_combinations_roundtrip_through_real_admin_routes()
    {
        using var harness = Harness.Create("mode-http-a");
        var draftId = await harness.CreateDraftAsync(Tehran);
        var path = $"{AdminDrafts}/{draftId}/method-settings";

        // Absent settings are explicitly UNCONFIGURED: no Manual/Prepaid default is invented.
        var (initialStatus, _, initial) = await harness.SendAsync(HttpMethod.Get, path);
        Assert.Equal(StatusCodes.Status200OK, initialStatus);
        Assert.False(initial.GetProperty("isConfigured").GetBoolean());
        Assert.Equal(JsonValueKind.Null, initial.GetProperty("freightPaymentMode").ValueKind);
        Assert.Equal(JsonValueKind.Null, initial.GetProperty("calculationMode").ValueKind);
        Assert.Equal(1, initial.GetProperty("revision").GetInt64());

        var combinations = new (int Freight, int Calculation)[]
        {
            (Prepaid, Manual),
            (Prepaid, CarrierApi),
            (Prepaid, Free),
            (CollectOnDelivery, Manual),
            (CollectOnDelivery, CarrierApi),
            (CollectOnDelivery, Free),
        };

        // All six combinations are legal INDEPENDENT choices: neither axis constrains the other.
        for (var index = 0; index < combinations.Length; index++)
        {
            var (freight, calculation) = combinations[index];
            var (status, _, body) = await harness.SendAsync(
                HttpMethod.Put,
                path,
                Settings(index + 1, freight, calculation));
            Assert.True(status == StatusCodes.Status200OK, body.ToString());
            Assert.True(body.GetProperty("isConfigured").GetBoolean());
            Assert.Equal(freight, body.GetProperty("freightPaymentMode").GetInt32());
            Assert.Equal(calculation, body.GetProperty("calculationMode").GetInt32());
            Assert.Equal(index + 2, body.GetProperty("revision").GetInt64());

            var (_, _, read) = await harness.SendAsync(HttpMethod.Get, path);
            Assert.Equal(freight, read.GetProperty("freightPaymentMode").GetInt32());
            Assert.Equal(calculation, read.GetProperty("calculationMode").GetInt32());
            Assert.Equal(index + 2, read.GetProperty("revision").GetInt64());
        }

        // An identical PUT is a truthful no-op: revision and timestamp are preserved.
        var stampBefore = (await harness.ReadDraftStampAsync(draftId)).Revision;
        var (noOpStatus, _, noOp) = await harness.SendAsync(
            HttpMethod.Put,
            path,
            Settings(7, CollectOnDelivery, Free));
        Assert.Equal(StatusCodes.Status200OK, noOpStatus);
        Assert.Equal(7, noOp.GetProperty("revision").GetInt64());
        var stampAfter = (await harness.ReadDraftStampAsync(draftId)).Revision;
        Assert.Equal(stampBefore, stampAfter);

        // Switching only ONE axis changes only that axis and advances the SAME root revision once.
        var (switchStatus, _, switched) = await harness.SendAsync(
            HttpMethod.Put,
            path,
            Settings(7, Prepaid, Free));
        Assert.Equal(StatusCodes.Status200OK, switchStatus);
        Assert.Equal(8, switched.GetProperty("revision").GetInt64());
        Assert.Equal(Prepaid, switched.GetProperty("freightPaymentMode").GetInt32());
        Assert.Equal(Free, switched.GetProperty("calculationMode").GetInt32());

        // A stale expected revision is the canonical conflict even for an otherwise identical payload.
        var (staleStatus, _, stale) = await harness.SendAsync(
            HttpMethod.Put,
            path,
            Settings(7, Prepaid, Free));
        Assert.Equal(StatusCodes.Status409Conflict, staleStatus);
        Assert.Equal(
            ShippingErrorCodes.AuthoringDraftRevisionConflict,
            stale.GetProperty("errorCode").GetString());
        var unchanged = (await harness.ReadDraftStampAsync(draftId)).Revision;
        Assert.Equal(8, unchanged);
    }

    [Fact]
    public async Task Absent_null_or_unknown_selections_are_rejected_before_any_mutation()
    {
        using var harness = Harness.Create("mode-http-b");
        var draftId = await harness.CreateDraftAsync(Tehran);
        var path = $"{AdminDrafts}/{draftId}/method-settings";
        var before = (await harness.ReadDraftStampAsync(draftId)).Revision;

        var invalid = new (string Json, string ExpectedCode)[]
        {
            // A missing working selection must never deserialize into the enum zero (Prepaid/Manual).
            ("{\"expectedRevision\":1,\"calculationMode\":0}", "shipping.validation.method_settings_freight_mode_invalid"),
            ("{\"expectedRevision\":1,\"freightPaymentMode\":null,\"calculationMode\":0}", "shipping.validation.method_settings_freight_mode_invalid"),
            ("{\"expectedRevision\":1,\"freightPaymentMode\":0}", "shipping.validation.method_settings_calculation_mode_invalid"),
            ("{\"expectedRevision\":1,\"freightPaymentMode\":0,\"calculationMode\":null}", "shipping.validation.method_settings_calculation_mode_invalid"),
            // Unknown numeric values are rejected, never coerced to a defined enum member.
            ("{\"expectedRevision\":1,\"freightPaymentMode\":7,\"calculationMode\":0}", "shipping.validation.method_settings_freight_mode_invalid"),
            ("{\"expectedRevision\":1,\"freightPaymentMode\":0,\"calculationMode\":9}", "shipping.validation.method_settings_calculation_mode_invalid"),
            // A non-positive revision is a shape failure like every other authoring write.
            ("{\"expectedRevision\":0,\"freightPaymentMode\":1,\"calculationMode\":2}", "shipping.validation.expected_revision_invalid"),
        };

        foreach (var (json, expectedCode) in invalid)
        {
            var (status, _, body) = await harness.SendAsync(HttpMethod.Put, path, json);
            Assert.Equal(StatusCodes.Status400BadRequest, status);
            Assert.Equal("validation.failed", body.GetProperty("errorCode").GetString());
            Assert.Contains(expectedCode, body.GetRawText(), StringComparison.Ordinal);
        }

        // Validation rejected every payload: nothing reached a handler and nothing was written.
        Assert.True(harness.SentCount > 0);
        Assert.Equal(0, harness.HandlerReached);
        var after = (await harness.ReadDraftStampAsync(draftId)).Revision;
        Assert.Equal(before, after);
        var (_, _, stillUnconfigured) = await harness.SendAsync(HttpMethod.Get, path);
        Assert.False(stillUnconfigured.GetProperty("isConfigured").GetBoolean());

        // Malformed JSON follows the canonical body-reader behaviour: never a handler.
        var (malformedStatus, _, malformed) = await harness.SendAsync(HttpMethod.Put, path, "{\"expectedRevision\":");
        Assert.Equal(StatusCodes.Status400BadRequest, malformedStatus);
        Assert.False(string.IsNullOrEmpty(malformed.GetProperty("errorCode").GetString()));

        // A rejected payload in the SAME context leaves no pending edit a later SaveChanges could publish.
        await harness.WithContextAsync(async context =>
        {
            var directory = new ShippingAuthoringDraftDirectory(context);
            var failure = await Assert.ThrowsAsync<SemanticException>(() => directory.ReplaceMethodSettingsAsync(
                ShippingAuthoringOwnerScope.ForStore("store-a"),
                draftId,
                1,
                null,
                ShippingAuthoringCalculationMode.Free,
                CancellationToken.None));
            Assert.Equal(ShippingErrorCodes.AuthoringMethodSettingsInvalid, failure.Error.Code);
            Assert.Empty(context.ChangeTracker.Entries().Where(entry => entry.State != EntityState.Unchanged));
        });
    }

    [Fact]
    public async Task Mode_changes_preserve_geography_rates_currency_text_and_schedule_in_both_directions()
    {
        using var harness = Harness.Create("mode-http-c");
        var draftId = await harness.CreateDraftAsync(Tehran);
        var groupId = await harness.WithContextAsync(async context => await context.AuthoringDrafts
            .AsNoTracking()
            .Where(draft => draft.DraftId == draftId)
            .SelectMany(draft => draft.RegionGroups)
            .Select(group => group.GroupId)
            .SingleAsync());

        // Configure the other capabilities first.
        var (rateStatus, _, rateBody) = await harness.SendAsync(
            HttpMethod.Put,
            $"{AdminDrafts}/{draftId}/region-groups/{groupId}/rate-rows",
            "{\"expectedRevision\":1,\"rows\":[{\"rateRowId\":null,\"minWeightGrams\":0,\"maxWeightGrams\":5000,"
            + "\"minItemCount\":null,\"maxItemCount\":null,\"minMerchandiseAmount\":null,"
            + "\"maxMerchandiseAmount\":null,\"freightAmount\":12.50,\"packagingAmount\":3.00,"
            + "\"minDeliveryDays\":1,\"maxDeliveryDays\":3}]}");
        Assert.True(rateStatus == StatusCodes.Status200OK, rateBody.ToString());

        var (textStatus, _, textBody) = await harness.SendAsync(
            HttpMethod.Patch,
            $"{AdminDrafts}/{draftId}/presentation",
            $"{{\"expectedRevision\":2,\"methodTranslations\":[{{\"languageId\":\"{PersianLanguage}\","
            + "\"name\":\"پست پیشتاز\",\"description\":\"ارسال سریع\"}],\"groupTitles\":[]}");
        Assert.True(textStatus == StatusCodes.Status200OK, textBody.ToString());

        var (scheduleStatus, _, scheduleBody) = await harness.SendAsync(
            HttpMethod.Put,
            $"{AdminDrafts}/{draftId}/weekly-schedule",
            Week(3, "Asia/Tehran"));
        Assert.True(scheduleStatus == StatusCodes.Status200OK, scheduleBody.ToString());

        // Now select Free shipping timing: every stored row/text/currency/geography/schedule survives.
        var (status, _, body) = await harness.SendAsync(
            HttpMethod.Put,
            $"{AdminDrafts}/{draftId}/method-settings",
            Settings(4, CollectOnDelivery, Free));
        Assert.True(status == StatusCodes.Status200OK, body.ToString());
        Assert.Equal(CollectOnDelivery, body.GetProperty("freightPaymentMode").GetInt32());
        Assert.Equal(Free, body.GetProperty("calculationMode").GetInt32());

        var snapshot = await harness.ReadSnapshotAsync(draftId);
        Assert.Equal("USD", snapshot.Currency);
        Assert.Equal(1, snapshot.RateRowCount);
        Assert.Equal(12.50m, snapshot.FreightAmount);
        Assert.Equal(3.00m, snapshot.PackagingAmount);
        Assert.Equal(Tehran, snapshot.AreaId);
        Assert.Equal("پست پیشتاز", snapshot.MethodName);
        Assert.Equal("Asia/Tehran", snapshot.TimeZoneId);
        Assert.Equal(7, snapshot.DayCount);
        Assert.Equal(CollectOnDelivery, (int)snapshot.Freight!);
        Assert.Equal(Free, (int)snapshot.Calculation!);

        // A subsequent EXISTING-capability write preserves the settings row in return.
        var (reorderStatus, _, reorderBody) = await harness.SendAsync(
            HttpMethod.Put,
            $"{AdminDrafts}/{draftId}",
            $"{{\"expectedRevision\":5,\"regionGroups\":[{{\"groupId\":\"{groupId}\",\"areas\":["
            + $"{{\"kind\":1,\"countryId\":\"{Iran}\",\"administrativeAreaId\":\"{Tehran}\"}},"
            + $"{{\"kind\":2,\"countryId\":\"{Iran}\",\"administrativeAreaId\":\"{Iran}\",\"cityId\":\"{Isfahan}\"}}]}}]}}");
        Assert.True(reorderStatus == StatusCodes.Status200OK, reorderBody.ToString());

        var (_, _, settingsAfter) = await harness.SendAsync(
            HttpMethod.Get,
            $"{AdminDrafts}/{draftId}/method-settings");
        Assert.True(settingsAfter.GetProperty("isConfigured").GetBoolean());
        Assert.Equal(CollectOnDelivery, settingsAfter.GetProperty("freightPaymentMode").GetInt32());
        Assert.Equal(Free, settingsAfter.GetProperty("calculationMode").GetInt32());
        Assert.Equal(6, settingsAfter.GetProperty("revision").GetInt64());

        // Free selection did not delete or zero the stored manual rows.
        var after = await harness.ReadSnapshotAsync(draftId);
        Assert.Equal(1, after.RateRowCount);
        Assert.Equal(12.50m, after.FreightAmount);
    }

    [Fact]
    public async Task Pre_dispatch_denials_foreign_owner_and_single_store_seller_paths_are_safe()
    {
        var draftIdText = Guid.NewGuid();

        // View-only admin: PUT is denied BEFORE dispatch and nothing is written.
        using (var viewOnly = Harness.Create("mode-http-d", adminAllowed: [ShippingAdminPermissions.View]))
        {
            var (status, _, body) = await viewOnly.SendAsync(
                HttpMethod.Put,
                $"{AdminDrafts}/{draftIdText}/method-settings",
                Settings(1, Prepaid, Manual));
            Assert.Equal(StatusCodes.Status403Forbidden, status);
            Assert.Equal(0, viewOnly.SentCount);
            Assert.Equal(0, viewOnly.HandlerReached);
            Assert.NotEqual(JsonValueKind.Null, body.ValueKind);
        }

        using var harness = Harness.Create("mode-http-e");
        var draftId = await harness.CreateDraftAsync(Tehran);
        var path = $"{AdminDrafts}/{draftId}/method-settings";

        // A foreign owner gets the same safe not-found and cannot change the row.
        var (foreignStatus, _, foreign) = await harness.SendAsync(
            HttpMethod.Put,
            path,
            Settings(1, CollectOnDelivery, Free),
            tenantId: "store-b");
        Assert.Equal(StatusCodes.Status404NotFound, foreignStatus);
        Assert.Equal(ShippingErrorCodes.AuthoringDraftNotFound, foreign.GetProperty("errorCode").GetString());
        var stamp = (await harness.ReadDraftStampAsync(draftId)).Revision;
        Assert.Equal(1, stamp);

        // Marketplace seller: granted permission succeeds, missing permission is denied pre-dispatch.
        using (var marketplace = Harness.Create(
                   "mode-http-f",
                   edition: ToobaEdition.Marketplace,
                   marketplace: true,
                   tenantId: null,
                   sellerGrants: [
                       new PlatformPermissionGrant(ShippingAdminPermissions.View, PlatformAccessScopeKind.GlobalWithinOwner, null, false),
                       new PlatformPermissionGrant(ShippingAdminPermissions.Manage, PlatformAccessScopeKind.GlobalWithinOwner, null, false),
                   ]))
        {
            var sellerDraft = await marketplace.CreateSellerDraftAsync(Tehran);
            var (okStatus, _, okBody) = await marketplace.SendAsync(
                HttpMethod.Put,
                $"{SellerDrafts}/{sellerDraft}/method-settings",
                Settings(1, Prepaid, CarrierApi));
            Assert.True(okStatus == StatusCodes.Status200OK, okBody.ToString());
            Assert.Equal(CarrierApi, okBody.GetProperty("calculationMode").GetInt32());
        }

        using (var deniedSeller = Harness.Create(
                   "mode-http-g",
                   edition: ToobaEdition.Marketplace,
                   marketplace: true,
                   tenantId: null,
                   sellerGrants: [new PlatformPermissionGrant(
                       ShippingAdminPermissions.Manage,
                       PlatformAccessScopeKind.Category,
                       Guid.NewGuid(),
                       false)]))
        {
            // A scoped seller grant is refused BEFORE draft resolution or dispatch.
            var (deniedStatus, _, deniedBody) = await deniedSeller.SendAsync(
                HttpMethod.Put,
                $"{SellerDrafts}/{Guid.NewGuid()}/method-settings",
                Settings(1, Prepaid, CarrierApi));
            Assert.Equal(StatusCodes.Status403Forbidden, deniedStatus);
            Assert.Equal(ShippingErrorCodes.AuthoringOwnerScopeDenied, deniedBody.GetProperty("errorCode").GetString());
            Assert.Equal(0, deniedSeller.SentCount);
            Assert.Equal(0, deniedSeller.HandlerReached);
        }

        // SingleStore seller routes stay canonically unavailable before any seller resolution.
        using var singleStore = Harness.Create("mode-http-h");
        var adminDraft = await singleStore.CreateDraftAsync(Tehran);
        var (unavailable, _, unavailableBody) = await singleStore.SendAsync(
            HttpMethod.Get,
            $"{SellerDrafts}/{adminDraft}/method-settings");
        Assert.Equal(StatusCodes.Status404NotFound, unavailable);
        Assert.Equal(
            ShippingErrorCodes.AuthoringOwnerScopeUnavailable,
            unavailableBody.GetProperty("errorCode").GetString());
        Assert.Equal(0, singleStore.SellerCalls);
    }

    private static string Settings(long revision, int freight, int calculation)
        => $"{{\"expectedRevision\":{revision},\"freightPaymentMode\":{freight},\"calculationMode\":{calculation}}}";

    private static string Week(long revision, string timeZoneId)
    {
        var days = Enumerable.Range(1, 7).Select(weekday => weekday == 6
            ? "{\"weekday\":6,\"isWorking\":true,\"startTime\":\"09:00:00\",\"endTime\":\"13:00:00\"}"
            : $"{{\"weekday\":{weekday},\"isWorking\":false}}");
        return $"{{\"expectedRevision\":{revision},\"timeZoneId\":\"{timeZoneId}\",\"days\":["
               + string.Join(',', days) + "]}";
    }

    private sealed class Harness : IDisposable
    {
        private readonly WebApplication _app;
        private readonly IServiceScope _scope;
        private readonly Counter _counter;

        private Harness(WebApplication app, IServiceScope scope, Counter counter, int sellerCalls)
        {
            _app = app;
            _scope = scope;
            _counter = counter;
            SellerCalls = sellerCalls;
        }

        public string AcceptLanguage { get; set; } = "fa";

        public TenantContext? DefaultTenant { get; set; }

        public int SellerCalls { get; private set; }

        public int SentCount => _counter.Sent;

        public int HandlerReached => _counter.Handler;

        public static Harness Create(
            string database,
            ToobaEdition edition = ToobaEdition.SingleStore,
            IReadOnlyList<string>? adminAllowed = null,
            IReadOnlyList<PlatformPermissionGrant>? sellerGrants = null,
            bool marketplace = false,
            string? tenantId = "store-a")
        {
            var counter = new Counter();
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
            builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(HandlerReachedBehavior<,>));
            builder.Services.AddProblemDetails();
            builder.Services.AddSingleton<IExceptionPresentationService, ExceptionPresentationService>();
            builder.Services.AddDbContext<ShippingDbContext>(options => options.UseInMemoryDatabase(database));
            builder.Services.AddScoped<IShippingAuthoringDraftDirectory, ShippingAuthoringDraftDirectory>();
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
            var harness = new Harness(app, scope, counter, 0) { DefaultTenant = marketplace ? null : Tenant(tenantId ?? "store-a") };
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

        public async Task<Guid> CreateDraftAsync(params Guid[] areas)
        {
            var (status, _, body) = await SendAsync(HttpMethod.Post, AdminDrafts, CreateBody(areas));
            Assert.Equal(StatusCodes.Status201Created, status);
            return body.GetProperty("draftId").GetGuid();
        }

        public async Task<Guid> CreateSellerDraftAsync(params Guid[] areas)
        {
            var (status, _, body) = await SendAsync(HttpMethod.Post, SellerDrafts, CreateBody(areas));
            Assert.Equal(StatusCodes.Status201Created, status);
            return body.GetProperty("draftId").GetGuid();
        }

        public async Task<(long Revision, DateTimeOffset UpdatedAt)> ReadDraftStampAsync(Guid draftId)
            => await WithContextAsync(async context =>
            {
                var draft = await context.AuthoringDrafts.AsNoTracking().SingleAsync(item => item.DraftId == draftId);
                return (draft.Revision, draft.UpdatedAt);
            });

        public async Task<MethodSettingsSnapshot> ReadSnapshotAsync(Guid draftId)
            => await WithContextAsync(async context =>
            {
                var draft = await context.AuthoringDrafts
                    .AsNoTracking()
                    .Include(item => item.RegionGroups)
                    .ThenInclude(group => group.Areas)
                    .Include(item => item.RegionGroups)
                    .ThenInclude(group => group.RateRows)
                    .Include(item => item.RegionGroups)
                    .ThenInclude(group => group.Titles)
                    .Include(item => item.Translations)
                    .Include(item => item.WeeklySchedule)
                    .ThenInclude(schedule => schedule!.Days)
                    .Include(item => item.MethodSettings)
                    .SingleAsync(item => item.DraftId == draftId);

                var group = draft.RegionGroups.OrderBy(item => item.GroupId).First();
                var row = group.RateRows.OrderBy(item => item.RateRowId).First();
                var translation = draft.Translations.OrderBy(item => item.LanguageId).First();
                return new MethodSettingsSnapshot(
                    group.CurrencyCode,
                    group.RateRows.Count,
                    row.FreightAmount,
                    row.PackagingAmount,
                    group.Areas.OrderBy(area => area.NodeId).First().NodeId,
                    translation.Name,
                    draft.WeeklySchedule?.TimeZoneId,
                    draft.WeeklySchedule?.Days.Count ?? 0,
                    draft.MethodSettings?.FreightPaymentMode,
                    draft.MethodSettings?.CalculationMode);
            });

        public async Task WithContextAsync(Func<ShippingDbContext, Task> action)
            => await action(_scope.ServiceProvider.GetRequiredService<ShippingDbContext>());

        public async Task<T> WithContextAsync<T>(Func<ShippingDbContext, Task<T>> action)
            => await action(_scope.ServiceProvider.GetRequiredService<ShippingDbContext>());

        public async Task<(int Status, string? Location, JsonElement Body)> SendAsync(
            HttpMethod method,
            string path,
            string? json = null,
            string? tenantId = null)
        {
            // The owner scope is resolved from the trusted per-request tenant: a caller can only switch
            // it in this test fixture to model a DIFFERENT store, never through the HTTP payload.
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

        private static string CreateBody(IEnumerable<Guid> areas)
            => string.Concat(
                "{\"serviceCode\":\"post\",\"optionCode\":null,\"regionGroups\":[",
                string.Join(',', areas.Select(area =>
                    $"{{\"groupId\":null,\"areas\":[{{\"kind\":1,\"countryId\":\"{Iran}\",\"administrativeAreaId\":\"{area}\"}}]}}")),
                "]}");

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

    private sealed record MethodSettingsSnapshot(
        string? Currency,
        int RateRowCount,
        decimal FreightAmount,
        decimal PackagingAmount,
        Guid AreaId,
        string MethodName,
        string? TimeZoneId,
        int DayCount,
        Tooba.Shipping.Contracts.Packaging.ShippingFreightPayerMode? Freight,
        ShippingAuthoringCalculationMode? Calculation);

    private sealed class Counter
    {
        public int Sent { get; set; }

        public int Handler { get; set; }
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
            if (IsMethodSettings(request))
            {
                counter.Sent++;
            }

            return await next();
        }
    }

    private sealed class HandlerReachedBehavior<TRequest, TResponse>(Counter counter)
        : IPipelineBehavior<TRequest, TResponse>
        where TRequest : notnull
    {
        public async Task<TResponse> Handle(
            TRequest request,
            RequestHandlerDelegate<TResponse> next,
            CancellationToken cancellationToken)
        {
            if (IsMethodSettings(request))
            {
                counter.Handler++;
            }

            return await next();
        }
    }

    private static bool IsMethodSettings(object request) =>
        request is GetShippingAuthoringMethodSettingsQuery or ReplaceShippingAuthoringMethodSettingsCommand;

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
        public Task<Result<bool>> ValidateAsync(
            string serviceCode,
            string? optionCode,
            CancellationToken cancellationToken)
            => Task.FromResult(string.Equals(serviceCode, "post", StringComparison.Ordinal)
                ? Result<bool>.Success(true)
                : Result<bool>.Failure(new SemanticError(ShippingErrorCodes.ServiceOptionUnknown)));
    }

    private sealed class FakeResolver : IShippingAuthoringDestinationResolver
    {
        public Task<Result<IReadOnlyList<ShippingDestinationSelectionPath>>> ResolveAsync(
            IReadOnlyList<ShippingAuthoringSelectedAreaWrite> selections,
            CancellationToken cancellationToken)
            => Task.FromResult(Result<IReadOnlyList<ShippingDestinationSelectionPath>>.Success(
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
