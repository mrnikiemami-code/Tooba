using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Tooba.BuildingBlocks.Observability.Correlation;

namespace Tooba.BuildingBlocks.Presentation.ProblemDetails;

/// <summary>
/// استخراج امن زمینه از HttpContext / Activity / Correlation — بدون DB و بدون dump هدر/توکن.
/// Commerce/auth از RequestServices در زمان درخواست خوانده می‌شود (سازگار با Singleton).
/// </summary>
public sealed class ProblemDetailsContextProvider : IProblemDetailsContextProvider
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IHostEnvironment _hostEnvironment;
    private readonly ICorrelationIdProvider _correlationIdProvider;

    /// <summary>Provider را می‌سازد.</summary>
    public ProblemDetailsContextProvider(
        IHttpContextAccessor httpContextAccessor,
        IHostEnvironment hostEnvironment,
        ICorrelationIdProvider correlationIdProvider)
    {
        _httpContextAccessor = httpContextAccessor;
        _hostEnvironment = hostEnvironment;
        _correlationIdProvider = correlationIdProvider;
    }

    /// <inheritdoc />
    public ToobaProblemDetailsContext GetCurrentContext()
    {
        HttpContext? httpContext = null;
        try
        {
            httpContext = _httpContextAccessor.HttpContext;
        }
        catch
        {
            // never throw from context extraction
        }

        var incoming = httpContext?.Request.Headers[CorrelationIdConstants.HeaderName].FirstOrDefault();
        var correlationId = _correlationIdProvider.EnsureCorrelationId(incoming);
        var activity = Activity.Current;
        var traceId = activity?.TraceId.ToString();
        if (string.IsNullOrWhiteSpace(traceId))
        {
            traceId = httpContext?.TraceIdentifier ?? correlationId;
        }

        string? tenantId = null;
        string? storeId = null;
        try
        {
            var commerce = httpContext?.RequestServices.GetService<ICurrentCommerceContext>()?.Current;
            tenantId = commerce?.Tenant?.TenantId.Value;
            storeId = commerce?.Tenant?.TenantId.Value;
        }
        catch
        {
            // ignore unavailable commerce
        }

        string? actorId = null;
        try
        {
            actorId = httpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier)
                      ?? httpContext?.User?.FindFirstValue("sub");
        }
        catch
        {
            // ignore unavailable auth
        }

        var hideDetails = !_hostEnvironment.IsDevelopment();

        return new ToobaProblemDetailsContext(
            CorrelationId: correlationId,
            TraceId: traceId,
            SpanId: activity?.SpanId.ToString(),
            RequestId: httpContext?.TraceIdentifier,
            TenantId: tenantId,
            StoreId: storeId,
            ActorId: actorId,
            Path: httpContext?.Request.Path.Value,
            Method: httpContext?.Request.Method,
            HideExceptionDetails: hideDetails);
    }
}
