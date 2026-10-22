using System.Text.Json;
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
using Tooba.Shipping.Application.Authoring.Ports;
using Tooba.Shipping.Application.Authoring.Queries;
using Tooba.Shipping.Contracts.Errors;
using Tooba.Shipping.Endpoints;
using Tooba.Shipping.Endpoints.Admin;
using Xunit;

namespace Tooba.Shipping.Tests.Authoring;

/// <summary>
/// TB-SHIPPING-ADMIN-CONFIGURATION-CONTEXT-188 — the real in-process admin GET.
/// </summary>
/// <remarks>
/// <para>
/// The endpoint set is produced by the production composition entry
/// (<see cref="ShippingEndpointModule.MapShippingEndpoints"/>) on a real
/// <see cref="WebApplication"/> route builder (built, never started), and each request is executed
/// through the real endpoint delegate, the real MediatR <see cref="MediatR.ISender"/>, the real
/// query handler, the real <see cref="ApiResponseFactory"/> and the real error catalog. Only the
/// authorizer seam and the authoring-context source are fakes, and they are labelled as such.
/// </para>
/// <para>
/// Literal-vs-parameterized precedence is answered by ASP.NET Core's own routing template engine
/// (<see cref="TemplateMatcher"/>), swept across every shipped template — not by a string
/// comparison. No <c>Microsoft.AspNetCore.TestHost</c> package was added to the repository.
/// </para>
/// </remarks>
public sealed class ShippingAuthoringContextEndpointTests
{
    private const string LiteralPath = "/v1/admin/shipping-services/authoring-context";
    private static readonly Guid Fa = Guid.Parse("01900000-0000-7000-8000-00000000aa01");
    private static readonly Guid En = Guid.Parse("01900000-0000-7000-8000-00000000aa02");
    private static readonly Guid Actor = Guid.Parse("01900000-0000-7000-8000-00000000ff01");

    [Fact]
    public async Task The_authorized_get_returns_the_typed_context_from_the_real_composition()
    {
        using var harness = Harness.Create(new AllowAuthorizer(Actor));

        var (status, body) = await harness.GetAsync(LiteralPath);

        Assert.Equal(StatusCodes.Status200OK, status);
        Assert.Equal("SingleStore", body.GetProperty("edition").GetString());
        Assert.Equal("IRR", body.GetProperty("defaultCurrency").GetString());
        Assert.Equal(Fa, body.GetProperty("defaultContentLanguageId").GetGuid());

        var languages = body.GetProperty("contentLanguages");
        Assert.Equal(1, languages.GetArrayLength());
        var language = languages[0];
        Assert.Equal(Fa, language.GetProperty("languageId").GetGuid());
        Assert.Equal("fa", language.GetProperty("code").GetString());
        Assert.Equal("fa-IR", language.GetProperty("culture").GetString());
        Assert.Equal("fa", language.GetProperty("urlPrefix").GetString());
        Assert.Equal("rtl", language.GetProperty("direction").GetString());
        Assert.Equal("Persian", language.GetProperty("displayName").GetString());
        Assert.Equal("فارسی", language.GetProperty("nativeName").GetString());
        Assert.True(language.GetProperty("isDefault").GetBoolean());
        Assert.Equal(0, language.GetProperty("sortOrder").GetInt32());

        // The permission is the existing catalog id, and the authorizer really ran.
        Assert.Equal(ShippingAdminPermissions.View, harness.Authorizer.LastPermission);
        Assert.Equal(1, harness.Authorizer.Calls);

        // Marketplace-only capabilities are NOT claimed just because context resolved.
        Assert.False(body.TryGetProperty("canManageSellerRules", out _));
        Assert.False(body.TryGetProperty("canSelectOwner", out _));
        Assert.False(body.TryGetProperty("sellerId", out _));
        Assert.False(body.TryGetProperty("tenantId", out _));
    }

    [Fact]
    public async Task An_unauthorized_call_is_denied_before_the_query_runs()
    {
        using var harness = Harness.Create(new DenyAuthorizer());

        var (status, body) = await harness.GetAsync(LiteralPath);

        Assert.Equal(StatusCodes.Status403Forbidden, status);
        Assert.Equal(FoundationErrorCodes.AdminAuthorizationDenied, body.GetProperty("errorCode").GetString());
        Assert.Equal(0, harness.Source.Calls);
    }

    [Fact]
    public async Task An_unavailable_authoring_configuration_returns_the_declared_503_problem()
    {
        // An active row without a usable owner-provided default is a readiness failure, not a fake success.
        using var harness = Harness.Create(
            new AllowAuthorizer(Actor),
            languages: [Snapshot(En, "en", "en-US", "en", isDefault: false)]);

        var (status, body) = await harness.GetAsync(LiteralPath);

        Assert.Equal(StatusCodes.Status503ServiceUnavailable, status);
        Assert.Equal(
            ShippingErrorCodes.AuthoringConfigurationUnavailable,
            body.GetProperty("errorCode").GetString());
        Assert.Equal(StatusCodes.Status503ServiceUnavailable, body.GetProperty("status").GetInt32());
        Assert.False(string.IsNullOrWhiteSpace(body.GetProperty("title").GetString()));
        Assert.False(string.IsNullOrWhiteSpace(body.GetProperty("traceId").GetString()));
        Assert.Equal(1, harness.Source.Calls);
    }

    [Fact]
    public async Task Browser_query_string_cannot_override_the_resolved_authority()
    {
        using var harness = Harness.Create(new AllowAuthorizer(Actor));

        var (status, body) = await harness.GetAsync(
            LiteralPath + "?edition=Marketplace&owner=someone&tenant=other&language=en&currency=USD&mode=marketplace");

        Assert.Equal(StatusCodes.Status200OK, status);
        Assert.Equal("SingleStore", body.GetProperty("edition").GetString());
        Assert.Equal("IRR", body.GetProperty("defaultCurrency").GetString());
        Assert.Equal(Fa, body.GetProperty("defaultContentLanguageId").GetGuid());
        Assert.Equal(1, body.GetProperty("contentLanguages").GetArrayLength());
    }

    [Fact]
    public void The_literal_route_has_no_parameters_and_no_shipped_template_collides_with_it()
    {
        using var harness = Harness.Create(new AllowAuthorizer(Actor));

        var literal = harness.FindEndpoint(LiteralPath, "GET");
        Assert.Empty(literal.RoutePattern.Parameters);

        // Sweep EVERY shipped template through the real routing template engine. Exactly one
        // template may claim the literal path, and it is the literal authoring-context route.
        var matchingTemplates = harness.Templates
            .Where(template => harness.TemplateMatches(template.Pattern, LiteralPath))
            .Select(template => template.Pattern)
            .ToArray();
        Assert.Equal([LiteralPath], matchingTemplates);

        // The {serviceCode} family still owns its own concrete paths and does not swallow the literal.
        Assert.True(harness.TemplateMatches("/v1/admin/shipping-services/{serviceCode}/rates", "/v1/admin/shipping-services/tooba-post/rates"));
        Assert.False(harness.TemplateMatches("/v1/admin/shipping-services/{serviceCode}/rates", LiteralPath));
        Assert.False(harness.TemplateMatches(LiteralPath, "/v1/admin/shipping-services/tooba-post/rates"));

        // Only GET is mapped on the literal path.
        Assert.Single(
            harness.Endpoints,
            endpoint => "/" + endpoint.RoutePattern.RawText?.TrimStart('/') == LiteralPath);
        Assert.Equal(
            ["GET"],
            literal.Metadata.GetMetadata<IHttpMethodMetadata>()!.HttpMethods.ToArray());
    }

    [Fact]
    public void The_new_route_is_the_only_authoring_route_and_keeps_the_admin_prefix()
    {
        using var harness = Harness.Create(new AllowAuthorizer(Actor));

        Assert.Equal(
            ["GET " + LiteralPath],
            harness.Routes
                .Where(route => route.Contains("authoring-context", StringComparison.Ordinal))
                .OrderBy(route => route, StringComparer.Ordinal)
                .ToArray());

        // Exactly one new endpoint was added on top of the certified 16-route surface.
        // TB-189 then added the admin and seller owner-scope reads, so the surface is 19 routes.
        Assert.Equal(27, harness.Routes.Count);
        Assert.Equal(27, harness.Endpoints.Count);

        // The endpoint declares the existing permission and no anonymous/transport binding.
        var source = File.ReadAllText(Path.Combine(
            Harness.RepoRoot(),
            "src",
            "backend",
            "Modules",
            "Shipping",
            "Tooba.Shipping.Endpoints",
            "Admin",
            "ShippingAuthoringContextEndpoints.cs"));
        Assert.Contains("ShippingAdminPermissions.View", source, StringComparison.Ordinal);
        Assert.DoesNotContain("AllowAnonymous", source, StringComparison.Ordinal);
        Assert.DoesNotContain("[FromQuery]", source, StringComparison.Ordinal);
        Assert.DoesNotContain("[FromRoute]", source, StringComparison.Ordinal);
        Assert.DoesNotContain("ReadFromJsonAsync", source, StringComparison.Ordinal);
        Assert.Contains("new GetShippingAuthoringContextQuery()", source, StringComparison.Ordinal);
    }

    private static ShippingAuthoringLanguageSource Snapshot(
        Guid id,
        string code,
        string culture,
        string urlPrefix,
        bool isDefault = true) =>
        new(id, code, culture, urlPrefix, true, isDefault, "rtl", "Persian", "فارسی", 0);

    private sealed class Harness : IDisposable
    {
        private readonly WebApplication _app;
        private readonly IServiceScope _scope;

        private Harness(
            WebApplication app,
            IServiceScope scope,
            IReadOnlyList<RouteEndpoint> endpoints,
            CountingAuthorizer authorizer,
            CountingSource source)
        {
            _app = app;
            _scope = scope;
            Endpoints = endpoints;
            Authorizer = authorizer;
            Source = source;
        }

        public IReadOnlyList<RouteEndpoint> Endpoints { get; }

        public CountingAuthorizer Authorizer { get; }

        public CountingSource Source { get; }

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
            CountingAuthorizer authorizer,
            IReadOnlyList<ShippingAuthoringLanguageSource>? languages = null)
        {
            var source = new CountingSource(new ShippingAuthoringContextSourceSnapshot(
                ToobaEdition.SingleStore,
                "IRR",
                languages ?? [Snapshot(Fa, "fa", "fa-IR", "fa")]));

            var builder = WebApplication.CreateBuilder(new WebApplicationOptions
            {
                EnvironmentName = Environments.Production,
            });
            builder.Logging.ClearProviders();
            builder.Services.AddRouting();
            builder.Services.AddSingleton<IHostEnvironment>(new StubHostEnvironment());
            builder.Services.AddToobaObservabilityFoundation();
            builder.Services.AddShippingEndpointPresentation();
            builder.Services.AddToobaCqrsFoundation(typeof(GetShippingAuthoringContextQuery).Assembly);
            // The Host's own exception envelope (same two registrations as Program.cs) so an endpoint
            // exception is mapped by the production service instead of a test-only re-implementation.
            builder.Services.AddProblemDetails();
            builder.Services.AddSingleton<IExceptionPresentationService, ExceptionPresentationService>();
            builder.Services.AddSingleton<IShippingAdminAuthorizer>(authorizer);
            builder.Services.AddSingleton<IShippingAuthoringContextSource>(source);

            var app = builder.Build();
            app.MapShippingEndpoints();

            var endpoints = ((IEndpointRouteBuilder)app).DataSources
                .SelectMany(dataSource => dataSource.Endpoints)
                .OfType<RouteEndpoint>()
                .ToArray();

            var scope = app.Services.CreateScope();
            return new Harness(app, scope, endpoints, authorizer, source);
        }

        public async Task<(int Status, JsonElement Body)> GetAsync(string pathAndQuery)
        {
            var endpoint = FindEndpoint(LiteralPath, "GET");
            var context = CreateContext(pathAndQuery);
            try
            {
                await endpoint.RequestDelegate!(context);
            }
            catch (Exception exception)
            {
                // Mirrors the Host pipeline: an uncaught endpoint exception is rendered by the real
                // central presentation service, not by the test.
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

        private DefaultHttpContext CreateContext(string pathAndQuery)
        {
            var context = new DefaultHttpContext { RequestServices = _scope.ServiceProvider };
            var split = pathAndQuery.Split('?', 2);
            context.Request.Method = "GET";
            context.Request.Path = split[0];
            context.Request.QueryString = split.Length == 2 ? new QueryString("?" + split[1]) : QueryString.Empty;
            context.Request.Headers.AcceptLanguage = "fa";
            context.Response.Body = new MemoryStream();
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

    private abstract class CountingAuthorizer : IShippingAdminAuthorizer
    {
        public int Calls { get; private set; }

        public string? LastPermission { get; private set; }

        public Task<Guid> RequireAuthorizedAsync(
            HttpContext context,
            string permission,
            CancellationToken cancellationToken)
        {
            Calls++;
            LastPermission = permission;
            return DecideAsync();
        }

        protected abstract Task<Guid> DecideAsync();
    }

    private sealed class AllowAuthorizer(Guid actor) : CountingAuthorizer
    {
        protected override Task<Guid> DecideAsync() => Task.FromResult(actor);
    }

    private sealed class DenyAuthorizer : CountingAuthorizer
    {
        protected override Task<Guid> DecideAsync() =>
            Task.FromException<Guid>(new SemanticException(
                new SemanticError(FoundationErrorCodes.AdminAuthorizationDenied)));
    }

    private sealed class CountingSource(ShippingAuthoringContextSourceSnapshot snapshot) : IShippingAuthoringContextSource
    {
        public int Calls { get; private set; }

        public Task<ShippingAuthoringContextSourceSnapshot> ReadAsync(CancellationToken cancellationToken)
        {
            Calls++;
            return Task.FromResult(snapshot);
        }
    }

    private sealed class StubHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Production;
        public string ApplicationName { get; set; } = "Tooba.Shipping.Tests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public string ContentRootFileProviderPath { get; set; } = AppContext.BaseDirectory;
        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } =
            new Microsoft.Extensions.FileProviders.NullFileProvider();
    }
}
