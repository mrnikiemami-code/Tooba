# 13 — Checkout proof (TB-P06-T021)

## Routes

| Surface | URL / API |
|---|---|
| Checkout UI | `http://127.0.0.1:3000/fa/checkout` |
| Host | `POST /v1/storefront/checkout/preview`, `POST /v1/storefront/checkout`, `GET /v1/storefront/checkout/{checkoutId}` |

FE: `src/frontend/app/checkout/page.tsx` → `storefront-checkout.tsx`  
Composer: `StorefrontCheckoutComposer.cs` · Order foundations prior P04/P05.

## Required behaviors

| Concern | Status | Notes |
|---|---|---|
| Buyer identity | LIVE | Authenticated customer session / guest rules per current checkout |
| Selected Offer | LIVE | Snapshot of OfferId + seller party on lines |
| Quantity | LIVE | From cart lines |
| Shipping / address | LIVE | Saved-address selection (T014) where flow requires |
| Promotion / Coupon | LIVE | `couponCode` on preview/submit → Promotion evaluator (T020) |
| Pricing | LIVE | Tax-exclusive base from Pricing; tax composed separately |
| Tax | LIVE | Tax module calculation on preview/submit |
| Total | LIVE | **Host-authoritative** — UI must not invent totals |
| Inventory interaction | LIVE | Reserve on submit; insufficient stock fail-closed |
| Idempotency | LIVE | Checkout create semantics per Order foundation |
| PendingPayment draft | LIVE | Order/checkout enters pending-payment awaiting PSP |

## Explicit non-shortcuts

- No Product.Price / Product.Stock on checkout lines
- Coupon must be real Promotion row (T020), not a hard-coded discount string
- Seller-authored Offer price/stock must already be Active/available (else preview/submit reject)

## Verdict

```text
CHECKOUT = LIVE
```
