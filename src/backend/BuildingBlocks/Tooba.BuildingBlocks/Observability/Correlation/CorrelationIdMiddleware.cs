using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Tooba.BuildingBlocks.Observability.Correlation;
using Tooba.BuildingBlocks.Observability.Logging;
using Tooba.BuildingBlocks.Observability.Tracing;

namespace Tooba.BuildingBlocks.Observability.Correlation;

/// <summary>
/// Middleware سبک R1: Ensure CorrelationId + response header + enrich موجود + log scope پایه.
/// </summary>
public sealed class CorrelationIdMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<CorrelationIdMiddleware> _logger;

    /// <summary>Middleware را می‌سازد.</summary>
    public CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    /// <summary>درخواست را با correlation lifecycle پردازش می‌کند.</summary>
    public async Task InvokeAsync(HttpContext context, ICorrelationIdProvider correlationIdProvider)
    {
        var incoming = context.Request.Headers[CorrelationIdConstants.HeaderName].FirstOrDefault();
        var correlationId = correlationIdProvider.EnsureCorrelationId(incoming);
        context.Response.Headers[CorrelationIdConstants.HeaderName] = correlationId;
        ToobaTraceEnricher.EnrichHttpRequest(context, correlationId);

        var state = ObservabilityLogScope.CreateState(
            correlationId,
            requestId: context.TraceIdentifier);
        using (ObservabilityLogScope.Begin(_logger, state))
        {
            await _next(context).ConfigureAwait(false);
        }
    }
}

/// <summary>Extension ثبت middleware همبستگی.</summary>
public static class CorrelationIdMiddlewareExtensions
{
    /// <summary>میان‌افزار CorrelationId Tooba را اضافه می‌کند.</summary>
    public static IApplicationBuilder UseToobaCorrelationId(this IApplicationBuilder app)
        => app.UseMiddleware<CorrelationIdMiddleware>();
}
