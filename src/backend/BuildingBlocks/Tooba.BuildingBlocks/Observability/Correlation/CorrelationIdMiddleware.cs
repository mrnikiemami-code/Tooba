using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Tooba.BuildingBlocks.Observability.Logging;
using Tooba.BuildingBlocks.Observability.Tracing;

namespace Tooba.BuildingBlocks.Observability.Correlation;

/// <summary>
/// Middleware همبستگی: Ensure CorrelationId + response header + enrich Activity + log scope پایه.
/// AsyncLocal با BeginScope بازسازی می‌شود تا نشت بین درخواست‌ها رخ ندهد.
/// </summary>
public sealed class CorrelationIdMiddleware
{
    /// <summary>آینهٔ سازگاری اختیاری روی HttpContext.Items — SSOT نیست.</summary>
    public const string HttpContextItemKey = "Tooba.CorrelationId";

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
        string correlationId;
        if (!string.IsNullOrWhiteSpace(incoming) && CorrelationIdContext.TryNormalize(incoming, out var normalizedIncoming))
        {
            correlationId = normalizedIncoming;
        }
        else
        {
            correlationId = Guid.NewGuid().ToString("N");
        }

        using var correlationScope = CorrelationIdContext.BeginScope(correlationId);
        correlationIdProvider.SetCorrelationId(correlationId);
        context.Items[HttpContextItemKey] = correlationId;
        context.Response.OnStarting(() =>
        {
            context.Response.Headers[CorrelationIdConstants.HeaderName] = correlationId;
            return Task.CompletedTask;
        });
        // Also set immediately so early failure responses still carry the header when possible.
        context.Response.Headers[CorrelationIdConstants.HeaderName] = correlationId;
        ToobaTraceEnricher.EnrichHttpRequest(context, correlationId);

        var state = ObservabilityLogScope.CreateState(
            correlationId,
            requestId: context.TraceIdentifier,
            httpMethod: context.Request.Method,
            httpPath: context.Request.Path.Value);
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
