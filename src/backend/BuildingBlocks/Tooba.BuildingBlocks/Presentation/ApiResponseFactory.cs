using System.Globalization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Tooba.BuildingBlocks.Localization;
using Tooba.BuildingBlocks.Presentation.Errors;
using Tooba.BuildingBlocks.Presentation.ProblemDetails;

namespace Tooba.BuildingBlocks.Presentation;

/// <summary>
/// مرز مرکزی ساخت ProblemDetails / IResult از استثناهای معنایی و پلتفرم.
/// مسیر تولید از culture/context مرکزی استفاده می‌کند — نه Accept-Language خام endpoint.
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

    /// <summary>استثنا را به <see cref="IResult"/> ProblemDetails تبدیل می‌کند (context-based).</summary>
    public IResult FromException(Exception exception)
    {
        var problem = CreateProblemDetails(exception);
        return Results.Json(problem, statusCode: problem.Status ?? StatusCodes.Status500InternalServerError);
    }

    /// <summary>ProblemDetails MVC را از استثنا می‌سازد (context-based).</summary>
    public Microsoft.AspNetCore.Mvc.ProblemDetails CreateProblemDetails(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        var mapped = _safeErrorMapper.Map(exception);
        return CreateFromMapped(mapped, exception);
    }

    /// <summary>
    /// ساخت ProblemDetails از نگاشت ازپیش‌محاسبه‌شده — برای جلوگیری از Map تکراری در orchestration.
    /// </summary>
    public Microsoft.AspNetCore.Mvc.ProblemDetails CreateFromMapped(MappedSafeError mapped, Exception exception)
    {
        ArgumentNullException.ThrowIfNull(mapped);
        ArgumentNullException.ThrowIfNull(exception);

        var context = _contextProvider.GetCurrentContext();
        var culture = ResolveCurrentCulture();
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
    /// فقط برای تست‌های واحد با override صریح Accept-Language — مسیر تولید از این استفاده نکند.
    /// </summary>
    public Microsoft.AspNetCore.Mvc.ProblemDetails CreateProblemDetailsForTests(
        Exception exception,
        string? acceptLanguage)
    {
        ArgumentNullException.ThrowIfNull(exception);
        var mapped = _safeErrorMapper.Map(exception);
        var context = _contextProvider.GetCurrentContext();
        var culture = _localeResolver.Resolve(acceptLanguage);
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
        if (mapped.ValidationErrors is { Count: > 0 })
        {
            problem.Extensions["errors"] = mapped.ValidationErrors;
        }

        return problem;
    }

    /// <summary>
    /// نگاشت SemanticException جاری به IResult — بدون اجبار مهاجرت Result&lt;T&gt;.
    /// </summary>
    public IResult FromSemanticException(SemanticException exception)
        => FromException(exception);

    /// <summary>نگاشت PlatformHttpException به IResult.</summary>
    public IResult FromPlatformException(PlatformHttpException exception)
        => FromException(exception);

    private CultureInfo ResolveCurrentCulture()
    {
        var header = _httpContextAccessor.HttpContext?.Request.Headers.AcceptLanguage.ToString();
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

    /// <summary>ProblemDetails امن از استثنا (context-based).</summary>
    public Microsoft.AspNetCore.Mvc.ProblemDetails Create(Exception exception)
        => _apiResponseFactory.CreateProblemDetails(exception);
}
