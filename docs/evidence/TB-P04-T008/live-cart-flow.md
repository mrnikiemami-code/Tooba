# TB-P04-T008 — Live cart flow

Guest secret is not recorded. Totals come from Host Pricing quotes, not `sessionStorage`.

## Product

| Field | Value |
| --- | --- |
| Slug | `workspace-live-shirt` |
| Title | پیراهن مردانه لینن |
| Market | IR / IRR |

## Offers (line identity = OfferId)

| Seller | OfferId | Unit (tax-exclusive) |
| --- | --- | --- |
| دیجی‌استایل نمونه | `01a030d1-4111-7000-8102-450cd76a8150` | 1 790 000 IRR |
| فروشگاه آرمان | `01a030d1-40f1-7000-95f6-b8efc58e2619` | 1 850 000 IRR |

Same Catalog product, two Cart lines. ProductId is presentation only.

## Guest cart

| Field | Value |
| --- | --- |
| CartId | `01a034b9-3a1c-7000-8b35-7f69fd0a363b` |
| Transport | `sessionStorage` keys `tooba.storefront.cartId` and `tooba.storefront.guestSecret` only |
| Version observed | 11 |

## Mutations (Host `/v1/storefront/cart*`)

1. **Add** from PDP `افزودن به سبد خرید` — Arman offer qty 1; badge and `/cart` refresh from GET.
2. **Update** — first session: qty 1 → 2 on Digistyle line; line total 1 790 000 → 3 580 000 IRR (screenshot `03`).
3. **Conflict** — PATCH qty 5 on Digistyle (stock 4): HTTP **409** `cart.inventory.insufficient` / «موجودی قابل‌فروش برای خط سبد کافی نیست.» UI also showed reservation copy: «فقط رزرو Held قابل آزادسازی یا مصرف است.» and later «آزادسازی رزرو با موجودی هم‌خوان نبود.»
4. **Remove** — Arman line removed successfully via UI. Digistyle qty 4 remove/decrease hit the same reservation mismatch (leftover Held from prior guest carts on this seed). Empty UI was then shown by clearing guest transport keys (not by completing that blocked remove).

## Summary truth

`itemCount` 5 = 4 + 1. Payable estimate 9 010 000 IRR = 7 160 000 + 1 850 000. No fake discount. Tax called out as Checkout-authoritative.

## Checkout boundary

CTA `ادامه به تسویه` → `/checkout` shell. No payment success.
