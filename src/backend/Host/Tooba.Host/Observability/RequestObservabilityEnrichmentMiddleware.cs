using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Observability.Correlation;
using Tooba.BuildingBlocks.Observability.Logging;

namespace Tooba.Host;

/// <summary>
/// غنی‌سازی تودرتوی log scope پس از Tenant + Session auth — بدون تغییر CorrelationId و بدون PII.
/// </summary>
internal sealed class RequestObservabilityEnrichmentMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestObservabilityEnrichmentMiddleware> _logger;

    public RequestObservabilityEnrichmentMiddleware(
        RequestDelegate next,
        ILogger<RequestObservabilityEnrichmentMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(
        HttpContext context,
        ICorrelationIdProvider correlationIdProvider,
        ICurrentCommerceContext commerce,
        CurrentAuthenticatedSession session)
    {
        var correlationId = correlationIdProvider.GetCorrelationId()
            ?? context.Items[CorrelationIdMiddleware.HttpContextItemKey] as string
            ?? correlationIdProvider.EnsureCorrelationId();

        var tenantId = commerce.Current?.Tenant?.TenantId.Value;
        // Single-Store durable store identity equals TenantId when no separate StoreId exists on CommerceContext.
        var storeId = tenantId;
        var actorId = session.UserId is Guid userId && userId != Guid.Empty
            ? userId.ToString("N")
            : null;
        var clientIp = context.Connection.RemoteIpAddress?.ToString();

        var state = ObservabilityLogScope.CreateState(
            correlationId,
            requestId: context.TraceIdentifier,
            tenantId: tenantId,
            storeId: storeId,
            actorId: actorId,
            clientIp: clientIp,
            httpMethod: context.Request.Method,
            httpPath: context.Request.Path.Value);

        using (ObservabilityLogScope.Begin(_logger, state))
        {
            await _next(context).ConfigureAwait(false);
        }
    }
}
