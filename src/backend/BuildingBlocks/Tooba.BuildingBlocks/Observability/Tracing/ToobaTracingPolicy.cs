using Microsoft.AspNetCore.Http;

namespace Tooba.BuildingBlocks.Observability.Tracing;

/// <summary>نام tagهای پایدار Tooba برای enrich span.</summary>
public static class TracingTagNames
{
    /// <summary>Correlation id.</summary>
    public const string CorrelationId = "tooba.correlation_id";

    /// <summary>نام ماژول.</summary>
    public const string Module = "tooba.module";

    /// <summary>نام عملیات.</summary>
    public const string Operation = "tooba.operation";

    /// <summary>نوع درخواست (http/message/internal).</summary>
    public const string RequestKind = "tooba.request_kind";

    /// <summary>ماژول مبدأ فراخوانی هم‌فرآیند.</summary>
    public const string ModuleSource = "tooba.module.source";

    /// <summary>ماژول مقصد فراخوانی هم‌فرآیند.</summary>
    public const string ModuleTarget = "tooba.module.target";

    /// <summary>نام نوع درخواست MediatR (بدون payload).</summary>
    public const string RequestType = "tooba.request_type";

    /// <summary>نوع استثنا — نه Message.</summary>
    public const string ExceptionType = "exception.type";

    /// <summary>وضعیت Result کسب‌وکار (مثلاً business_failure).</summary>
    public const string ResultStatus = "result.status";

    /// <summary>کد خطای پایدار اولیه — نه message.</summary>
    public const string ErrorCode = "error.code";

    /// <summary>متد HTTP.</summary>
    public const string HttpMethod = "http.method";

    /// <summary>مسیر/route HTTP.</summary>
    public const string HttpRoute = "http.route";
}

/// <summary>تصمیم policy برای span HTTP.</summary>
public enum HttpSpanDecision
{
    /// <summary>جمع‌آوری نشود.</summary>
    Skip = 0,

    /// <summary>span موجود ASP.NET enrich شود.</summary>
    EnrichExisting = 1,

    /// <summary>فقط وقتی instrumentation مالک غایب است fallback ساخته شود.</summary>
    CreateFallback = 2,
}

/// <summary>
/// Policy متمرکز tracing — مالکیت span با ASP.NET/HttpClient/MassTransit؛ Tooba فقط enrich/fallback.
/// </summary>
public static class ToobaTracingPolicy
{
    /// <summary>نام منبع Activity MassTransit.</summary>
    public const string MassTransitActivitySourceName = "MassTransit";

    /// <summary>آیا مسیر HTTP باید collect شود؟</summary>
    public static bool ShouldCollectHttpRequest(PathString path, bool tracingEnabled = true)
    {
        if (!tracingEnabled)
        {
            return false;
        }

        return !IsExcludedPath(path);
    }

    /// <summary>health/ready و پیشوندهای نویز را حذف می‌کند.</summary>
    public static bool IsExcludedPath(PathString path)
        => path.StartsWithSegments("/health") || path.StartsWithSegments("/ready");

    /// <summary>تصمیم span HTTP.</summary>
    public static HttpSpanDecision ResolveHttpSpan(
        PathString path,
        System.Diagnostics.Activity? current,
        bool aspNetCoreInstrumentationActive = true,
        bool tracingEnabled = true)
    {
        if (!ShouldCollectHttpRequest(path, tracingEnabled))
        {
            return HttpSpanDecision.Skip;
        }

        if (aspNetCoreInstrumentationActive && current is not null)
        {
            return HttpSpanDecision.EnrichExisting;
        }

        return HttpSpanDecision.CreateFallback;
    }

    /// <summary>آیا Activity متعلق به MassTransit است؟</summary>
    public static bool IsMassTransitOwnedActivity(System.Diagnostics.Activity activity)
        => activity.Source.Name.StartsWith(MassTransitActivitySourceName, StringComparison.Ordinal);
}

/// <summary>Enricher سبک برای tagهای Tooba روی Activity موجود.</summary>
public static class ToobaTraceEnricher
{
    /// <summary>Correlation و metadata را روی Activity موجود می‌نویسد — بدون StartActivity تکراری.</summary>
    public static void Enrich(
        System.Diagnostics.Activity? activity,
        string? correlationId,
        string? module = null,
        string? operation = null,
        string? requestKind = null)
    {
        if (activity is null)
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(correlationId))
        {
            activity.SetTag(TracingTagNames.CorrelationId, correlationId);
        }

        if (!string.IsNullOrWhiteSpace(module))
        {
            activity.SetTag(TracingTagNames.Module, module);
        }

        if (!string.IsNullOrWhiteSpace(operation))
        {
            activity.SetTag(TracingTagNames.Operation, operation);
        }

        if (!string.IsNullOrWhiteSpace(requestKind))
        {
            activity.SetTag(TracingTagNames.RequestKind, requestKind);
        }
    }

    /// <summary>Enrich درخواست HTTP روی span موجود طبق policy.</summary>
    public static void EnrichHttpRequest(
        HttpContext httpContext,
        string correlationId,
        System.Diagnostics.Activity? current = null)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        current ??= System.Diagnostics.Activity.Current;
        var decision = ToobaTracingPolicy.ResolveHttpSpan(httpContext.Request.Path, current);
        if (decision == HttpSpanDecision.Skip)
        {
            return;
        }

        // R1: never create duplicate HTTP spans when ASP.NET owns the server span.
        if (decision == HttpSpanDecision.CreateFallback)
        {
            return;
        }

        Enrich(current, correlationId, requestKind: "http");
        current?.SetTag(TracingTagNames.HttpMethod, httpContext.Request.Method);
        current?.SetTag(TracingTagNames.HttpRoute, httpContext.Request.Path.Value);
    }
}
