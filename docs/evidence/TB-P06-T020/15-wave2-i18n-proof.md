# 15 — Wave 2 i18n / RTL-LTR proof (TB-P06-T020)

Date: 2026-08-27

## Scope

Touched Wave 2 panel surfaces: seller coupons list/form, seller reviews list, admin promotions DataGrid, existing admin reviews. No new translation architecture.

## Verification matrix

| Surface | Persian (fa) | English where catalog supports | RTL (fa) | LTR (en) |
|---|---|---|---|---|
| Seller shell + coupons / reviews | Primary chrome FA (`تخفیف‌ها`, `مدیریت نظرات`, status pills) | Panel chrome remains FA-first (same as Wave 1); no parallel EN vendor i18n invented | `vendor-shell.tsx` `dir="rtl"` | Coupon codes / numeric fields use local `dir="ltr"` islands only |
| Admin promotions / reviews | FA labels in shell + DataGrid | Existing admin FA baseline | Admin shell RTL baseline | IDs/codes LTR where needed |
| Storefront cart coupon apply | Existing storefront locale routes (`/fa`, `/en`) | EN storefront catalog where present | Locale-prefixed public routing (T016) | Unchanged |
| Customer notifications | N/A (deferred shell) | N/A | Deferred | Deferred |

## Rules followed

- **locale ≠ market** — coupon/promotion evaluation still uses market/currency axes on Host; panel UI does not treat locale as market.
- Locale-preserving internal panel links (relative `/vendor-panel/…`, `/admin/…`).
- No direction breakage: RTL shell preserved; mono coupon codes marked LTR.
- No duplicate i18n system for Wave 2 forms.

## Non-claims

- Full English string catalog for all vendor/admin chrome remains a broader readiness gap.
- Wave 2 did not productize locale-prefixed panel routes (`/fa/vendor-panel/…`).
