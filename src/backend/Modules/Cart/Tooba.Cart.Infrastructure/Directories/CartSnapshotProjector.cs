using CartContract = Tooba.Cart.Contracts;
using Tooba.Cart.Domain.Aggregates;
using Tooba.Inventory.Contracts.Availability;

namespace Tooba.Cart.Infrastructure.Directories;

/// <summary>
/// Projects the Cart aggregate into the stable <see cref="CartContract.CartSnapshot"/> boundary contract.
/// Owns the only Cart -> Contracts projection and the only availability batch read.
/// </summary>
internal sealed class CartSnapshotProjector(IInventoryAvailabilityGateway availability)
{
    /// <summary>Builds a Cart snapshot enriched with business line availability.</summary>
    public async Task<CartContract.CartSnapshot> ToSnapshotAsync(ShoppingCart cart, CancellationToken cancellationToken)
    {
        var offerIds = cart.Lines.Select(x => x.OfferId).Distinct().ToArray();
        var stock = offerIds.Length == 0
            ? new Dictionary<Guid, InventoryAvailability>()
            : await availability.GetAvailabilityBatchAsync(offerIds, cancellationToken);
        return new CartContract.CartSnapshot(
            cart.CartId,
            (CartContract.CartStatus)(int)cart.Status,
            (CartContract.CartAccessKind)(int)cart.AccessKind,
            cart.OwnerUserId,
            cart.Market,
            cart.DefaultCurrency,
            cart.Channel,
            cart.ExpiresAt,
            (CartContract.CartConversionIntent)(int)cart.ConversionIntent,
            cart.Version,
            // ترتیب خطوط قرارداد است، نه ترتیب فیزیکی ردیف‌ها. بدون ORDER BY، PostgreSQL ردیف‌ها را
            // به ترتیب heap برمی‌گرداند و هر UPDATE (تغییر تعداد) نسخهٔ تازه را به انتهای صفحه می‌برد؛
            // نتیجه این بود که همان خطِ تغییرکرده در UI به آخر لیست می‌پرید. LineId از UuidV7 است، پس
            // این ترتیب هم قطعی است و هم با ترتیب افزودن به سبد یکی است.
            cart.Lines.OrderBy(line => line.LineId).Select(line =>
            {
                stock.TryGetValue(line.OfferId, out var availabilityForLine);
                var available = availabilityForLine?.Available ?? 0;
                var kind = available >= line.Quantity
                    ? CartContract.CartLineAvailabilityKind.Available
                    : available > 0
                        ? CartContract.CartLineAvailabilityKind.LimitedQuantity
                        : CartContract.CartLineAvailabilityKind.Unavailable;
                return new CartContract.CartLineSnapshot(
                    line.LineId,
                    line.OfferId,
                    line.CatalogVariantId,
                    line.SellerPartyId,
                    line.Quantity,
                    line.ReservationId,
                    line.QuotedAmount,
                    line.QuotedCurrency,
                    line.QuotedTaxExclusive,
                    line.PriceId,
                    line.QuotedAt,
                    kind,
                    line.MerchandisingCampaignId);
            }).ToList());
    }
}
