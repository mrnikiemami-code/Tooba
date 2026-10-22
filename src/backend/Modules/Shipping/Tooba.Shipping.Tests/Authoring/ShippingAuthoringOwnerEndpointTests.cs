using System.Text.Json;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Template;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.DependencyInjection;
using Tooba.BuildingBlocks.Presentation;
using Tooba.BuildingBlocks.Presentation.Errors;
using Tooba.BuildingBlocks.Results;
using Tooba.BuildingBlocks.Security;
using Tooba.Shipping.Application.Authoring.Queries;
using Tooba.Shipping.Contracts.Errors;
using Tooba.Shipping.Endpoints;
using Tooba.Shipping.Endpoints.Admin;
using Xunit;

namespace Tooba.Shipping.Tests.Authoring;

/// <summary>
/// TB-SHIPPING-ADMIN-AUTHORIZED-OWNER-SCOPE-189 — the two real in-process owner reads.
/// </summary>
/// <remarks>
/// <para>
/// The endpoint set comes from the production composition entry
/// (<see cref="ShippingEndpointModule.MapShippingEndpoints"/>), and each request executes the real
/// endpoint delegate, the real registered owner-scope adapter
/// (<see cref="ShippingAuthoringOwnerAuthorizer"/>), the real MediatR pipeline, the real query
/// handler, the real <see cref="ApiResponseFactory"/> and the real error catalog.
/// </para>
/// <para>
/// Only the platform seams the production adapter consumes are fakes — the admin gate, the seller
/// panel access, the effective-access reader and the resolved edition/tenant context. They are
/// labelled as fakes: no running identity store or merchant runtime is claimed.
/// </para>
/// </remarks>
[Collection(nameof(Tooba.Shipping.Tests.Observability.ShippingModuleCallTracingCollection))]
public sealed class ShippingAuthoringOwnerEndpointTests
{
    private const string AdminPath = "/v1/admin/shipping-services/authoring-owner";
    private const string SellerPath = "/v1/seller/shipping-services/authoring-owner";
    private static readonly Guid Actor = Guid.Parse("01900000-0000-7000-8000-00000000ff01");
    private static readonly Guid Seller = Guid.Parse("01900000-0000-7000-8000-00000000bb01");
    private static readonly Guid OtherSeller = Guid.Parse("01900000-0000-7000-8000-00000000bb02");

    [Fact]
    public async Task Admin_SingleStore_returns_Store_without_tenant_or_seller()
    {
        using var harness = Harness.Create(
            edition: ToobaEdition.SingleStore,
            tenant: Harness.Tenant(TenantStatus.Active, "store-alpha"),
            admin: new RecordingAdminAuthorizer());

        var (status, body) = await harness.GetAsync(AdminPath);

        Assert.Equal(StatusCodes.Status200OK, status);
        Assert.Equal("Store", body.GetProperty("ownerKind").GetString());
        Assert.Equal(JsonValueKind.Null, body.GetProperty("sellerPartyId").ValueKind);
        Assert.Equal(1, harness.Counter.Dispatched);

        // The existing catalog permission was used, and the seller seams were never involved.
        Assert.Equal(ShippingAdminPermissions.View, Assert.Single(harness.Admin.Permissions));
        Assert.Equal(0, harness.Seller.Calls);
        Assert.Equal(0, harness.Reader.Calls);

        // Internal tenant identity and mode/label/policy claims stay out of the wire contract.
        Assert.False(body.TryGetProperty("tenantId", out _));
        Assert.False(body.TryGetProperty("edition", out _));
        Assert.False(body.TryGetProperty("canManageSellerRules", out _));
        Assert.False(body.TryGetProperty("canSelectOwner", out _));
        Assert.False(body.TryGetProperty("mode", out _));
        Assert.False(body.TryGetProperty("token", out _));
        Assert.Equal(2, body.EnumerateObject().Count());
    }

    [Fact]
    public async Task Admin_Marketplace_returns_Marketplace_scope_from_the_trusted_context()
    {
        using var harness = Harness.Create(
            edition: ToobaEdition.Marketplace,
            tenant: new StubCurrentTenant(null),
            admin: new RecordingAdminAuthorizer());

        var (status, body) = await harness.GetAsync(AdminPath);

        Assert.Equal(StatusCodes.Status200OK, status);
        Assert.Equal("Marketplace", body.GetProperty("ownerKind").GetString());
        Assert.Equal(JsonValueKind.Null, body.GetProperty("sellerPartyId").ValueKind);
        Assert.Equal(1, harness.Counter.Dispatched);
    }

    [Fact]
    public async Task Seller_Marketplace_with_a_whole_owner_grant_returns_the_authenticated_seller()
    {
        using var harness = Harness.Create(
            edition: ToobaEdition.Marketplace,
            tenant: new StubCurrentTenant(null),
            seller: new RecordingSellerAccess(Actor, Seller),
            reader: new RecordingEffectiveAccess([
                new PlatformPermissionGrant(
                    ShippingAdminPermissions.View, PlatformAccessScopeKind.GlobalWithinOwner, null, false),
            ]));

        var (status, body) = await harness.GetAsync(SellerPath);

        Assert.Equal(StatusCodes.Status200OK, status);
        Assert.Equal("Seller", body.GetProperty("ownerKind").GetString());
        Assert.Equal(Seller, body.GetProperty("sellerPartyId").GetGuid());
        Assert.Equal(1, harness.Counter.Dispatched);

        // The granted owner is the one returned by the canonical seam, read for the same actor.
        var read = Assert.Single(harness.Reader.Requests);
        Assert.Equal(Actor, read.ActorUserId);
        Assert.Equal(PlatformAccessOwnerKind.Seller, read.OwnerKind);
        Assert.Equal(Seller, read.OwnerScopeId);
        Assert.Equal(1, harness.Seller.Calls);
        Assert.Equal(0, harness.Admin.Calls);
    }

    [Fact]
    public async Task Seller_route_in_SingleStore_is_404_before_seller_resolution_or_dispatch()
    {
        using var harness = Harness.Create(
            edition: ToobaEdition.SingleStore,
            tenant: Harness.Tenant(TenantStatus.Active, "store-alpha"),
            seller: new RecordingSellerAccess(Actor, Seller),
            reader: new RecordingEffectiveAccess([]));

        var (status, body) = await harness.GetAsync(SellerPath);

        Assert.Equal(StatusCodes.Status404NotFound, status);
        Assert.Equal(ShippingErrorCodes.AuthoringOwnerScopeUnavailable, body.GetProperty("errorCode").GetString());
        Assert.Equal(0, harness.Seller.Calls);
        Assert.Equal(0, harness.Reader.Calls);
        Assert.Equal(0, harness.Counter.Dispatched);

        // A request cannot forge Marketplace to unlock the seller route.
        var (overrideStatus, _) = await harness.GetAsync(
            SellerPath + "?edition=Marketplace&mode=marketplace&tenant=other");
        Assert.Equal(StatusCodes.Status404NotFound, overrideStatus);
        Assert.Equal(0, harness.Seller.Calls);
        Assert.Equal(0, harness.Counter.Dispatched);
    }

    [Theory]
    [InlineData("no-grant")]
    [InlineData("category-only")]
    [InlineData("ceiling")]
    public async Task Seller_without_a_whole_owner_grant_is_403_before_dispatch(string scenario)
    {
        IReadOnlyList<PlatformPermissionGrant> grants = scenario switch
        {
            "no-grant" => [],
            "category-only" => [new PlatformPermissionGrant(
                ShippingAdminPermissions.View, PlatformAccessScopeKind.Category, Guid.NewGuid(), false)],
            _ => [new PlatformPermissionGrant(
                ShippingAdminPermissions.View, PlatformAccessScopeKind.GlobalWithinOwner, null, true)],
        };
        using var harness = Harness.Create(
            edition: ToobaEdition.Marketplace,
            tenant: new StubCurrentTenant(null),
            seller: new RecordingSellerAccess(Actor, Seller),
            reader: new RecordingEffectiveAccess(grants));

        var (status, body) = await harness.GetAsync(SellerPath);

        Assert.Equal(StatusCodes.Status403Forbidden, status);
        Assert.Equal(ShippingErrorCodes.AuthoringOwnerScopeDenied, body.GetProperty("errorCode").GetString());
        Assert.Equal(0, harness.Counter.Dispatched);
        Assert.Equal(1, harness.Seller.Calls);
    }

    [Fact]
    public async Task Admin_denial_is_403_before_dispatch_and_never_calls_the_seller_seams()
    {
        using var harness = Harness.Create(
            edition: ToobaEdition.SingleStore,
            tenant: Harness.Tenant(TenantStatus.Active, "store-alpha"),
            admin: new RecordingAdminAuthorizer
            {
                Failure = new SemanticException(new SemanticError(FoundationErrorCodes.AdminAuthorizationDenied)),
            });

        var (status, body) = await harness.GetAsync(AdminPath);

        Assert.Equal(StatusCodes.Status403Forbidden, status);
        Assert.Equal(FoundationErrorCodes.AdminAuthorizationDenied, body.GetProperty("errorCode").GetString());
        Assert.Equal(0, harness.Counter.Dispatched);
        Assert.Equal(0, harness.Seller.Calls);
        Assert.Equal(0, harness.Reader.Calls);
    }

    [Fact]
    public async Task Admin_unresolved_context_is_the_canonical_503_before_dispatch()
    {
        using var harness = Harness.Create(
            edition: ToobaEdition.Unset,
            tenant: Harness.Tenant(TenantStatus.Active, "store-alpha"),
            admin: new RecordingAdminAuthorizer());

        var (status, body) = await harness.GetAsync(AdminPath);

        Assert.Equal(StatusCodes.Status503ServiceUnavailable, status);
        Assert.Equal(FoundationErrorCodes.AdminAuthorizationUnavailable, body.GetProperty("errorCode").GetString());
        Assert.Equal(0, harness.Counter.Dispatched);
    }

    [Fact]
    public async Task Query_string_and_headers_cannot_replace_the_resolved_owner_scope()
    {
        using var harness = Harness.Create(
            edition: ToobaEdition.Marketplace,
            tenant: new StubCurrentTenant(null),
            seller: new RecordingSellerAccess(Actor, Seller),
            reader: new RecordingEffectiveAccess([
                new PlatformPermissionGrant(
                    ShippingAdminPermissions.View, PlatformAccessScopeKind.GlobalWithinOwner, null, false),
            ]));

        var (status, body) = await harness.GetAsync(
            SellerPath + "?ownerKind=Marketplace&sellerId=" + OtherSeller + "&mode=store&tenant=other",
            context =>
            {
                context.Request.Headers["X-Tooba-Seller-Party-Id"] = OtherSeller.ToString();
                context.Request.Headers["X-Tooba-Owner-Kind"] = "Marketplace";
            });

        Assert.Equal(StatusCodes.Status200OK, status);
        Assert.Equal("Seller", body.GetProperty("ownerKind").GetString());
        Assert.Equal(Seller, body.GetProperty("sellerPartyId").GetGuid());
        Assert.NotEqual(OtherSeller, body.GetProperty("sellerPartyId").GetGuid());
        Assert.False(body.TryGetProperty("tenantId", out _));
    }

    [Fact]
    public async Task Each_request_authorizes_afresh_and_the_response_is_not_a_reusable_credential()
    {
        using var harness = Harness.Create(
            edition: ToobaEdition.Marketplace,
            tenant: new StubCurrentTenant(null),
            seller: new RecordingSellerAccess(Actor, Seller),
            reader: new RecordingEffectiveAccess([
                new PlatformPermissionGrant(
                    ShippingAdminPermissions.View, PlatformAccessScopeKind.GlobalWithinOwner, null, false),
            ]));

        await harness.GetAsync(SellerPath);
        await harness.GetAsync(SellerPath);

        // No cache and no stored grant: both the panel gate and the effective-permission read run again.
        Assert.Equal(2, harness.Seller.Calls);
        Assert.Equal(2, harness.Reader.Calls);
        Assert.Equal(2, harness.Counter.Dispatched);
    }

    [Fact]
    public void Both_owner_routes_are_literal_parameterless_and_do_not_collide()
    {
        using var harness = Harness.Create();

        foreach (var path in new[] { AdminPath, SellerPath })
        {
            var endpoint = harness.FindEndpoint(path, "GET");
            Assert.Empty(endpoint.RoutePattern.Parameters);
            Assert.Equal(["GET"], endpoint.Metadata.GetMetadata<IHttpMethodMetadata>()!.HttpMethods.ToArray());

            var claimed = harness.Templates
                .Where(template => harness.TemplateMatches(template.Pattern, path))
                .Select(template => template.Pattern)
                .ToArray();
            Assert.Equal([path], claimed);
        }

        // The {serviceCode} family still owns its concrete paths and cannot swallow either literal.
        Assert.True(harness.TemplateMatches(
            "/v1/admin/shipping-services/{serviceCode}/rates",
            "/v1/admin/shipping-services/tooba-post/rates"));
        Assert.False(harness.TemplateMatches(
            "/v1/admin/shipping-services/{serviceCode}/rates",
            AdminPath));
        Assert.False(harness.TemplateMatches(AdminPath, SellerPath));

        // Exactly the two new owner routes were added on top of the certified 17-route surface;
        // TB-190/191/192 draft routes and the TB-193 weekly schedule routes bring the certified
        // surface to 43 routes; TB-197 adds the choices read plus the assignment read/replace for
        // both audiences, so the certified surface is 53.
        Assert.Equal(53, harness.Routes.Count);
        Assert.Equal(
            ["GET " + AdminPath, "GET " + SellerPath],
            harness.Routes
                .Where(route => route.Contains("authoring-owner", StringComparison.Ordinal))
                .OrderBy(route => route, StringComparer.Ordinal)
                .ToArray());
    }

    [Fact]
    public void Neither_route_source_binds_transport_owner_input_or_a_fake_allow_authorizer()
    {
        using var harness = Harness.Create();

        foreach (var file in new[] { "ShippingAuthoringOwnerEndpoints.cs", "ShippingSellerAuthoringOwnerEndpoints.cs" })
        {
            var source = File.ReadAllText(Path.Combine(
                Harness.RepoRoot(), "src", "backend", "Modules", "Shipping", "Tooba.Shipping.Endpoints",
                file.StartsWith("ShippingSeller", StringComparison.Ordinal) ? "Seller" : "Admin",
                file));

            Assert.DoesNotContain("AllowAnonymous", source, StringComparison.Ordinal);
            Assert.DoesNotContain("[FromQuery]", source, StringComparison.Ordinal);
            Assert.DoesNotContain("[FromRoute]", source, StringComparison.Ordinal);
            Assert.DoesNotContain("ReadFromJsonAsync", source, StringComparison.Ordinal);
            Assert.DoesNotContain("Request.Headers", source, StringComparison.Ordinal);
            Assert.DoesNotContain("new ShippingAuthoringOwnerScope", source, StringComparison.Ordinal);
            Assert.Contains("new GetShippingAuthoringOwnerQuery(scope)", source, StringComparison.Ordinal);
        }

        // Both real route families exist under their real group prefixes.
        Assert.Contains(harness.Templates, template => template.Pattern == AdminPath);
        Assert.Contains(harness.Templates, template => template.Pattern == SellerPath);
    }

    private sealed class Harness : IDisposable
    {
        private readonly WebApplication _app;
        private readonly IServiceScope _scope;

        private Harness(
            WebApplication app,
            IServiceScope scope,
            IReadOnlyList<RouteEndpoint> endpoints,
            RecordingAdminAuthorizer admin,
            RecordingSellerAccess seller,
            RecordingEffectiveAccess reader,
            DispatchCounter counter)
        {
            _app = app;
            _scope = scope;
            Endpoints = endpoints;
            Admin = admin;
            Seller = seller;
            Reader = reader;
            Counter = counter;
        }

        public IReadOnlyList<RouteEndpoint> Endpoints { get; }

        public RecordingAdminAuthorizer Admin { get; }

        public RecordingSellerAccess Seller { get; }

        public RecordingEffectiveAccess Reader { get; }

        public DispatchCounter Counter { get; }

        public IReadOnlyList<(string Pattern, string Verb)> Templates => Endpoints
            .Select(endpoint => (
                "/" + endpoint.RoutePattern.RawText?.TrimStart('/'),
                string.Join(',', endpoint.Metadata.GetMetadata<IHttpMethodMetadata>()?.HttpMethods ?? [])))
            .ToArray();

        public IReadOnlyList<string> Routes => Templates
            .Select(template => $"{template.Verb} {template.Pattern}")
            .OrderBy(route => route, StringComparer.Ordinal)
            .ToArray();

        public static Harness Create(
            ToobaEdition edition = ToobaEdition.SingleStore,
            ICurrentTenant? tenant = null,
            RecordingAdminAuthorizer? admin = null,
            RecordingSellerAccess? seller = null,
            RecordingEffectiveAccess? reader = null)
        {
            admin ??= new RecordingAdminAuthorizer();
            seller ??= new RecordingSellerAccess(Actor, ShippingAuthoringOwnerEndpointTests.Seller);
            reader ??= new RecordingEffectiveAccess([]);
            tenant ??= Tenant(TenantStatus.Active, "store-alpha");

            var counter = new DispatchCounter();
            var builder = WebApplication.CreateBuilder(new WebApplicationOptions
            {
                EnvironmentName = Environments.Production,
            });
            builder.Logging.ClearProviders();
            builder.Services.AddRouting();
            builder.Services.AddSingleton<IHostEnvironment>(new StubHostEnvironment());
            builder.Services.AddToobaObservabilityFoundation();
            builder.Services.AddShippingEndpointPresentation();
            builder.Services.AddToobaCqrsFoundation(typeof(GetShippingAuthoringOwnerQuery).Assembly);
            // The Host's own exception envelope, so an endpoint exception is mapped by the production
            // presentation service instead of a test-only re-implementation.
            builder.Services.AddProblemDetails();
            builder.Services.AddSingleton<IExceptionPresentationService, ExceptionPresentationService>();
            builder.Services.AddSingleton(admin);
            builder.Services.AddSingleton<IShippingAdminAuthorizer>(admin);
            builder.Services.AddSingleton<ISellerPanelAccess>(seller);
            builder.Services.AddSingleton<IPlatformEffectiveAccessReader>(reader);
            builder.Services.AddSingleton<ICurrentEdition>(new StubCurrentEdition(new EditionContext(edition, "deployment-test")));
            builder.Services.AddSingleton(tenant);
            builder.Services.AddSingleton(counter);
            // Counts real ISender dispatch (innermost behavior) without replacing the handler.
            builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(DispatchCountingBehavior<,>));

            var app = builder.Build();
            app.MapShippingEndpoints();

            var endpoints = ((IEndpointRouteBuilder)app).DataSources
                .SelectMany(dataSource => dataSource.Endpoints)
                .OfType<RouteEndpoint>()
                .ToArray();

            var scope = app.Services.CreateScope();
            return new Harness(app, scope, endpoints, admin, seller, reader, counter);
        }

        public static ICurrentTenant Tenant(TenantStatus status, string tenantId) =>
            new StubCurrentTenant(new TenantContext(
                new TenantId(tenantId),
                status,
                new ConnectionReference("store-db"),
                "Test Store",
                null,
                null,
                "store.test",
                null));

        public Task<(int Status, JsonElement Body)> GetAsync(string pathAndQuery) =>
            GetAsync(pathAndQuery, configure: null);

        public async Task<(int Status, JsonElement Body)> GetAsync(
            string pathAndQuery,
            Action<HttpContext>? configure)
        {
            var path = pathAndQuery.Split('?', 2)[0];
            var endpoint = FindEndpoint(path, "GET");
            var context = CreateContext(pathAndQuery, configure);
            try
            {
                await endpoint.RequestDelegate!(context);
            }
            catch (Exception exception)
            {
                await _app.Services.GetRequiredService<IExceptionPresentationService>()
                    .WriteAsync(context, exception, CancellationToken.None);
            }

            return (context.Response.StatusCode, ReadBody(context));
        }

        public RouteEndpoint FindEndpoint(string pattern, string method) =>
            Endpoints.Single(endpoint =>
                string.Equals("/" + endpoint.RoutePattern.RawText?.TrimStart('/'), pattern, StringComparison.Ordinal)
                && (endpoint.Metadata.GetMetadata<IHttpMethodMetadata>()?.HttpMethods.Contains(method) ?? false));

        public bool TemplateMatches(string pattern, string path)
        {
            var template = TemplateParser.Parse(pattern.TrimStart('/'));
            var matcher = new TemplateMatcher(template, new RouteValueDictionary());
            return matcher.TryMatch(new PathString(path), new RouteValueDictionary());
        }

        public void Dispose()
        {
            _scope.Dispose();
            _app.DisposeAsync().AsTask().GetAwaiter().GetResult();
        }

        public static string RepoRoot()
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

        private DefaultHttpContext CreateContext(string pathAndQuery, Action<HttpContext>? configure)
        {
            var context = new DefaultHttpContext { RequestServices = _scope.ServiceProvider };
            var split = pathAndQuery.Split('?', 2);
            context.Request.Method = "GET";
            context.Request.Path = split[0];
            context.Request.QueryString = split.Length == 2 ? new QueryString("?" + split[1]) : QueryString.Empty;
            context.Request.Headers.AcceptLanguage = "fa";
            context.Response.Body = new MemoryStream();
            configure?.Invoke(context);
            _app.Services.GetRequiredService<IHttpContextAccessor>().HttpContext = context;
            return context;
        }

        private static JsonElement ReadBody(HttpContext context)
        {
            context.Response.Body.Position = 0;
            using var document = JsonDocument.Parse(context.Response.Body);
            return document.RootElement.Clone();
        }
    }

    private sealed class DispatchCounter
    {
        public int Dispatched { get; set; }
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
            if (request is GetShippingAuthoringOwnerQuery)
            {
                counter.Dispatched++;
            }

            return await next();
        }
    }

    private sealed class RecordingAdminAuthorizer : IShippingAdminAuthorizer
    {
        public List<string> Permissions { get; } = [];

        public int Calls => Permissions.Count;

        public Exception? Failure { get; init; }

        public Task<Guid> RequireAuthorizedAsync(
            HttpContext context, string permission, CancellationToken cancellationToken)
        {
            Permissions.Add(permission);
            return Failure is null ? Task.FromResult(Actor) : Task.FromException<Guid>(Failure);
        }
    }

    private sealed class RecordingSellerAccess(Guid actor, Guid seller) : ISellerPanelAccess
    {
        public int Calls { get; private set; }

        public Exception? Failure { get; init; }

        public Task<(Guid ActorUserId, Guid SellerPartyId)> RequireAuthorizedAsync(
            HttpRequest request, CancellationToken cancellationToken)
        {
            Calls++;
            return Failure is null
                ? Task.FromResult((actor, seller))
                : Task.FromException<(Guid, Guid)>(Failure);
        }
    }

    private sealed class RecordingEffectiveAccess(IReadOnlyList<PlatformPermissionGrant> grants)
        : IPlatformEffectiveAccessReader
    {
        public int Calls => Requests.Count;

        public List<(Guid ActorUserId, PlatformAccessOwnerKind OwnerKind, Guid? OwnerScopeId)> Requests { get; } = [];

        public Exception? Failure { get; init; }

        public Task<IReadOnlyList<PlatformPermissionGrant>> GetEffectivePermissionsAsync(
            Guid userId,
            PlatformAccessOwnerKind ownerKind,
            Guid? ownerScopeId,
            CancellationToken cancellationToken)
        {
            Requests.Add((userId, ownerKind, ownerScopeId));
            return Failure is null
                ? Task.FromResult(grants)
                : Task.FromException<IReadOnlyList<PlatformPermissionGrant>>(Failure);
        }
    }

    private sealed class StubCurrentEdition(EditionContext? current) : ICurrentEdition
    {
        public EditionContext? Current { get; } = current;
    }

    private sealed class StubCurrentTenant(TenantContext? current) : ICurrentTenant
    {
        public TenantContext? Current { get; } = current;
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
