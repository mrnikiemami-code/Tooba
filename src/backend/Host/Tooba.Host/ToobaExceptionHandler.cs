using System.Diagnostics;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Tooba.Host;

internal sealed class ToobaExceptionHandler : IExceptionHandler
{
    private readonly ILogger<ToobaExceptionHandler> _logger;
    private readonly IHostEnvironment _environment;
    private readonly IProblemDetailsService _problemDetailsService;

    public ToobaExceptionHandler(
        ILogger<ToobaExceptionHandler> logger,
        IHostEnvironment environment,
        IProblemDetailsService problemDetailsService)
    {
        _logger = logger;
        _environment = environment;
        _problemDetailsService = problemDetailsService;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.TraceId.ToString() ?? httpContext.TraceIdentifier;
        var mapped = PlatformExceptionMapper.Map(exception);

        _logger.LogError(
            exception,
            "Unhandled exception. TraceId={TraceId} Path={Path} Method={Method} StatusCode={StatusCode}",
            traceId,
            httpContext.Request.Path.Value,
            httpContext.Request.Method,
            mapped.StatusCode);

        string? developmentDetail = null;
        if (_environment.IsDevelopment() && mapped.StatusCode >= 500)
        {
            developmentDetail = exception.GetType().Name;
        }

        var problem = PlatformExceptionMapper.ToProblemDetails(mapped, traceId, developmentDetail);
        httpContext.Response.StatusCode = mapped.StatusCode;

        await _problemDetailsService.WriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = problem,
        });

        return true;
    }
}
