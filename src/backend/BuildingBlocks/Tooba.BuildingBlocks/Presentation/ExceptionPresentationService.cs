using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Tooba.BuildingBlocks.Presentation.Errors;
using Tooba.BuildingBlocks.Presentation.ProblemDetails;

namespace Tooba.BuildingBlocks.Presentation;

/// <summary>ارکستراسیون یک‌بارهٔ نگاشت، لاگ و نوشتن ProblemDetails.</summary>
public interface IExceptionPresentationService
{
    /// <summary>نگاشت امن، لاگ یک‌بار، و نوشتن پاسخ ProblemDetails.</summary>
    Task WriteAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken);
}

/// <summary>پیاده‌سازی مرکزی ارائهٔ استثنا — بدون تکرار mapper در handler.</summary>
public sealed class ExceptionPresentationService : IExceptionPresentationService
{
    private readonly ISafeErrorMapper _safeErrorMapper;
    private readonly ApiResponseFactory _apiResponseFactory;
    private readonly IProblemDetailsContextProvider _contextProvider;
    private readonly IProblemDetailsService _problemDetailsService;
    private readonly ILogger<ExceptionPresentationService> _logger;

    /// <summary>سرویس ارائه را می‌سازد.</summary>
    public ExceptionPresentationService(
        ISafeErrorMapper safeErrorMapper,
        ApiResponseFactory apiResponseFactory,
        IProblemDetailsContextProvider contextProvider,
        IProblemDetailsService problemDetailsService,
        ILogger<ExceptionPresentationService> logger)
    {
        _safeErrorMapper = safeErrorMapper;
        _apiResponseFactory = apiResponseFactory;
        _contextProvider = contextProvider;
        _problemDetailsService = problemDetailsService;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task WriteAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        ArgumentNullException.ThrowIfNull(exception);

        var mapped = _safeErrorMapper.Map(exception);
        var context = _contextProvider.GetCurrentContext();
        var activity = Activity.Current;
        var spanId = activity?.SpanId.ToString() ?? context.SpanId;

        if (mapped.Severity == ErrorSeverity.Error)
        {
            _logger.LogError(
                exception,
                "Unhandled exception. ErrorCode={ErrorCode} Classification={Classification} StatusCode={StatusCode} CorrelationId={CorrelationId} TraceId={TraceId} SpanId={SpanId} RequestId={RequestId} TenantId={TenantId} StoreId={StoreId} ActorId={ActorId} Path={Path} Method={Method}",
                mapped.ErrorCode,
                mapped.Classification,
                mapped.StatusCode,
                context.CorrelationId,
                context.TraceId,
                spanId,
                context.RequestId,
                context.TenantId,
                context.StoreId,
                context.ActorId,
                httpContext.Request.Path.Value,
                httpContext.Request.Method);
        }
        else
        {
            _logger.LogWarning(
                "Handled business exception. ErrorCode={ErrorCode} Classification={Classification} StatusCode={StatusCode} CorrelationId={CorrelationId} TraceId={TraceId} SpanId={SpanId} RequestId={RequestId} TenantId={TenantId} StoreId={StoreId} ActorId={ActorId} Path={Path} Method={Method}",
                mapped.ErrorCode,
                mapped.Classification,
                mapped.StatusCode,
                context.CorrelationId,
                context.TraceId,
                spanId,
                context.RequestId,
                context.TenantId,
                context.StoreId,
                context.ActorId,
                httpContext.Request.Path.Value,
                httpContext.Request.Method);
        }

        var problem = _apiResponseFactory.CreateFromMapped(mapped, exception);
        httpContext.Response.StatusCode = problem.Status ?? mapped.StatusCode;

        await _problemDetailsService.WriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = problem,
        });
    }
}
