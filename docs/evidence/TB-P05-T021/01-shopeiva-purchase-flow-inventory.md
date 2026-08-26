# 01 — Shopeiva Purchase Flow Inventory

Accent note: Shopeiva uses `#E53935`; Tooba keeps established `#2563EB` (MINOR TECHNICAL DEVIATION, same as T017–T020).

| Source | Path | Structure / interaction | Tooba equivalent | Deviation (before) | Required action |
| --- | --- | --- | --- | --- | --- |
| Cart client | `components/cart/CartClient.jsx` | max-w 1800, breadcrumb, Hero, 2/3 items + 1/3 sticky summary, CartBottom | `storefront-cart.tsx` | Flat h1 + 8/4 grid; no hero/breadcrumb/sticky/coupon/benefits | Restore shell |
| CartHero | `CartHero.jsx` | red gradient banner + 3 metric cards (count / total / discount%) | missing | Title only | Add hero with honest discount=0 when no promo |
| CartItems | `CartItems.jsx` | image, title, qty ±, trash, line total, save-for-later, shipping method grid | line cards in cart | OfferId leaked; no bottom actions; fake multi-carrier would violate Tooba | Match row chrome; hide OfferId; **no fake carriers** — defer shipping truth to Checkout |
| CartSummary | `CartSummary.jsx` | sticky card, totals, CTA → `/shipping`, trust row | aside summary → `/checkout` | Not sticky; weaker trust row; CTA label | Sticky + Shopeiva hierarchy; CTA to live `/checkout` |
| CartCoupon | `CartCoupon.jsx` | code input + demo coupons | missing | — | Honest unavailable panel (no fake accept) |
| CartEmpty | `CartEmpty.jsx` | large bag icon + CTA + shortcut chips | empty state | Close; chips pointed at missing routes | Align copy; chips → `/products` only |
| CartBottom | `CartBottom.jsx` | 4 benefit tiles + product swiper from JSON | missing | — | Benefit tiles only (no fake JSON products) |
| Shipping client | `shipping/ShippingClient.jsx` | breadcrumb, ShippingHero+stepper, form + sticky summary | combined `storefront-checkout.tsx` | Tiny pill stepper; no hero; summary not sticky | Hero+Cart→Shipping→Payment stepper; sticky summary |
| ShippingForm | `ShippingForm.jsx` | new vs saved address toggle, fields, delivery slot UI | address + recipient sections | Visually flatter; no delivery-slot fake | Match address chrome; **no fake slots** |
| Payment client | `payment/PaymentClient.jsx` | PaymentHero+stepper, methods, card form, sticky pay CTA, success/fail cards | payment section + confirmation page | Amber text box only; confirmation plain | Payment panel honest (sandbox/pending); confirmation card like Shopeiva success shell |
| Confirmation | PaymentClient `result===success` | centered card, order code, amount, CTAs | `storefront-order-confirmation.tsx` | Flat cards | Match success/pending card geometry with live Host state |

## Live commerce bindings (locked)

- Cart line → OfferId (backend authority)
- AddressBook → Checkout ownership validation → immutable shipping snapshot
- Payment / Paid state from Host only
- Tax/discount/payable from Checkout preview/submit responses

## Explicit non-goals

- No redesign of Home / PDP / Listing
- No fake coupons, free shipping, multi-carrier prices, or PSP logos
- No Product.Price / Product.Stock authority
