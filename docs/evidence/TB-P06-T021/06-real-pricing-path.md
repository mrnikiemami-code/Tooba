# 06 — Real Pricing path (TB-P06-T021)

## Rule

No `Product.Price`. Seller writes tax-exclusive base amount through `IPriceDirectory`.

## HTTP

| Method | Path | Body |
|---|---|---|
| POST | `/v1/seller/offers/{offerId}/price` | `{ amount, currency?, market? }` |
| PUT | `/v1/seller/offers/{offerId}/price` | same |

Defaults (store-alpha demo): Market=`IR`, Currency=`IRR`, Channel from Offer (`Marketplace`).

## Composer flow (`SetOfferPriceAsync`)

1. `RequireOwnedOfferAsync` — foreign OfferId → 404 `seller.offer.missing`
2. Reject `amount < 0`
3. If no Active/Draft price for market/channel/currency (or Retired): `CreatePriceAsync` → `ActivateAsync`
4. Else: `ChangeAmountAsync`; Activate if still Draft

## Consumers

Storefront listing/PDP/cart/checkout continue to resolve via `IPriceLookupGateway` / composed Offer amount — same authored Pricing rows.

## UI

Vendor Offer create + detail edit price field posts to live Pricing write API (not a Product scalar).

## Tests

`SellerOfferSaleWriteTests`: own price write allow; foreign seller deny 404.
