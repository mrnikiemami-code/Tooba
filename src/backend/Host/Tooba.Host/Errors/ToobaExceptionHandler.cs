using System.Diagnostics;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Tooba.BuildingBlocks.Presentation;
using Tooba.BuildingBlocks.Presentation.Errors;

namespace Tooba.Host;

/// <summary>
/// نگاشت استثنای مدیریت‌نشده به ProblemDetails از طریق foundation مرکزی.
/// در Production جزئیات پیاده‌سازی و stack به کلاینت نمی‌رود.
/// </summary>
internal sealed class ToobaExceptionHandler : IExceptionHandler
{
    private readonly ILogger<ToobaExceptionHandler> _logger;
    private readonly ToobaProblemDetailsFactory _problemDetailsFactory;
    private readonly ISafeErrorMapper _safeErrorMapper;
    private readonly IProblemDetailsService _problemDetailsService;

    /// <summary>
    /// handler سراسری خطا را با کارخانهٔ مرکزی ProblemDetails تزریق می‌کند.
    /// </summary>
    public ToobaExceptionHandler(
        ILogger<ToobaExceptionHandler> logger,
        ToobaProblemDetailsFactory problemDetailsFactory,
        ISafeErrorMapper safeErrorMapper,
        IProblemDetailsService problemDetailsService)
    {
        _logger = logger;
        _problemDetailsFactory = problemDetailsFactory;
        _safeErrorMapper = safeErrorMapper;
        _problemDetailsService = problemDetailsService;
    }

    /// <summary>
    /// پاسخ استاندارد می‌نویسد و همیشه true برمی‌گرداند تا pipeline پیش‌فرض جزئیات را لو ندهد.
    /// </summary>
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var mapped = _safeErrorMapper.Map(exception);
        var traceId = Activity.Current?.TraceId.ToString() ?? httpContext.TraceIdentifier;

        if (mapped.Severity == ErrorSeverity.Error)
        {
            _logger.LogError(
                exception,
                "Unhandled exception. TraceId={TraceId} Path={Path} Method={Method} StatusCode={StatusCode} ErrorCode={ErrorCode}",
                traceId,
                httpContext.Request.Path.Value,
                httpContext.Request.Method,
                mapped.StatusCode,
                mapped.ErrorCode);
        }
        else
        {
            _logger.LogWarning(
                exception,
                "Handled business exception. TraceId={TraceId} Path={Path} Method={Method} StatusCode={StatusCode} ErrorCode={ErrorCode}",
                traceId,
                httpContext.Request.Path.Value,
                httpContext.Request.Method,
                mapped.StatusCode,
                mapped.ErrorCode);
        }

        var problem = _problemDetailsFactory.Create(
            exception,
            httpContext.Request.Headers.AcceptLanguage);
        httpContext.Response.StatusCode = problem.Status ?? mapped.StatusCode;

        await _problemDetailsService.WriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = problem,
        });

        return true;
    }
}
