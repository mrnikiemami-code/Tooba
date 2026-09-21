using System.Diagnostics;
using MediatR;
using Tooba.BuildingBlocks.Observability.Correlation;
using Tooba.BuildingBlocks.Results;

namespace Tooba.BuildingBlocks.Observability.Tracing;

/// <summary>نام‌گذاری و برچسب‌گذاری پایدار spanهای MediatR.</summary>
public static class MediatRTraceNaming
{
    /// <summary>
    /// ماژول را از namespace اسمبلی استخراج می‌کند: <c>Tooba.{Module}.…</c> → module.
    /// </summary>
    public static string ResolveModule(Type requestType)
    {
        ArgumentNullException.ThrowIfNull(requestType);
        var ns = requestType.Namespace ?? string.Empty;
        const string prefix = "Tooba.";
        if (ns.StartsWith(prefix, StringComparison.Ordinal))
        {
            var rest = ns[prefix.Length..];
            var dot = rest.IndexOf('.');
            if (dot > 0)
            {
                return rest[..dot];
            }

            if (rest.Length > 0)
            {
                return rest;
            }
        }

        var assembly = requestType.Assembly.GetName().Name ?? "Unknown";
        if (assembly.StartsWith(prefix, StringComparison.Ordinal))
        {
            var parts = assembly.Split('.', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 2)
            {
                return parts[1];
            }
        }

        return "Unknown";
    }

    /// <summary>command / query / request از نام نوع.</summary>
    public static string ResolveRequestKind(Type requestType)
    {
        ArgumentNullException.ThrowIfNull(requestType);
        var name = requestType.Name;
        if (name.EndsWith("Command", StringComparison.Ordinal))
        {
            return TracingRequestKinds.Command;
        }

        if (name.EndsWith("Query", StringComparison.Ordinal))
        {
            return TracingRequestKinds.Query;
        }

        return TracingRequestKinds.Request;
    }

    /// <summary>نام عملیات کم‌کاردینالیتی: نام نوع بدون generic arity.</summary>
    public static string ResolveOperation(Type requestType)
    {
        ArgumentNullException.ThrowIfNull(requestType);
        var name = requestType.Name;
        var tick = name.IndexOf('`');
        return tick > 0 ? name[..tick] : name;
    }

    /// <summary><c>mediatr.{module}.{request}</c></summary>
    public static string ResolveActivityName(Type requestType)
    {
        var module = ResolveModule(requestType);
        var operation = ResolveOperation(requestType);
        return $"mediatr.{module}.{operation}";
    }
}

/// <summary>رفتار tracing مرکزی MediatR — یک span فرزند به ازای هر درخواست.</summary>
public sealed class TracingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    /// <inheritdoc />
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestType = typeof(TRequest);
        var activityName = MediatRTraceNaming.ResolveActivityName(requestType);
        using var activity = ToobaTelemetry.ActivitySource.StartActivity(activityName, ActivityKind.Internal);
        if (activity is not null)
        {
            var module = MediatRTraceNaming.ResolveModule(requestType);
            var operation = MediatRTraceNaming.ResolveOperation(requestType);
            var kind = MediatRTraceNaming.ResolveRequestKind(requestType);
            ToobaTraceEnricher.Enrich(
                activity,
                CorrelationIdContext.Current,
                module: module,
                operation: operation,
                requestKind: kind);
            activity.SetTag(TracingTagNames.RequestType, operation);
        }

        try
        {
            var response = await next().ConfigureAwait(false);
            if (response is IResultStatus { IsFailure: true } failure)
            {
                // Expected business failure: not a system Error span.
                activity?.SetStatus(ActivityStatusCode.Ok);
                activity?.SetTag(TracingTagNames.ResultStatus, "business_failure");
                if (failure.Errors.Count > 0)
                {
                    activity?.SetTag(TracingTagNames.ErrorCode, failure.Errors[0].Code);
                }
            }
            else
            {
                activity?.SetStatus(ActivityStatusCode.Ok);
            }

            return response;
        }
        catch (Exception ex)
        {
            if (activity is not null)
            {
                activity.SetStatus(ActivityStatusCode.Error);
                activity.SetTag(TracingTagNames.ExceptionType, ex.GetType().FullName);
            }

            throw;
        }
    }
}
