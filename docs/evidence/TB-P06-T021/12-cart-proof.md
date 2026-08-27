# 12 — Cart proof (TB-P06-T021)

## Routes

| Surface | URL / API |
|---|---|
| Cart UI | `http://127.0.0.1:3000/fa/cart` |
| Host | `POST /v1/storefront/cart`, `GET /v1/storefront/cart/{cartId}`, `POST …/lines`, `PATCH …/lines/{lineId}` |

FE: `src/frontend/app/cart/page.tsx` → `storefront-cart.tsx`  
Composer: `StorefrontCartComposer.cs` · foundations: `CartFoundationTests.cs`

## Required behaviors

| Behavior | Status | Notes |
|---|---|---|
| Seller Offer identity retained | LIVE | Lines keyed by **OfferId** (not Product.Price scalar) |
| Quantity | LIVE | Line qty patch; capped by availability on PDP/ATC |
| Variant / option identity | LIVE | Cart retains Offer bound to `CatalogVariantId` |
| Price snapshot / quote | LIVE | Amount from Pricing lookup at add/refresh — authoritative owner |
| Coupon | LIVE | Real Promotion codes from T020; applied at checkout preview/submit (cart may surface code field per Shopeiva cart UX) |
| Totals | LIVE | Host-composed; no fake client-only authoritative total |
| Insufficient stock | LIVE | Fail-closed via Inventory availability / reserve on checkout |
| No fake line data | LIVE | Panel honesty: empty cart = empty, not mock SKUs |

## Link to seller-authored merchandise

ATC from PDP for Offers created via `/vendor-panel/products/new` (price + inventory written) uses the same cart endpoints as seed Offers.

## Verdict

```text
CART = LIVE
```
