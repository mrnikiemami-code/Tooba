using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.Fulfillment.Application.Errors;
using Tooba.Fulfillment.Contracts.Errors;
using Tooba.Fulfillment.Contracts.Shipping;
using Tooba.Localization.Contracts.Ports;

namespace Tooba.Fulfillment.Application.Shipping;

/// <summary>نگاشت خطاهای معنایی shipping_service.* به SemanticError.</summary>
public static class ShippingServiceSemantic
{
    /// <summary>Failure بدون مقدار.</summary>
    public static Result Failure(string code) =>
        Result.Failure(new SemanticError(FulfillmentErrors.RequireKnown(code)));

    /// <summary>Failure با نوع مقدار.</summary>
    public static Result<T> Failure<T>(string code) =>
        Result.Failure<T>(new SemanticError(FulfillmentErrors.RequireKnown(code)));

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
