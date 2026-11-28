using Tooba.Shipping.Contracts.Rates;

namespace Tooba.Shipping.Contracts.Authoring;

/// <summary>
/// Query for the published, active shipping methods of one trusted owner and shipping type.
/// </summary>
/// <remarks>
/// The owner fields are stable server-built data identity, never a caller authorization. The shipping type is
/// the real Fulfillment-owned type id; a method is only returned when the selected published capture of the
/// exact owner binding carries that same type. No "latest"/"current" fallback exists: only an explicit
/// publication selection is eligible.
/// </remarks>
/// <param name="OwnerKind">Resolved owner family.</param>
/// <param name="OwnerTenantId">Store tenant id for a SingleStore owner, otherwise <c>null</c>.</param>
/// <param name="OwnerSellerPartyId">Real seller id for a Marketplace seller owner, otherwise <c>null</c>.</param>
/// <param name="ShippingTypeId">Real shipping type id the method must be assigned to.</param>
/// <param name="LanguageCode">Active UI language code used to select the stored display name.</param>
public sealed record OfferShippingMethodQuery(
    ShippingCapturedQuoteOwnerKind OwnerKind,
    string? OwnerTenantId,
    Guid? OwnerSellerPartyId,
    Guid ShippingTypeId,
    string? LanguageCode = null);

/// <summary>
/// One published, active method available to an offer of the queried owner and shipping type.
/// </summary>
/// <param name="MethodId">Canonical stable method identity (the immutable authoring draft identity).</param>
/// <param name="ServiceCode">Owning service code of the selected capture.</param>
/// <param name="OptionCode">Owning option code, or <c>null</c> for a service-level method.</param>
/// <param name="Label">Display name resolved from the stored translation of the active language.</param>
public sealed record OfferShippingMethodItem(
    Guid MethodId,
    string ServiceCode,
    string? OptionCode,
    string Label);

/// <summary>
/// Read-only seam exposing the published, active eligible shipping methods for an offer's owner and type.
/// </summary>
/// <remarks>
/// This is a bounded read of an owner's own published methods. It writes nothing, never publishes, never
/// falls back to a draft/current revision, and never returns a method of another owner or type.
/// </remarks>
public interface IOfferShippingMethodLookup
{
    /// <summary>Published active methods of the queried owner and shipping type.</summary>
    /// <param name="query">Trusted owner identity and real shipping type.</param>
    /// <param name="cancellationToken">Caller token.</param>
    Task<IReadOnlyList<OfferShippingMethodItem>> ListAsync(
        OfferShippingMethodQuery query,
        CancellationToken cancellationToken);
}
