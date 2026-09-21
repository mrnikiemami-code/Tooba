using System.Diagnostics;
using Tooba.BuildingBlocks.Observability.Correlation;

namespace Tooba.BuildingBlocks.Observability.Tracing;

/// <summary>قرارداد tracing فراخوانی هم‌فرآیند بین ماژول‌ها.</summary>
public interface IModuleCallTracer
{
    /// <summary>یک span فرزند برای فراخوانی ماژول مقصد باز می‌کند.</summary>
    ModuleCallTrace Begin(
        string sourceModule,
        string targetModule,
        string operation);
}

/// <summary>پیاده‌سازی canonical روی <see cref="ToobaTelemetry.ActivitySource"/>.</summary>
public sealed class ModuleCallTracer : IModuleCallTracer
{
    /// <inheritdoc />
    public ModuleCallTrace Begin(string sourceModule, string targetModule, string operation)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceModule);
        ArgumentException.ThrowIfNullOrWhiteSpace(targetModule);
        ArgumentException.ThrowIfNullOrWhiteSpace(operation);

        var name = $"module.{sourceModule}.{targetModule}.{operation}";
        var activity = ToobaTelemetry.ActivitySource.StartActivity(name, ActivityKind.Internal);
        var correlationId = CorrelationIdContext.Current;
        ToobaTraceEnricher.Enrich(
            activity,
            correlationId,
            module: targetModule,
            operation: operation,
            requestKind: TracingRequestKinds.ModuleCall);
        activity?.SetTag(TracingTagNames.ModuleSource, sourceModule);
        activity?.SetTag(TracingTagNames.ModuleTarget, targetModule);
        return new ModuleCallTrace(activity);
    }
}

/// <summary>Facade سبک برای span فراخوانی ماژول — بدون payload/PII.</summary>
public sealed class ModuleCallTrace : IDisposable
{
    private readonly Activity? _activity;
    private bool _disposed;

    internal ModuleCallTrace(Activity? activity) => _activity = activity;

    /// <summary>وضعیت موفقیت.</summary>
    public void SetOk()
    {
        if (_activity is null)
        {
            return;
        }

        _activity.SetStatus(ActivityStatusCode.Ok);
    }

    /// <summary>وضعیت خطا؛ فقط نوع استثنا — نه Message.</summary>
    public void SetError(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        if (_activity is null)
        {
            return;
        }

        _activity.SetStatus(ActivityStatusCode.Error);
        _activity.SetTag(TracingTagNames.ExceptionType, exception.GetType().FullName);
    }

    /// <summary>شکست کسب‌وکار Result — وضعیت Ok با tagهای محدود؛ نه Error سیستم.</summary>
    public void SetBusinessFailure(string errorCode)
    {
        if (_activity is null)
        {
            return;
        }

        _activity.SetStatus(ActivityStatusCode.Ok);
        _activity.SetTag(TracingTagNames.ResultStatus, "business_failure");
        if (!string.IsNullOrWhiteSpace(errorCode))
        {
            _activity.SetTag(TracingTagNames.ErrorCode, errorCode.Trim());
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _activity?.Dispose();
    }
}

/// <summary>مقادیر پایدار <c>tooba.request_kind</c>.</summary>
public static class TracingRequestKinds
{
    /// <summary>HTTP.</summary>
    public const string Http = "http";

    /// <summary>فرمان MediatR.</summary>
    public const string Command = "command";

    /// <summary>پرس‌وجوی MediatR.</summary>
    public const string Query = "query";

    /// <summary>درخواست عمومی MediatR.</summary>
    public const string Request = "request";

    /// <summary>فراخوانی هم‌فرآیند ماژول.</summary>
    public const string ModuleCall = "module_call";

    /// <summary>پیام integration.</summary>
    public const string Message = "message";
}
