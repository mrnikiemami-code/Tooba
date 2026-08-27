# 07 — Real Inventory path (TB-P06-T021)

## Rule

No `Product.Stock`. Seller adjusts sellable stock through `IInventoryDirectory` on Offer positions.

## HTTP

| Method | Path | Body |
|---|---|---|
| POST | `/v1/seller/offers/{offerId}/inventory` | `{ onHand, reason? }` |
| PUT | `/v1/seller/offers/{offerId}/inventory` | same |

## Composer flow (`SetOfferInventoryAsync`)

1. `RequireOwnedOfferAsync` — foreign → 404 `seller.offer.missing`
2. Reject `onHand < 0`
3. If no position: resolve Active location (existing WH-* or create `SELLER-DEFAULT`) → `OpenPositionAsync`
4. `AdjustAsync(..., StockAdjustmentKind.Set, onHand, reason)`

Default reason when omitted: `seller-panel-adjust`.

## Compatibility

Checkout reservation / consume / release paths unchanged; they already operate on Offer stock positions.

## UI

Vendor create + detail set on-hand via live Inventory write API.

## Tests

`SellerOfferSaleWriteTests`: own inventory Set allow; foreign deny 404; foreign mutate does not change owner quantities.
