namespace Tooba.Offer.Contracts.Dtos;

/// <summary>One published, active method available to this offer's owner and shipping type.</summary>
/// <param name="MethodId">Canonical stable method identity.</param>
/// <param name="ServiceCode">Owning service code.</param>
/// <param name="OptionCode">Owning option code, or <c>null</c>.</param>
/// <param name="Label">Display name resolved from the active content language.</param>
public sealed record OfferShippingMethodOption(
    Guid MethodId,
    string ServiceCode,
    string? OptionCode,
    string Label);

/// <summary>
/// Bounded candidate list plus the offer's current persisted selection.
/// </summary>
/// <param name="Responsibility">Current explicit responsibility, or <c>null</c>.</param>
/// <param name="ShippingTypeId">Current real shipping type id, or <c>null</c>.</param>
/// <param name="Mode">Current selection mode (legacy reads as Automatic).</param>
/// <param name="SelectedMethodIds">Current persisted stable method identities.</param>
/// <param name="Methods">Published active eligible methods for the owner and type.</param>
public sealed record OfferShippingMethodOptionsPage(
    OfferFulfillmentResponsibility? Responsibility,
    Guid? ShippingTypeId,
    OfferShippingSelectionMode Mode,
    IReadOnlyList<Guid> SelectedMethodIds,
    IReadOnlyList<OfferShippingMethodOption> Methods);
