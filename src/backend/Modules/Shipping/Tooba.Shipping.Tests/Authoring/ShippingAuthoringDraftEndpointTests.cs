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
using Tooba.Shipping.Application.Authoring.Validation;
using Tooba.Shipping.Application.Rates.Ports;
using Tooba.Shipping.Contracts.Authoring;
using Tooba.Shipping.Contracts.Errors;
using Tooba.Shipping.Domain.Authoring;
using Tooba.Shipping.Endpoints;
using Tooba.Shipping.Endpoints.Admin;
using Tooba.Shipping.Infrastructure.Authoring;
using Tooba.Shipping.Infrastructure.Persistence;
using Xunit;

namespace Tooba.Shipping.Tests.Authoring;

/// <summary>
/// TB-SHIPPING-ADMIN-OWNER-REGION-DRAFTS-190 — the real authorized draft HTTP surface.
/// </summary>
/// <remarks>
/// The routes come from the production composition entry (<see cref="ShippingEndpointModule"/>), and each
/// request executes the real endpoint delegate, the real owner authorizer, the real MediatR pipeline, the
/// real handlers, the real <see cref="ShippingAuthoringDraftDirectory"/> over the real
/// <see cref="ShippingDbContext"/> and the real response factory. Only platform seams and the two foreign
/// read ports are fakes. EF InMemory is used, so this file proves HTTP ownership/permission wiring and the
/// owner-filtered store roundtrip — not relational concurrency or unique-index behavior.
/// </remarks>
[Collection(nameof(Tooba.Shipping.Tests.Observability.ShippingModuleCallTracingCollection))]
public sealed class ShippingAuthoringDraftEndpointTests
{
    private const string AdminDrafts = "/v1/admin/shipping-services/authoring-drafts";
    private const string SellerDrafts = "/v1/seller/shipping-services/authoring-drafts";
    private static readonly Guid Iran = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid Tehran = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid Isfahan = Guid.Parse("44444444-4444-4444-4444-444444444444");

    [Fact]
    public async Task Admin_create_returns_a_created_location_and_another_store_cannot_see_or_change_it()
    {
        using var harness = Harness.Create("draft-http-a");

        var (status, location, body) = await harness.SendAsync(
            HttpMethod.Post,
            AdminDrafts,
            CreateBody(Tehran));

        Assert.Equal(StatusCodes.Status201Created, status);
        var draftId = body.GetProperty("draftId").GetGuid();
        Assert.Equal($"{AdminDrafts}/{draftId}", location);
        Assert.Equal(1, body.GetProperty("revision").GetInt64());
        Assert.Equal("Store", body.GetProperty("owner").GetProperty("ownerKind").GetString());

        // Store A reads its own draft through the real handler/store.
        var (ownStatus, _, own) = await harness.SendAsync(HttpMethod.Get, $"{AdminDrafts}/{draftId}");
        Assert.Equal(StatusCodes.Status200OK, ownStatus);
        Assert.Single(own.GetProperty("regionGroups").EnumerateArray());

        // Store B resolves neither the draft nor a group of another owner: same safe 404, no change.
        harness.TenantContext = Harness.Tenant("store-b");
        var (foreignRead, _, foreignReadBody) = await harness.SendAsync(HttpMethod.Get, $"{AdminDrafts}/{draftId}");
        Assert.Equal(StatusCodes.Status404NotFound, foreignRead);
        Assert.Equal(
            ShippingErrorCodes.AuthoringDraftNotFound,
            foreignReadBody.GetProperty("errorCode").GetString());

        var (foreignWrite, _, foreignWriteBody) = await harness.SendAsync(
            HttpMethod.Put,
            $"{AdminDrafts}/{draftId}",
            ReplaceBody(1, Isfahan));
        Assert.Equal(StatusCodes.Status404NotFound, foreignWrite);
        Assert.Equal(
            ShippingErrorCodes.AuthoringDraftNotFound,
            foreignWriteBody.GetProperty("errorCode").GetString());

        // Nothing was created for Store B and Store A's tree/revision are untouched.
        harness.TenantContext = Harness.Tenant("store-a");
        var (_, _, list) = await harness.SendAsync(HttpMethod.Get, AdminDrafts);
        Assert.Single(list.GetProperty("drafts").EnumerateArray());
        var (_, _, after) = await harness.SendAsync(HttpMethod.Get, $"{AdminDrafts}/{draftId}");
        Assert.Equal(1, after.GetProperty("revision").GetInt64());
        Assert.Single(after.GetProperty("regionGroups").EnumerateArray());
    }

    [Fact]
    public async Task A_view_only_permission_cannot_create_and_is_denied_before_dispatch()
    {
        using var harness = Harness.Create("draft-http-b", allowed: [ShippingAdminPermissions.View]);

        var (status, _, body) = await harness.SendAsync(HttpMethod.Post, AdminDrafts, CreateBody(Tehran));

        Assert.Equal(StatusCodes.Status403Forbidden, status);
        Assert.Equal(FoundationErrorCodes.AdminAuthorizationDenied, body.GetProperty("errorCode").GetString());
        Assert.False(harness.DispatchedHasDraftWrite);

        // The same view permission still reads, and no draft was persisted by the denied write.
        var (readStatus, _, list) = await harness.SendAsync(HttpMethod.Get, AdminDrafts);
        Assert.Equal(StatusCodes.Status200OK, readStatus);
        Assert.Empty(list.GetProperty("drafts").EnumerateArray());
    }

    [Fact]
    public async Task Seller_routes_in_a_single_store_installation_are_unavailable_and_write_nothing()
    {
        using var harness = Harness.Create("draft-http-c");

        var (status, _, body) = await harness.SendAsync(HttpMethod.Post, SellerDrafts, CreateBody(Tehran));

        Assert.Equal(StatusCodes.Status404NotFound, status);
        Assert.Equal(
            ShippingErrorCodes.AuthoringOwnerScopeUnavailable,
            body.GetProperty("errorCode").GetString());
        Assert.Equal(0, harness.SellerCalls);

        var (_, _, list) = await harness.SendAsync(HttpMethod.Get, AdminDrafts);
        Assert.Empty(list.GetProperty("drafts").EnumerateArray());
    }

    [Fact]
    public async Task A_missing_region_group_collection_is_rejected_before_dispatch_and_persists_nothing()
    {
        using var harness = Harness.Create("draft-http-d");

        // R190-01: the property is OMITTED — a non-nullable C# annotation is not a JSON contract.
        var (omittedStatus, _, omitted) = await harness.SendAsync(
            HttpMethod.Post,
            AdminDrafts,
            "{\"serviceCode\":\"post\",\"optionCode\":null}");
        Assert.Equal(StatusCodes.Status400BadRequest, omittedStatus);
        Assert.Equal("validation.failed", omitted.GetProperty("errorCode").GetString());
        Assert.Contains(
            omitted.GetProperty("errors").EnumerateObject().SelectMany(property => property.Value.EnumerateArray()),
            code => code.GetString() == ShippingAuthoringValidationCodes.RegionGroupsRequired);

        // The property is present but explicitly null: same required failure, never "empty draft".
        var (nullStatus, _, nullBody) = await harness.SendAsync(
            HttpMethod.Post,
            AdminDrafts,
            "{\"serviceCode\":\"post\",\"optionCode\":null,\"regionGroups\":null}");
        Assert.Equal(StatusCodes.Status400BadRequest, nullStatus);
        Assert.Equal("validation.failed", nullBody.GetProperty("errorCode").GetString());
        Assert.Contains(
            nullBody.GetProperty("errors").EnumerateObject().SelectMany(property => property.Value.EnumerateArray()),
            code => code.GetString() == ShippingAuthoringValidationCodes.RegionGroupsRequired);

        // Neither malformed POST reached a handler or the store.
        Assert.Equal(0, harness.DraftDispatched);
        var (_, _, emptyList) = await harness.SendAsync(HttpMethod.Get, AdminDrafts);
        Assert.Empty(emptyList.GetProperty("drafts").EnumerateArray());

        // An explicit empty array stays a valid incomplete draft (draft CRUD, not coverage).
        var (createdStatus, _, created) = await harness.SendAsync(
            HttpMethod.Post,
            AdminDrafts,
            "{\"serviceCode\":\"post\",\"optionCode\":null,\"regionGroups\":[]}");
        Assert.Equal(StatusCodes.Status201Created, createdStatus);
        var draftId = created.GetProperty("draftId").GetGuid();
        Assert.Equal(1, harness.DraftDispatched);

        // The same required rule guards PUT, and a rejected replacement keeps the stored revision/tree.
        var (putOmittedStatus, _, putOmitted) = await harness.SendAsync(
            HttpMethod.Put,
            $"{AdminDrafts}/{draftId}",
            "{\"expectedRevision\":1}");
        Assert.Equal(StatusCodes.Status400BadRequest, putOmittedStatus);
        Assert.Equal("validation.failed", putOmitted.GetProperty("errorCode").GetString());
        Assert.Contains(
            putOmitted.GetProperty("errors").EnumerateObject().SelectMany(property => property.Value.EnumerateArray()),
            code => code.GetString() == ShippingAuthoringValidationCodes.RegionGroupsRequired);

        var (putNullStatus, _, putNull) = await harness.SendAsync(
            HttpMethod.Put,
            $"{AdminDrafts}/{draftId}",
            "{\"expectedRevision\":1,\"regionGroups\":null}");
        Assert.Equal(StatusCodes.Status400BadRequest, putNullStatus);
        Assert.Equal("validation.failed", putNull.GetProperty("errorCode").GetString());

        Assert.Equal(1, harness.DraftDispatched);
        var (_, _, after) = await harness.SendAsync(HttpMethod.Get, $"{AdminDrafts}/{draftId}");
        Assert.Equal(1, after.GetProperty("revision").GetInt64());
        Assert.Empty(after.GetProperty("regionGroups").EnumerateArray());
    }

    private static string CreateBody(Guid administrativeAreaId) => JsonSerializer.Serialize(
        new ShippingAuthoringDraftCreateRequest(
            "post",
            null,
            [
                new ShippingAuthoringRegionGroupWrite(
                    null,
                    [new ShippingAuthoringSelectedAreaWrite(ShippingAuthoringAreaKind.AdministrativeArea, Iran, administrativeAreaId)]),
            ]),
        new JsonSerializerOptions(JsonSerializerDefaults.Web));

    private static string ReplaceBody(long expectedRevision, Guid administrativeAreaId) => JsonSerializer.Serialize(
        new ShippingAuthoringDraftReplaceRegionsRequest(
            expectedRevision,
            [
                new ShippingAuthoringRegionGroupWrite(
                    null,
                    [new ShippingAuthoringSelectedAreaWrite(ShippingAuthoringAreaKind.AdministrativeArea, Iran, administrativeAreaId)]),
            ]),
        new JsonSerializerOptions(JsonSerializerDefaults.Web));

    private sealed class Harness : IDisposable
    {
        private readonly WebApplication _app;
        private readonly IServiceScope _scope;
        private readonly DispatchCounter _counter;
        private readonly StubCurrentTenant _tenant;

        private Harness(WebApplication app, IServiceScope scope, DispatchCounter counter, StubCurrentTenant tenant)
        {
            _app = app;
            _scope = scope;
            _counter = counter;
            _tenant = tenant;
        }

        public TenantContext TenantContext
        {
            get => _tenant.Current!;
            set => _tenant.Current = value;
        }

        public int SellerCalls { get; private set; }

        public bool DispatchedHasDraftWrite => _counter.DraftWrite;

        public int DraftDispatched => _counter.DraftDispatchCount;

        public static Harness Create(
            string database,
            ToobaEdition edition = ToobaEdition.SingleStore,
            IReadOnlyList<string>? allowed = null)
        {
            var counter = new DispatchCounter();
            var seller = new RecordingSellerAccess(counter);
            var tenant = new StubCurrentTenant(Tenant("store-a"));

            var builder = WebApplication.CreateBuilder(new WebApplicationOptions
            {
                EnvironmentName = Environments.Production,
            });
            builder.Logging.ClearProviders();
            builder.Services.AddRouting();
            builder.Services.AddSingleton<IHostEnvironment>(new StubHostEnvironment());
            builder.Services.AddToobaObservabilityFoundation();
            builder.Services.AddShippingEndpointPresentation();
            builder.Services.AddToobaCqrsFoundation(typeof(CreateShippingAuthoringDraftCommand).Assembly);
            builder.Services.AddProblemDetails();
            builder.Services.AddSingleton<IExceptionPresentationService, ExceptionPresentationService>();
            builder.Services.AddDbContext<ShippingDbContext>(options => options.UseInMemoryDatabase(database));
            builder.Services.AddScoped<IShippingAuthoringDraftDirectory, ShippingAuthoringDraftDirectory>();
            builder.Services.AddSingleton<IShippingAdminAuthorizer>(
                new RecordingAdminAuthorizer(allowed ?? [ShippingAdminPermissions.View, ShippingAdminPermissions.Manage]));
            builder.Services.AddSingleton<ISellerPanelAccess>(seller);
            builder.Services.AddSingleton<IPlatformEffectiveAccessReader>(new RecordingEffectiveAccess());
            builder.Services.AddSingleton<ICurrentEdition>(
                new StubCurrentEdition(new EditionContext(edition, "deployment-test")));
            builder.Services.AddSingleton<ICurrentTenant>(tenant);
            builder.Services.AddSingleton<IShippingMethodOptionValidator>(new FakeMethodValidator());
            builder.Services.AddSingleton<IShippingAuthoringDestinationResolver>(new FakeResolver());
            builder.Services.AddSingleton(counter);
            builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(DispatchCountingBehavior<,>));

            var app = builder.Build();
            app.MapShippingEndpoints();
            var scope = app.Services.CreateScope();
            var harness = new Harness(app, scope, counter, tenant);
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

        public async Task<(int Status, string? Location, JsonElement Body)> SendAsync(
            HttpMethod method,
            string path,
            string? json = null)
        {
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
            context.Request.Headers.AcceptLanguage = "fa";
            context.Request.ContentType = "application/json";
            if (json is not null)
            {
                context.Request.Body = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(json));
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

    private sealed class DispatchCounter
    {
        public bool DraftWrite { get; set; }

        public int DraftDispatchCount { get; set; }
    }

    private sealed class DispatchCountingBehavior<TRequest, TResponse>(DispatchCounter counter)
        : IPipelineBehavior<TRequest, TResponse>
        where TRequest : notnull
    {
        public async Task<TResponse> Handle(
            TRequest request,
            RequestHandlerDelegate<TResponse> next,
            CancellationToken cancellationToken)
        {
            if (request is CreateShippingAuthoringDraftCommand or ReplaceShippingAuthoringDraftRegionsCommand)
            {
                counter.DraftWrite = true;
                counter.DraftDispatchCount++;
            }

            return await next();
        }
    }

    private sealed class RecordingAdminAuthorizer(IReadOnlyList<string> allowed) : IShippingAdminAuthorizer
    {
        public Task<Guid> RequireAuthorizedAsync(
            HttpContext context,
            string permission,
            CancellationToken cancellationToken)
            => allowed.Contains(permission, StringComparer.Ordinal)
                ? Task.FromResult(ShippingAuthoringDraftEndpointTests.Iran)
                : Task.FromException<Guid>(
                    new SemanticException(new SemanticError(FoundationErrorCodes.AdminAuthorizationDenied)));
    }

    private sealed class RecordingSellerAccess(DispatchCounter counter) : ISellerPanelAccess
    {
        public Action? OnCall { get; set; }

        public Task<(Guid ActorUserId, Guid SellerPartyId)> RequireAuthorizedAsync(
            HttpRequest request,
            CancellationToken cancellationToken)
        {
            OnCall?.Invoke();
            return Task.FromResult((ShippingAuthoringDraftEndpointTests.Iran, ShippingAuthoringDraftEndpointTests.Tehran));
        }
    }

    private sealed class RecordingEffectiveAccess : IPlatformEffectiveAccessReader
    {
        public Task<IReadOnlyList<PlatformPermissionGrant>> GetEffectivePermissionsAsync(
            Guid userId,
            PlatformAccessOwnerKind ownerKind,
            Guid? ownerScopeId,
            CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<PlatformPermissionGrant>>(
                [new PlatformPermissionGrant(
                    ShippingAdminPermissions.Manage, PlatformAccessScopeKind.GlobalWithinOwner, null, false)]);
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
