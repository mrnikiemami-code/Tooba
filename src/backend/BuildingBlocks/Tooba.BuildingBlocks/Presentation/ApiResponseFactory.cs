using System.Globalization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Tooba.BuildingBlocks.Localization;
using Tooba.BuildingBlocks.Presentation.Errors;
using Tooba.BuildingBlocks.Presentation.ProblemDetails;
using Tooba.BuildingBlocks.Results;

namespace Tooba.BuildingBlocks.Presentation;

/// <summary>
/// مرز مرکزی ساخت IResult از Result موفقیت/شکست و از استثناهای معنایی/پلتفرم.
/// مسیر تولید از culture/context مرکزی استفاده می‌کند — نه Accept-Language خام endpoint.
/// موفقیت Offer seller به‌صورت raw DTO (سازگاری کلاینت) برمی‌گردد؛ نه envelope اجباری.
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

    /// <summary>
    /// Result بدون مقدار: موفقیت → 204؛ شکست → ProblemDetails مرکزی.
    /// </summary>
    public IResult From(Result result)
    {
        ArgumentNullException.ThrowIfNull(result);
        if (result.IsFailure)
        {
            return FromFailure(result.Errors);
        }

        return Microsoft.AspNetCore.Http.Results.NoContent();
    }

    /// <summary>
    /// Result&lt;T&gt;: موفقیت → JSON خام مقدار (سازگاری کلاینت)؛ شکست → ProblemDetails.
    /// </summary>
    public IResult From<T>(Result<T> result)
    {
        ArgumentNullException.ThrowIfNull(result);
        if (result.IsFailure)
        {
            return FromFailure(result.Errors);
        }

        return Microsoft.AspNetCore.Http.Results.Json(result.Value);
    }

    /// <summary>
    /// Created: موفقیت → 201 + Location + JSON خام؛ شکست → ProblemDetails (بدون Location).
    /// </summary>
    public IResult Created<T>(string location, Result<T> result)
    {
        ArgumentNullException.ThrowIfNull(result);
        if (string.IsNullOrWhiteSpace(location))
        {
            throw new ArgumentException("created_location_required", nameof(location));
        }

        if (result.IsFailure)
        {
            return FromFailure(result.Errors);
        }

        return Microsoft.AspNetCore.Http.Results.Created(location, result.Value);
    }

    /// <summary>SemanticError(ها) را به ProblemDetails IResult نگاشت می‌کند.</summary>
    public IResult FromFailure(IReadOnlyList<SemanticError> errors)
    {
        ArgumentNullException.ThrowIfNull(errors);
        var mapped = _safeErrorMapper.Map(errors);
        var problem = BuildProblemDetails(mapped, exceptionForUnexpectedDetail: null);
        return Microsoft.AspNetCore.Http.Results.Json(problem, statusCode: problem.Status ?? StatusCodes.Status500InternalServerError);
    }

    /// <summary>یک SemanticError را به ProblemDetails IResult نگاشت می‌کند.</summary>
    public IResult FromFailure(SemanticError error)
    {
        ArgumentNullException.ThrowIfNull(error);
        return FromFailure((IReadOnlyList<SemanticError>)[error]);
    }

    /// <summary>استثنا را به <see cref="IResult"/> ProblemDetails تبدیل می‌کند (context-based).</summary>
    public IResult FromException(Exception exception)
    {
        var problem = CreateProblemDetails(exception);
        return Microsoft.AspNetCore.Http.Results.Json(problem, statusCode: problem.Status ?? StatusCodes.Status500InternalServerError);
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
        => BuildProblemDetails(mapped, exception);

    private Microsoft.AspNetCore.Mvc.ProblemDetails BuildProblemDetails(
        MappedSafeError mapped,
        Exception? exceptionForUnexpectedDetail)
    {
        ArgumentNullException.ThrowIfNull(mapped);

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
            && mapped.Classification == ErrorClassification.Unexpected
            && exceptionForUnexpectedDetail is not null)
        {
            // Controlled Development detail only — never stack trace.
            problem.Detail = exceptionForUnexpectedDetail.GetType().Name;
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
