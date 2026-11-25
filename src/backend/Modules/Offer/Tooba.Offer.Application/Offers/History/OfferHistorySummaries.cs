using System.Globalization;
using Tooba.Offer.Domain.Aggregates;
using Tooba.Offer.Domain.ValueObjects;

namespace Tooba.Offer.Application.Offers.History;

/// <summary>Stable Offer-owned change kinds and deterministic value summaries for recorded history.</summary>
internal static class OfferHistorySummaries
{
    public const string Created = "Created";
    public const string CommercialChanged = "CommercialChanged";
    public const string PriceChanged = "PriceChanged";
    public const string InventoryChanged = "InventoryChanged";
    public const string FulfillmentChanged = "FulfillmentChanged";

    public static OfferHistoryEntry CreateEntry(
        Guid entryId,
        Guid offerId,
        DateTimeOffset occurredAt,
        string changeKind,
        string summary,
        string? oldValue,
        string? newValue,
        Guid? actorUserId = null) =>
        new()
        {
            OfferHistoryEntryId = entryId,
            OfferId = offerId,
            OccurredAt = occurredAt,
            ChangeKind = changeKind,
            ActorUserId = actorUserId,
            Summary = summary,
            OldValue = oldValue,
            NewValue = newValue,
        };

    public static string StatusChannel(SellerOffer offer) =>
        $"status={offer.Status};channel={offer.Channel}";

    public static string Commercial(SellerOffer offer) =>
        $"sku={offer.SellerSku ?? "null"};status={offer.Status};return={offer.ReturnPolicyChoice}"
        + $";returnDays={offer.CustomReturnWindowDays?.ToString(CultureInfo.InvariantCulture) ?? "null"}"
        + $";min={Decimal(offer.MinimumOrderQuantity)};max={Decimal(offer.MaximumOrderQuantity)}";

    public static string Price(decimal amount, string currency) =>
        $"amount={Decimal(amount)};currency={currency}";

    public static string InventoryLevel(decimal onHand) => $"onHand={Decimal(onHand)}";

    public static string Inventory(Guid? stockItemId, Guid? locationId, decimal onHand, string? reason) =>
        $"stockItemId={stockItemId?.ToString() ?? "null"};location={locationId?.ToString() ?? "null"}"
        + $";onHand={Decimal(onHand)};reason={reason ?? "null"}";

    public static string Fulfillment(FulfillmentResponsibility? responsibility, Guid? shippingTypeId) =>
        $"responsibility={responsibility?.ToString() ?? "null"};type={shippingTypeId?.ToString() ?? "null"}";

    private static string Decimal(decimal? value) =>
        value?.ToString("0.######", CultureInfo.InvariantCulture) ?? "null";
}
