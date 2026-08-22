using System.Diagnostics;
using Tooba.BuildingBlocks;

namespace Tooba.Host;

internal sealed class HttpCommerceContextAccessor : ICurrentCommerceContext, ICurrentEdition, ICurrentTenant
{
    internal const string ItemKey = "Tooba.CommerceContext";

    private readonly IHttpContextAccessor _httpContextAccessor;

    public HttpCommerceContextAccessor(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public CommerceContext? Current =>
        _httpContextAccessor.HttpContext?.Items[ItemKey] as CommerceContext;

    EditionContext? ICurrentEdition.Current => Current?.Edition;

    TenantContext? ICurrentTenant.Current => Current?.Tenant;
}

internal sealed class TenantResolutionMiddleware
{
    private static readonly PathString[] SkipPrefixes =
    [
        new("/health"),
        new("/ready"),
        new("/__platform-error"),
        new("/__platform-conflict"),
    ];

    private readonly RequestDelegate _next;
    private readonly ControlPlaneRegistry _registry;
    private readonly IDatabaseConnectionResolver _connections;
    private readonly ILogger<TenantResolutionMiddleware> _logger;
    private readonly IProblemDetailsService _problemDetails;

    public TenantResolutionMiddleware(
        RequestDelegate next,
        ControlPlaneRegistry registry,
        IDatabaseConnectionResolver connections,
        ILogger<TenantResolutionMiddleware> logger,
        IProblemDetailsService problemDetails)
    {
        _next = next;
        _registry = registry;
        _connections = connections;
        _logger = logger;
        _problemDetails = problemDetails;
    }

    public async Task InvokeAsync(HttpContext httpContext)
    {
        if (ShouldSkip(httpContext.Request.Path))
        {
            await _next(httpContext);
            return;
        }

        var traceId = Activity.Current?.TraceId.ToString() ?? httpContext.TraceIdentifier;

        try
        {
            var context = Resolve(httpContext, traceId);
            httpContext.Items[HttpCommerceContextAccessor.ItemKey] = context;
            Activity.Current?.SetTag("tooba.edition", context.Edition.Edition.ToString());
            Activity.Current?.SetTag("tooba.deployment", context.Edition.DeploymentId);
            if (context.Tenant is { } tenant)
            {
                Activity.Current?.SetTag("tooba.tenant_id", tenant.TenantId.Value);
            }

            using (_logger.BeginScope(new Dictionary<string, object>
            {
                ["Edition"] = context.Edition.Edition.ToString(),
                ["DeploymentId"] = context.Edition.DeploymentId,
                ["TenantId"] = context.Tenant?.TenantId.Value ?? string.Empty,
            }))
            {
                await _next(httpContext);
            }
        }
        catch (PlatformHttpException ex)
        {
            _logger.LogWarning(
                "Commerce resolution failed. TraceId={TraceId} ErrorCode={ErrorCode} Path={Path}",
                traceId,
                ex.ErrorCode,
                httpContext.Request.Path.Value);
            await WriteProblemAsync(httpContext, ex, traceId);
        }
    }

    private CommerceContext Resolve(HttpContext httpContext, string traceId)
    {
        var editionContext = new EditionContext(_registry.Edition, _registry.DeploymentId);

        if (_registry.Edition == ToobaEdition.Unset)
        {
            throw new PlatformHttpException(
                StatusCodes.Status503ServiceUnavailable,
                "Service Unavailable",
                "platform.edition.unconfigured");
        }

        if (_registry.Edition == ToobaEdition.Marketplace)
        {
            var marketplaceRef = _registry.MarketplaceConnectionReference
                ?? throw new PlatformHttpException(
                    StatusCodes.Status503ServiceUnavailable,
                    "Service Unavailable",
                    "platform.connection.unconfigured");
            _ = _connections.Resolve(marketplaceRef);
            return new CommerceContext(editionContext, Tenant: null, marketplaceRef, traceId);
        }

        var rawHost = httpContext.Request.Host.Value;
        if (!HostNormalizer.TryNormalize(rawHost, out var host)
            || !_registry.Hosts.TryGetValue(host, out var record))
        {
            throw FailClosed();
        }

        if (record.Status != TenantStatus.Active)
        {
            throw FailClosed();
        }

        _ = _connections.Resolve(record.ConnectionReference);

        var tenant = new TenantContext(
            record.TenantId,
            record.Status,
            record.ConnectionReference,
            record.DisplayName,
            record.ThemeReference,
            record.DefaultMarketReference,
            host,
            record.PrimaryDomain);

        return new CommerceContext(editionContext, tenant, record.ConnectionReference, traceId);
    }

    private static PlatformHttpException FailClosed() =>
        new(StatusCodes.Status404NotFound, "Not Found", "platform.resolution.failed");

    private static bool ShouldSkip(PathString path) =>
        SkipPrefixes.Any(prefix => path.StartsWithSegments(prefix));

    private async Task WriteProblemAsync(HttpContext httpContext, PlatformHttpException exception, string traceId)
    {
        var mapped = PlatformExceptionMapper.Map(exception);
        var problem = PlatformExceptionMapper.ToProblemDetails(mapped, traceId, developmentDetail: null);
        httpContext.Response.StatusCode = mapped.StatusCode;
        await _problemDetails.WriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = problem,
        });
    }
}
