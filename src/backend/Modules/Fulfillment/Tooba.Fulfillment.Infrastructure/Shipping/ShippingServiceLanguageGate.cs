using Tooba.Fulfillment.Application.Errors;
using Tooba.Fulfillment.Application.Shipping;
using Tooba.Fulfillment.Contracts.Errors;
using Tooba.Localization.Contracts.Errors;
using Tooba.Localization.Contracts.Ports;

namespace Tooba.Fulfillment.Infrastructure.Shipping;

/// <summary>
/// Fulfillment-owned language gate for shipping-service writes/seed.
/// Depends only on <see cref="ILanguageLookup"/> (Localization.Contracts).
/// </summary>
public sealed class ShippingServiceLanguageGate : IShippingServiceLanguageGate
{
    private readonly ILanguageLookup _languages;

    /// <summary>Creates the language gate.</summary>
    public ShippingServiceLanguageGate(ILanguageLookup languages) => _languages = languages;

    /// <inheritdoc />
    public async Task EnsureKnownAsync(IReadOnlyList<Guid> languageIds, CancellationToken cancellationToken)
    {
        var known = (await _languages.ListAsync(cancellationToken)).Select(x => x.LanguageId).ToHashSet();
        if (languageIds.Any(id => !known.Contains(id)))
        {
            throw FulfillmentErrors.SemanticFault(FulfillmentErrorCodes.ShippingServiceLanguageInvalid);
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ShippingServiceSeedLanguage>> ListForSeedAsync(CancellationToken cancellationToken)
    {
        var langs = await _languages.ListAsync(cancellationToken);
        return langs.Select(x => new ShippingServiceSeedLanguage(x.LanguageId, x.Code, x.Culture, x.IsDefault)).ToList();
    }
}
