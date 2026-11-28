using Tooba.Fulfillment.Contracts.Shipping;
using Tooba.Localization.Contracts.Ports;
using Tooba.Shipping.Contracts.Authoring;
using Tooba.Shipping.Contracts.Rates;

namespace Tooba.Shipping.Infrastructure.Authoring;

/// <summary>
/// Read-only composition of the authoritative catalog bindings with the exact publication selection.
/// </summary>
/// <remarks>
/// <para>
/// A method is eligible for an offer owner/type only when (a) an explicit selected captured revision exists
/// for the exact owner + service/option binding, (b) that capture carries the same real shipping type, and
/// (c) the stable method identity (the immutable authoring draft id) is non-empty. Drafts, cleared pointers
/// and captures of another owner/type never surface.
/// </para>
/// <para>
/// Identity is the immutable authoring draft id returned by the selected-capture read, never the mutable
/// capture/snapshot id. This seam writes nothing and publishes nothing.
/// </para>
/// </remarks>
public sealed class ShippingOfferMethodLookup(
    IShippingCatalogReader catalog,
    IShippingOneLevelCanonicalMethodReader canonical,
    IShippingSelectedCapturedRevisionLookup selectedCaptures,
    ILanguageLookup? languages = null) : IOfferShippingMethodLookup
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<OfferShippingMethodItem>> ListAsync(
        OfferShippingMethodQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        if (query.ShippingTypeId == Guid.Empty)
        {
            return [];
        }

        var bindings = await ResolveBindingsAsync(cancellationToken);
        if (bindings.Count == 0)
        {
            return [];
        }

        var languageId = await ResolveLanguageIdAsync(query.LanguageCode, cancellationToken);
        var result = new List<OfferShippingMethodItem>(bindings.Count);
        var seen = new HashSet<Guid>();

        foreach (var (serviceCode, optionCode) in bindings)
        {
            var pointer = await selectedCaptures.ReadAsync(
                new ShippingSelectedCapturedRevisionRequest(
                    query.OwnerKind,
                    query.OwnerTenantId,
                    query.OwnerSellerPartyId,
                    serviceCode,
                    optionCode),
                cancellationToken);

            if (pointer.Verdict != ShippingSelectedCaptureVerdict.Selected
                || pointer.DraftId is not Guid draftId
                || draftId == Guid.Empty
                || pointer.ShippingTypeId != query.ShippingTypeId
                || !seen.Add(draftId))
            {
                continue;
            }

            var resolvedService = pointer.ServiceCode ?? serviceCode;
            var resolvedOption = pointer.OptionCode;
            var label = ResolveLabel(pointer.Translations, languageId, resolvedService, resolvedOption);
            result.Add(new OfferShippingMethodItem(draftId, resolvedService, resolvedOption, label));
        }

        return result;
    }

    /// <summary>
    /// Real catalog bindings: every active option plus the distinct service-level (null option) identity,
    /// plus canonical one-level methods that have no legacy option row. Read-only and bounded.
    /// </summary>
    private async Task<IReadOnlyList<(string ServiceCode, string? OptionCode)>> ResolveBindingsAsync(
        CancellationToken cancellationToken)
    {
        var snapshot = await catalog.ListAsync(cancellationToken);
        var bindings = new List<(string ServiceCode, string? OptionCode)>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var service in snapshot.Where(service => service.IsActive))
        {
            var serviceCode = Normalize(service.Code);
            if (serviceCode is null)
            {
                continue;
            }

            foreach (var option in service.Options.Where(option => option.IsActive))
            {
                var optionCode = Normalize(option.Code);
                if (optionCode is not null && seen.Add($"{serviceCode}|{optionCode}"))
                {
                    bindings.Add((serviceCode, optionCode));
                }
            }

            if (seen.Add($"{serviceCode}|"))
            {
                bindings.Add((serviceCode, null));
            }
        }

        var canonicalMethods = await canonical.ListActiveUnboundAsync(cancellationToken);
        foreach (var method in canonicalMethods)
        {
            var serviceCode = Normalize(method.ServiceCode);
            var optionCode = Normalize(method.OptionCode);
            if (serviceCode is not null
                && optionCode is not null
                && seen.Add($"{serviceCode}|{optionCode}"))
            {
                bindings.Add((serviceCode, optionCode));
            }
        }

        return bindings;
    }

    private async Task<Guid?> ResolveLanguageIdAsync(string? code, CancellationToken cancellationToken)
    {
        if (languages is null || string.IsNullOrWhiteSpace(code))
        {
            return null;
        }

        var trimmed = code.Trim();
        var rows = await languages.ListAsync(cancellationToken);
        return rows.FirstOrDefault(row => string.Equals(row.Code, trimmed, StringComparison.OrdinalIgnoreCase))?.LanguageId;
    }

    private static string ResolveLabel(
        IReadOnlyList<ShippingSelectedCapturedTranslation> translations,
        Guid? languageId,
        string serviceCode,
        string? optionCode)
    {
        if (languageId is Guid id)
        {
            var match = translations
                .FirstOrDefault(translation => translation.LanguageId == id
                    && !string.IsNullOrWhiteSpace(translation.Name));
            if (match is not null)
            {
                return match.Name.Trim();
            }
        }

        var fallback = translations.FirstOrDefault(translation => !string.IsNullOrWhiteSpace(translation.Name));
        if (fallback is not null)
        {
            return fallback.Name.Trim();
        }

        return optionCode is null ? serviceCode : $"{serviceCode}:{optionCode}";
    }

    private static string? Normalize(string? value)
    {
        var trimmed = value?.Trim();
        return string.IsNullOrEmpty(trimmed) ? null : trimmed;
    }
}
