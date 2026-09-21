using System.Globalization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Tooba.BuildingBlocks.Localization;
using Tooba.BuildingBlocks.Presentation.Errors;
using Tooba.BuildingBlocks.Presentation.ProblemDetails;

namespace Tooba.BuildingBlocks.Presentation;

/// <summary>
/// مرز مرکزی ساخت ProblemDetails / IResult از استثناهای معنایی و پلتفرم.
/// </summary>
public sealed class ApiResponseFactory
{
    private readonly ISafeErrorMapper _safeErrorMapper;
    private readonly IProblemDetailsContextProvider _contextProvider;
    private readonly IRequestLocaleResolver _localeResolver;
    private readonly IErrorMessageLocalizer _errorLocalizer;
    private readonly IHttpContextAccessor _httpContextAccessor;

    /// <summary>کارخانه پاسخ API را می‌سازد.</summary>
    public ApiResponseFactory(
        ISafeErrorMapper safeErrorMapper,
        IProblemDetailsContextProvider contextProvider,
        IRequestLocaleResolver localeResolver,
        IErrorMessageLocalizer errorLocalizer,
        IHttpContextAccessor httpContextAccessor)
    {
        _safeErrorMapper = safeErrorMapper;
        _contextProvider = contextProvider;
        _localeResolver = localeResolver;
        _errorLocalizer = errorLocalizer;
        _httpContextAccessor = httpContextAccessor;
    }

    /// <summary>استثنا را به <see cref="IResult"/> ProblemDetails تبدیل می‌کند.</summary>
    public IResult FromException(Exception exception, string? acceptLanguage = null)
    {
        var problem = CreateProblemDetails(exception, acceptLanguage);
        return Results.Json(problem, statusCode: problem.Status ?? StatusCodes.Status500InternalServerError);
    }

    /// <summary>ProblemDetails MVC را از استثنا می‌سازد.</summary>
    public Microsoft.AspNetCore.Mvc.ProblemDetails CreateProblemDetails(
        Exception exception,
        string? acceptLanguage = null)
    {
        ArgumentNullException.ThrowIfNull(exception);
        var mapped = _safeErrorMapper.Map(exception);
        var context = _contextProvider.GetCurrentContext();
        var culture = ResolveCulture(acceptLanguage);
        var title = _errorLocalizer.Localize(
            mapped.LocalizationKey,
            culture,
            mapped.Arguments,
            mapped.SafeTitleFallback);

        var problem = new Microsoft.AspNetCore.Mvc.ProblemDetails
        {
            Type = "about:blank",
            Title = title,
            Status = mapped.StatusCode,
            Instance = context.Path,
        };

        problem.Extensions["errorCode"] = mapped.ErrorCode;
        problem.Extensions["traceId"] = context.TraceId;
        problem.Extensions["correlationId"] = context.CorrelationId;
        if (!string.IsNullOrWhiteSpace(context.RequestId))
        {
            problem.Extensions["requestId"] = context.RequestId;
        }

        if (mapped.ValidationErrors is { Count: > 0 })
        {
            problem.Extensions["errors"] = mapped.ValidationErrors;
        }

        if (!context.HideExceptionDetails
            && mapped.Classification == ErrorClassification.Unexpected)
        {
            // Controlled Development detail only — never stack trace.
            problem.Detail = exception.GetType().Name;
        }

        return problem;
    }

    /// <summary>
    /// نگاشت SemanticException جاری به IResult — بدون اجبار مهاجرت Result&lt;T&gt;.
    /// </summary>
    public IResult FromSemanticException(SemanticException exception, string? acceptLanguage = null)
        => FromException(exception, acceptLanguage);

    /// <summary>نگاشت PlatformHttpException به IResult.</summary>
    public IResult FromPlatformException(PlatformHttpException exception, string? acceptLanguage = null)
        => FromException(exception, acceptLanguage);

    private CultureInfo ResolveCulture(string? acceptLanguage)
    {
        var header = acceptLanguage
                     ?? _httpContextAccessor.HttpContext?.Request.Headers.AcceptLanguage.ToString();
        return _localeResolver.Resolve(header);
    }
}

/// <summary>کارخانه ProblemDetails هم‌تراز با ApiResponseFactory برای handler سراسری.</summary>
public sealed class ToobaProblemDetailsFactory
{
    private readonly ApiResponseFactory _apiResponseFactory;

    /// <summary>کارخانه را روی ApiResponseFactory می‌سازد.</summary>
    public ToobaProblemDetailsFactory(ApiResponseFactory apiResponseFactory)
        => _apiResponseFactory = apiResponseFactory;

    /// <summary>ProblemDetails امن از استثنا.</summary>
    public Microsoft.AspNetCore.Mvc.ProblemDetails Create(Exception exception, string? acceptLanguage = null)
        => _apiResponseFactory.CreateProblemDetails(exception, acceptLanguage);
}
