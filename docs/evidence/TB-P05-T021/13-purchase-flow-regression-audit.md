# 13 — Purchase Flow Regression Audit (before repair)

Sources: Shopeiva runtime captures `02`–`09`, Tooba before `10`–`12`, source inventory `01`.

| Area | Shopeiva | Tooba before | Severity | Plan |
| --- | --- | --- | --- | --- |
| Cart shell | Hero + breadcrumb + max density | Plain title, no hero | Material | Restore CartHero-equivalent + breadcrumb |
| Cart item | Dense card, no internal OfferId | Shows truncated OfferId | Material (trust/UX) | Hide customer-facing OfferId |
| Quantity / remove | ± chip + trash + footer actions | ± + trash only | Minor | Keep live mutate; optional remove link row |
| Summary | Sticky `lg:top-24` | Static aside | Material | Sticky |
| Coupon | Visible panel | Absent | Material | Honest unavailable panel |
| Cart shipping methods | 3 fake carriers in CartItems | Absent (good) | — | Keep absent / honest deferral |
| Stepper | Cart → Shipping → Payment in heroes | Tiny “۱. ارسال / ۲. پرداخت” pills | Material | Full stepper in checkout hero |
| Address cards | Toggle new/saved, bordered cards | Present but flatter | Minor→Material | Strengthen Shopeiva chrome |
| Payment panel | Method list + pay CTA language | Amber “no gateway” box | Material | Panel + honest pending/sandbox copy |
| Confirmation | Centered success card | Simple white cards | Material | Shopeiva-like card + live Paid truth |
| Mobile | Stacked; sticky summary below | Usable but sparse | Material | Match hero + stack order |
| Empty cart | Hero still shown + empty body | Empty only | Minor | Hero + empty |
| Home/PDP/Listing | Locked | Untouched | Guard | `test:critical-storefront` before/after |

Before captures on empty cart/checkout show storefront chrome only (no seeded lines) — still valid shell regression evidence; after captures will seed a live cart via PDP/addOfferToCart.
