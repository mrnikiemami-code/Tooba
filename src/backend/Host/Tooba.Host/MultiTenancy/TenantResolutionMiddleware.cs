using System.Diagnostics;
using Tooba.BuildingBlocks;
using Tooba.Host.Configuration;
using Tooba.BuildingBlocks.Presentation;
using Tooba.BuildingBlocks.Presentation.Errors;
using Tooba.StoreContext.Contracts.Current;

namespace Tooba.Host.MultiTenancy;

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
    private readonly IExceptionPresentationService _presentation;

    /// <summary>
    /// میان‌افزار resolve را با registry پیکربندی و resolver اتصال می‌سازد.
    /// </summary>
    public TenantResolutionMiddleware(
        RequestDelegate next,
        ControlPlaneRegistry registry,
        IDatabaseConnectionResolver connections,
        ILogger<TenantResolutionMiddleware> logger,
        IExceptionPresentationService presentation)
    {
        _next = next;
        _registry = registry;
        _connections = connections;
        _logger = logger;
        _presentation = presentation;
    }

    /// <summary>
    /// زمینه را می‌سازد یا پاسخ استاندارد fail-closed می‌نویسد. جزئیات اتصال در پاسخ نیست.
    /// </summary>
    public async Task InvokeAsync(HttpContext httpContext, IStoreCommerceContextAssigner storeCommerceAssigner)
    {
        if (ShouldSkip(httpContext.Request.Path))
        {
            await _next(httpContext);
            return;
        }

        var traceId = Activity.Current?.TraceId.ToString() ?? httpContext.TraceIdentifier;

        try
        {
            var (context, storeCommerce) = Resolve(httpContext, traceId);
            httpContext.Items[HttpCommerceContextAccessor.ItemKey] = context;
            storeCommerceAssigner.Assign(storeCommerce);
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
        catch (SemanticException ex)
        {
            // Unique operational signal for commerce resolution; presentation service also logs canonically.
            _logger.LogWarning(
                "Commerce resolution failed. TraceId={TraceId} ErrorCode={ErrorCode} Path={Path}",
                traceId,
                ex.Error.Code,
                httpContext.Request.Path.Value);
            await _presentation.WriteAsync(httpContext, ex, httpContext.RequestAborted);
        }
    }

    /// <summary>
    /// Host نرمال‌شده را با allowlist تطبیق می‌دهد. Marketplace Tenant نمی‌سازد.
    /// زمینهٔ تجارت مؤثر فروشگاه جدا از زمینهٔ فنی بازگردانده می‌شود.
    /// </summary>
    private (CommerceContext Context, StoreCommerceContext StoreCommerce) Resolve(HttpContext httpContext, string traceId)
    {
        var editionContext = new EditionContext(_registry.Edition, _registry.DeploymentId);

        if (_registry.Edition == ToobaEdition.Unset)
        {
            throw new SemanticException(new SemanticError(FoundationErrorCodes.PlatformEditionUnconfigured));
        }

        if (_registry.Edition == ToobaEdition.Marketplace)
        {
            var marketplaceRef = _registry.MarketplaceConnectionReference
                ?? throw new SemanticException(new SemanticError(FoundationErrorCodes.PlatformConnectionUnconfigured));
            _ = _connections.Resolve(marketplaceRef);
            var marketplaceContext = new CommerceContext(
                editionContext,
                Tenant: null,
                marketplaceRef,
                traceId);
            return (marketplaceContext, _registry.DeploymentStoreCommerce);
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

        var context = new CommerceContext(
            editionContext,
            tenant,
            record.ConnectionReference,
            traceId);
        return (context, record.StoreCommerce);
    }

    /// <summary>
    /// ۴۰۴ یکسان برای Host ناشناخته، Disabled و Suspended تا enumeration نشود.
    /// </summary>
    private static SemanticException FailClosed() =>
        new(new SemanticError(FoundationErrorCodes.PlatformResolutionFailed));

    /// <summary>
    /// health/ready و probeهای تشخیصی از resolve و باز شدن DB معاف‌اند.
    /// </summary>
    private static bool ShouldSkip(PathString path) =>
        SkipPrefixes.Any(prefix => path.StartsWithSegments(prefix));
}
