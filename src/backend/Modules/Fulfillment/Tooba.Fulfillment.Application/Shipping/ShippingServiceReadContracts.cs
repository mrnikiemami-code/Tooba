using Tooba.Fulfillment.Contracts.Shipping;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.Fulfillment.Contracts.Errors;
using Tooba.Localization.Contracts;

namespace Tooba.Fulfillment.Application.Shipping;

/// <summary>ردیف لیست سرویس ارسال (شکل JSON پایدار).</summary>
public sealed record ShippingServiceListItemDto(
    Guid ShippingServiceId,
    string Code,
    string ProviderKind,
    string IconKey,
    string ColorKey,
    string Name,
    bool IsActive,
    int SortOrder,
    int OptionCount,
    int ActiveOptionCount);

/// <summary>ترجمهٔ سرویس ارسال والد.</summary>
public sealed record ShippingServiceTranslationDto(Guid LanguageId, string Name, string? Description);

/// <summary>ترجمهٔ نوع سرویس فرزند.</summary>
public sealed record ShippingServiceOptionTranslationDto(Guid LanguageId, string Name);

/// <summary>جزئیات گزینهٔ سطح ۲.</summary>
public sealed record ShippingServiceOptionDetailDto(
    Guid ShippingServiceOptionId,
    string Code,
    bool IsActive,
    int SortOrder,
    IReadOnlyList<ShippingServiceOptionTranslationDto> Translations);

/// <summary>جزئیات سرویس ارسال (شکل JSON پایدار).</summary>
public sealed record ShippingServiceDetailDto(
    Guid ShippingServiceId,
    string Code,
    string ProviderKind,
    string IconKey,
    string ColorKey,
    bool IsActive,
    int SortOrder,
    IReadOnlyList<ShippingServiceTranslationDto> Translations,
    IReadOnlyList<ShippingServiceOptionDetailDto> Options);

/// <summary>نگاشت خطاهای معنایی shipping_service.* به SemanticError.</summary>
public static class ShippingServiceSemantic
{
    /// <summary>Failure بدون مقدار.</summary>
    public static Result Failure(string code) =>
        Result.Failure(new SemanticError(Normalize(code)));

    /// <summary>Failure با نوع مقدار.</summary>
    public static Result<T> Failure<T>(string code) =>
        Result.Failure<T>(new SemanticError(Normalize(code)));

private static string Normalize(string code)
    {
        if (Errors.FulfillmentExceptionMapper.TryMapExact(code, out var error))
        {
            return error.Code;
        }

        throw new InvalidOperationException(code);
    }
/// <summary>Resolve language id with default/culture/code/urlPrefix fallback.</summary>
    public static async Task<Guid> ResolveLanguageIdAsync(
        ILanguageLookup languages,
        string? language,
        CancellationToken cancellationToken)
    {
        var langs = await languages.ListAsync(cancellationToken);
        if (langs.Count == 0)
        {
            return Guid.Empty;
        }

        if (!string.IsNullOrWhiteSpace(language))
        {
            var needle = language.Trim();
            var hit = langs.FirstOrDefault(x =>
                x.Code.Equals(needle, StringComparison.OrdinalIgnoreCase)
                || x.Culture.Equals(needle, StringComparison.OrdinalIgnoreCase)
                || x.UrlPrefix.Equals(needle, StringComparison.OrdinalIgnoreCase)
                || x.Culture.StartsWith(needle, StringComparison.OrdinalIgnoreCase));
            if (hit is not null)
            {
                return hit.LanguageId;
            }
        }

        return (langs.FirstOrDefault(x => x.IsDefault) ?? langs[0]).LanguageId;
    }

    /// <summary>Map catalog snapshot to detail DTO.</summary>
    public static ShippingServiceDetailDto ToDetail(ShippingCatalogServiceSnapshot entity) =>
        new(
            entity.ShippingServiceId,
            entity.Code,
            entity.ProviderKind,
            entity.IconKey,
            entity.ColorKey,
            entity.IsActive,
            entity.SortOrder,
            entity.Translations.Select(t => new ShippingServiceTranslationDto(t.LanguageId, t.Name, t.Description)).ToList(),
            entity.Options.Select(o =>
                new ShippingServiceOptionDetailDto(
                    o.ShippingServiceOptionId,
                    o.Code,
                    o.IsActive,
                    o.SortOrder,
                    o.Translations.Select(r => new ShippingServiceOptionTranslationDto(r.LanguageId, r.Name)).ToList())).ToList());
}
