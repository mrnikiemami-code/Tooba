using FluentValidation;
using Microsoft.AspNetCore.Http;

namespace Tooba.BuildingBlocks.Presentation.Errors;

/// <summary>
/// نگاشت مرکزی SemanticException / ValidationException / PlatformHttpException / unknown.
/// بدون switch ماژول‌محور و بدون ارسال exception.Message به کلاینت.
/// </summary>
public sealed class SafeErrorMapper : ISafeErrorMapper
{
    private static readonly IReadOnlyDictionary<string, string?> EmptyArgs =
        new Dictionary<string, string?>();

    /// <inheritdoc />
    public MappedSafeError Map(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        return exception switch
        {
            SemanticException semantic => MapSemantic(semantic),
            ValidationException validation => MapValidation(validation),
            PlatformHttpException platform => MapPlatform(platform),
            _ => MapUnexpected(),
        };
    }

    private static MappedSafeError MapSemantic(SemanticException semantic)
    {
        var code = semantic.Error.Code;
        var classification = ClassifySemanticCode(code);
        var status = classification switch
        {
            ErrorClassification.NotFound => StatusCodes.Status404NotFound,
            ErrorClassification.Conflict => StatusCodes.Status409Conflict,
            ErrorClassification.Forbidden => StatusCodes.Status403Forbidden,
            _ => StatusCodes.Status400BadRequest,
        };

        return new MappedSafeError(
            StatusCode: status,
            ErrorCode: code,
            LocalizationKey: code,
            Arguments: semantic.Error.Arguments,
            Classification: classification,
            Severity: ErrorSeverity.Warning,
            SafeTitleFallback: "Request rejected.");
    }

    private static MappedSafeError MapValidation(ValidationException validation)
    {
        var grouped = validation.Errors
            .GroupBy(e => string.IsNullOrWhiteSpace(e.PropertyName) ? "request" : e.PropertyName, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                g => g.Key,
                g => g.Select(e => string.IsNullOrWhiteSpace(e.ErrorCode) ? "validation.failed" : e.ErrorCode)
                    .Distinct(StringComparer.Ordinal)
                    .ToArray(),
                StringComparer.OrdinalIgnoreCase);

        return new MappedSafeError(
            StatusCode: StatusCodes.Status400BadRequest,
            ErrorCode: "validation.failed",
            LocalizationKey: "validation.failed",
            Arguments: EmptyArgs,
            Classification: ErrorClassification.Validation,
            Severity: ErrorSeverity.Warning,
            SafeTitleFallback: "Validation failed.",
            ValidationErrors: grouped);
    }

    private static MappedSafeError MapPlatform(PlatformHttpException platform)
    {
        var code = string.IsNullOrWhiteSpace(platform.ErrorCode)
            ? "platform.error"
            : platform.ErrorCode!;
        var severity = platform.StatusCode >= 500 ? ErrorSeverity.Error : ErrorSeverity.Warning;

        return new MappedSafeError(
            StatusCode: platform.StatusCode,
            ErrorCode: code,
            LocalizationKey: code,
            Arguments: EmptyArgs,
            Classification: ErrorClassification.Platform,
            Severity: severity,
            // Title on PlatformHttpException is already a safe client-facing string by contract.
            SafeTitleFallback: string.IsNullOrWhiteSpace(platform.Title) ? "Request failed." : platform.Title);
    }

    private static MappedSafeError MapUnexpected() =>
        new(
            StatusCode: StatusCodes.Status500InternalServerError,
            ErrorCode: "platform.unexpected",
            LocalizationKey: "platform.unexpected",
            Arguments: EmptyArgs,
            Classification: ErrorClassification.Unexpected,
            Severity: ErrorSeverity.Error,
            SafeTitleFallback: "An unexpected error occurred.");

    /// <summary>
    /// طبقه‌بندی convention-based روی کد معنایی — بدون نام ماژول خاص.
    /// </summary>
    internal static ErrorClassification ClassifySemanticCode(string code)
    {
        var c = code.Trim().ToLowerInvariant();
        if (c.EndsWith(".not_found", StringComparison.Ordinal)
            || c.EndsWith(".missing", StringComparison.Ordinal))
        {
            return ErrorClassification.NotFound;
        }

        if (c.Contains(".duplicate", StringComparison.Ordinal)
            || c.Contains("cannot_activate", StringComparison.Ordinal)
            || c.Contains(".conflict", StringComparison.Ordinal))
        {
            return ErrorClassification.Conflict;
        }

        if (c.Contains(".denied", StringComparison.Ordinal)
            || c.Contains("_denied", StringComparison.Ordinal)
            || c.EndsWith(".forbidden", StringComparison.Ordinal)
            || c.Contains(".forbidden", StringComparison.Ordinal))
        {
            return ErrorClassification.Forbidden;
        }

        return ErrorClassification.Business;
    }
}
