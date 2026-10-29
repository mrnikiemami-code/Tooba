# TB-STOREFRONT-NAVIGATION-LATENCY-AND-PENDING-FEEDBACK-245-R1 — WORKER RESULT

Status: **PASS** within scope. Stop-State: STOP_FOR_ARCHITECT. automaticNextImplementationTask: NONE.  
Worker ≠ Architect / Visual ACCEPT.

## A — shipping-choice invalidation (244-R3 correction)

**Before:** `reconcileCapturedChoicesAgainstPreview` retained method/date/slot even when coupon/cart/snapshot/rate/goods changed; zero-slot days could keep non-null ticks.

**After:**
- Routing identity `{destinationKey, couponCode, cartVersion}` stored beside choices; change clears **all** choices.
- Per-group `captureGroupConfirmationFingerprint` (group+service/option+snapshot+sourceRev+selVer+area+rate+goods+date+ticks); mismatch clears **that group only**.
- Zero-slot day: ticks required → reject; tickless legacy → keep when fingerprint matches.
- Recipient fields remain outside identity; Continue blockers do not mutate choices.

Focused RED→GREEN facts covered in `storefront-shipping-captured-selection.test.ts`.

**Canonical half:** cart `01a17b2d-e219…` Active v48 qty18 checkouts0 preserved on FE3000→Host5088. Live coupon pricing-context flip not exercised this slice (no merchant mutation); exact focused source facts for dest/coupon/cart/fingerprint cover that half — disclosed.

**Identity correction (R3 receipt):** actual buyer id is `01a0996c-b8d9-7000-9854-…`, not `…-8f5e-…`. Append-only note under 244-R3 evidence.

## B — navigation pending correctness (245 correction)

**Before:** `begin()` returned `false` for both modified clicks and duplicates → LocalizedLink `preventDefault` blocked Ctrl/Cmd/middle; pending reconciled only on `pathname` (query/hash/no-op could stick).

**After:** `resolveBeginNavigation` → `native | started | duplicate | same-location`; only `duplicate` prevents default. Path+search+hash reconcile, popstate+hashchange, 5s timeout clears lock and shows localized FA/EN retry. Hardcoded Persian-only live text removed.

**245 performance retained:** PDP no `/home`; detail `cache`; mini-cart soft Link; header memo/cancel.

## Timings (production `next start` :3000 → :5088)

| Journey | Pending | Usable / HTML |
| --- | --- | --- |
| Warm product click | **9ms** | **345ms** |
| Warm cart HTTP | — | **118ms** |
| Cold PDP HTML | — | **238ms** |
| Cold cart HTML | — | **117ms** |

Budgets: pending ≤100ms; warm ≤1s; cold ≤5s. Product click home API count remains 0 (245 retained).

## Proof

Focused 55/55 (nav + shipping-choice + continue + cart-ui guards); `npm run typecheck` clean; production build 0.

## SHAs

Expected-Base / Base: `7bb390682cddb8a02b9e41c38d9c8c96b6ca6d26` (issue tip after format correction; issue body `9f1462a97`)  
Code-SHA: `3f656e364acefae49af363fe27952486254066df`
Evidence-SHA: `3f656e364acefae49af363fe27952486254066df`
