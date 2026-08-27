# 16 — Native-fit map (TB-P06-T020)

Date: 2026-08-27  
Principle: exact Shopeiva reuse where present; otherwise source-derived native fit from closest Vendor/Admin patterns. `VISUAL_CONTRACT = SHOPEIVA_LOCKED`.

## Coupons / promotions

| Tooba | Shopeiva source | Reused geometry / behavior |
|---|---|---|
| `app/vendor-panel/coupons/coupons-list.tsx` | `components/vendor/panel/coupons/couponsList.jsx` | Tag icon chip `#E53935`, title **تخفیف‌ها**, stats `grid-cols-3`, search/filter, card grid `md:grid-cols-2`, emerald/red status pills, pagination 4/page |
| `app/vendor-panel/coupons/coupon-form.tsx` | `components/vendor/panel/coupons/couponForm.jsx` | `max-w-2xl` card, gradient header tint, mono uppercase code, discount kind select, date fields, amber tip, Back + primary Save |
| `/vendor-panel/coupons`, `/new`, `/[id]/edit` | Vendor coupons routes | List + create; edit for Draft/Expired only (Shopeiva edit route was broken — Tooba supplies working edit) |
| `AdminPromotionsScreen` (`admin-screens.tsx`) | Accepted Admin DataGrid / reviews-stories shell | Moderation-group list, status + seller owner columns, deactivate action — no foreign dashboard chrome |

### Intentional honesty vs Shopeiva mock

- No fake uses/maxUses progress bars (Shopeiva list had mock usage).
- No fake stats beyond Host-backed counts when present.

## Reviews

| Tooba | Shopeiva source | Reused geometry / behavior |
|---|---|---|
| `app/vendor-panel/vendor-reviews-ui.tsx` | `components/vendor/panel/reviews/reviewsList.jsx` | Star chip, **مدیریت نظرات**, emerald published pill, stats `grid-cols-3`, search + status filter, status card borders, amber stars, 4/page pagination |
| `/vendor-panel/reviews` | Vendor reviews page | Live list replaces prior `VendorCapabilityShell` stub |
| `/admin/reviews` | Existing Tooba Admin reviews (pre-Wave 2) | Unchanged DataGrid moderation queue |

### Intentional honesty vs Shopeiva mock

- Seller Approve / Reject / Delete **omitted** (admin owns moderation; seller read-only).
- No seller reply UI while `SellerResponseSupported: false`.

## Notifications

| Tooba | Shopeiva source | Action |
|---|---|---|
| Customer deferred shell | `dashboard/notifications/notifications.jsx` | **Not ported** — Option C defer |
| Seller inbox | MISSING in Shopeiva Vendor | **Not invented** |

## Shared tokens

| Concern | Pattern |
|---|---|
| Accent | `#E53935` (seller coupons/reviews) matching Shopeiva Vendor |
| Admin shell | Existing Tooba admin blue / DataGrid vocabulary (accepted minor brand deviation) |
| Cards / borders | `rounded-2xl`, status emerald/amber/red pills |
| Motion | Existing hover/focus on nav + CTAs; no new carousel |

## Verdict

Coupons + reviews map to exact Shopeiva Vendor sources; admin promotions derive from accepted Admin shell; notifications not visually invented.
