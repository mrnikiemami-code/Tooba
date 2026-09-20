using System.Diagnostics;
using Tooba.BuildingBlocks;

namespace Tooba.Host;

/// <summary>
/// نگهداشت <see cref="CommerceContext"/> روی HttpContext.Items. هدر Tenant منبع حقیقت نیست.
/// </summary>
internal sealed class HttpCommerceContextAccessor : ICurrentCommerceContext, ICurrentEdition, ICurrentTenant, ICommerceContextAssigner
{
    /// <summary>
    /// کلید Items برای زمینهٔ تثبیت‌شدهٔ همین درخواست.
    /// </summary>
    internal const string ItemKey = "Tooba.CommerceContext";

    private readonly IHttpContextAccessor _httpContextAccessor;
    private CommerceContext? _assigned;

    /// <summary>
    /// accessor را به HttpContext درخواست وصل می‌کند. کارگر می‌تواند بدون Host مقدار بگذارد.
    /// </summary>
    public HttpCommerceContextAccessor(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    /// <inheritdoc />
    public CommerceContext? Current =>
        _assigned ?? _httpContextAccessor.HttpContext?.Items[ItemKey] as CommerceContext;

    /// <inheritdoc />
    public void Assign(CommerceContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        _assigned = context;
    }

    /// <inheritdoc />
    EditionContext? ICurrentEdition.Current => Current?.Edition;

    /// <inheritdoc />
    TenantContext? ICurrentTenant.Current => Current?.Tenant;
}

/// <summary>
/// Resolve امن Host → Tenant (Single-Store) یا اتصال marketplace. ناشناخته/غیرفعال = ۴۰۴ بدون نشت وجود.
/// Forwarded Host فقط اگر proxy در allowlist باشد در pipeline فعال شده است.
/// مسیرهای health/ready و probeهای dev از resolve رد می‌شوند تا DB برای liveness باز نشود.
/// </summary>
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

    /// <summary>
    /// میان‌افزار resolve را با registry پیکربندی و resolver اتصال می‌سازد.
    /// </summary>
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

    /// <summary>
    /// زمینه را می‌سازد یا ProblemDetails fail-closed می‌نویسد. جزئیات اتصال در پاسخ نیست.
    /// </summary>
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

    /// <summary>
    /// Host نرمال‌شده را با allowlist تطبیق می‌دهد. Marketplace Tenant نمی‌سازد.
    /// </summary>
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

    /// <summary>
    /// ۴۰۴ یکسان برای Host ناشناخته، Disabled و Suspended تا enumeration نشود.
    /// </summary>
    private static PlatformHttpException FailClosed() =>
        new(StatusCodes.Status404NotFound, "Not Found", "platform.resolution.failed");

    /// <summary>
    /// health/ready و probeهای تشخیصی از resolve و باز شدن DB معاف‌اند.
    /// </summary>
    private static bool ShouldSkip(PathString path) =>
        SkipPrefixes.Any(prefix => path.StartsWithSegments(prefix));

    /// <summary>
    /// ProblemDetails بدون جزئیات پیکربندی می‌نویسد.
    /// </summary>
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
