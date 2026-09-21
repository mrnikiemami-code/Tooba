using FluentValidation;
using Microsoft.AspNetCore.Http;

namespace Tooba.BuildingBlocks.Presentation.Errors;

/// <summary>
/// نگاشت مرکزی SemanticException / ValidationException / PlatformHttpException / unknown.
/// طبقه‌بندی از کاتالوگ صریح — بدون heuristic روی نام کد.
/// </summary>
public sealed class SafeErrorMapper : ISafeErrorMapper
{
    private static readonly IReadOnlyDictionary<string, string?> EmptyArgs =
        new Dictionary<string, string?>();

    private readonly IErrorDefinitionCatalog _catalog;

    /// <summary>نگاشت‌گر را با کاتالوگ صریح می‌سازد.</summary>
    public SafeErrorMapper(IErrorDefinitionCatalog catalog)
        => _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));

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

    private MappedSafeError MapSemantic(SemanticException semantic)
    {
        var code = semantic.Error.Code;
        if (_catalog.TryGet(code, out var descriptor))
        {
            return new MappedSafeError(
                StatusCode: descriptor.HttpStatus,
                ErrorCode: descriptor.Code,
                LocalizationKey: descriptor.LocalizationKey,
                Arguments: semantic.Error.Arguments,
                Classification: descriptor.Classification,
                Severity: descriptor.Severity,
                SafeTitleFallback: descriptor.SafeTitleFallback);
        }

        // Unknown semantic code: safe generic business fallback — never guess status from name.
        return new MappedSafeError(
            StatusCode: StatusCodes.Status400BadRequest,
            ErrorCode: code,
            LocalizationKey: code,
            Arguments: semantic.Error.Arguments,
            Classification: ErrorClassification.Business,
            Severity: ErrorSeverity.Warning,
            SafeTitleFallback: "Request rejected.");
    }

    private MappedSafeError MapValidation(ValidationException validation)
    {
        var grouped = validation.Errors
            .GroupBy(e => string.IsNullOrWhiteSpace(e.PropertyName) ? "request" : e.PropertyName, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                g => g.Key,
                g => g.Select(e => string.IsNullOrWhiteSpace(e.ErrorCode) ? "validation.failed" : e.ErrorCode)
                    .Distinct(StringComparer.Ordinal)
                    .ToArray(),
                StringComparer.OrdinalIgnoreCase);

        if (_catalog.TryGet("validation.failed", out var descriptor))
        {
            return new MappedSafeError(
                StatusCode: descriptor.HttpStatus,
                ErrorCode: descriptor.Code,
                LocalizationKey: descriptor.LocalizationKey,
                Arguments: EmptyArgs,
                Classification: descriptor.Classification,
                Severity: descriptor.Severity,
                SafeTitleFallback: descriptor.SafeTitleFallback,
                ValidationErrors: grouped);
        }

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

    private MappedSafeError MapPlatform(PlatformHttpException platform)
    {
        // Transitional legacy input: HTTP status from exception; prefer catalog when code is registered.
        var code = string.IsNullOrWhiteSpace(platform.ErrorCode)
            ? "platform.error"
            : platform.ErrorCode!;
        var severity = platform.StatusCode >= 500 ? ErrorSeverity.Error : ErrorSeverity.Warning;
        var safeTitle = string.IsNullOrWhiteSpace(platform.Title) ? "Request failed." : platform.Title;

        if (_catalog.TryGet(code, out var descriptor))
        {
            return new MappedSafeError(
                StatusCode: platform.StatusCode,
                ErrorCode: descriptor.Code,
                LocalizationKey: descriptor.LocalizationKey,
                Arguments: EmptyArgs,
                Classification: ErrorClassification.Platform,
                Severity: severity,
                SafeTitleFallback: safeTitle);
        }

        return new MappedSafeError(
            StatusCode: platform.StatusCode,
            ErrorCode: code,
            LocalizationKey: code,
            Arguments: EmptyArgs,
            Classification: ErrorClassification.Platform,
            Severity: severity,
            SafeTitleFallback: safeTitle);
    }

    private MappedSafeError MapUnexpected()
    {
        if (_catalog.TryGet("platform.unexpected", out var descriptor))
        {
            return new MappedSafeError(
                StatusCode: descriptor.HttpStatus,
                ErrorCode: descriptor.Code,
                LocalizationKey: descriptor.LocalizationKey,
                Arguments: EmptyArgs,
                Classification: descriptor.Classification,
                Severity: descriptor.Severity,
                SafeTitleFallback: descriptor.SafeTitleFallback);
        }

        return new MappedSafeError(
            StatusCode: StatusCodes.Status500InternalServerError,
            ErrorCode: "platform.unexpected",
            LocalizationKey: "platform.unexpected",
            Arguments: EmptyArgs,
            Classification: ErrorClassification.Unexpected,
            Severity: ErrorSeverity.Error,
            SafeTitleFallback: "An unexpected error occurred.");
    }
}
