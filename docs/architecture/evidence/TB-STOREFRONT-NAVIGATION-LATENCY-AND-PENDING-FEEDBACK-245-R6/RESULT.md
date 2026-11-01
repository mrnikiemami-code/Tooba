# TB-STOREFRONT-NAVIGATION-LATENCY-AND-PENDING-FEEDBACK-245-R6 — WORKER RESULT

Status: **PASS** within scope. Stop-State: STOP_FOR_ARCHITECT. automaticNextImplementationTask: NONE.  
Worker ≠ Architect / Visual ACCEPT.

## Defect

Successful projections copied `page.revalidationMessage` into the long-lived operational `error` state. Lawful method/date/slot reselection updated `capturedChoices` but left the stale red banner beside a valid R5 summary. Fee rows showed only the shared method label («پیشتاز»), so two equal fees were indistinguishable.

## Repair

- Stop copying revalidation into `error`.
- New pure `resolveCapturedRevalidationAdvisory`: owned by current projection message + confirmed preview + choices; suppressed when `capturedSelectionComplete`; names owning seller via `sellerPartyId` when a group still needs reselect; unconfirmed destination suppresses advisory.
- Separate DOM `data-testid="shipping-revalidation-advisory"`; network/save errors stay on `shipping-error`.
- Extend R5 fee rows with `sellerPartyId`; `formatCapturedFeeRowLabel` → `«فروشنده» — «روش»` (localized group fallback, no GUID).

## Proof

Focused: revalidation + summary + selection + continue-state + destination-choice/refresh + navigation/cart guards → **80/80**; typecheck clean; production build exit 0.

Live FE3000→Host5088 cart `01a17b2d…` Active v48 qty18:

| Fact | Value |
| --- | --- |
| Before reselect | advisory names فروشگاه آرمان + stale message; `shipping-error` null |
| After both method/date/slot | advisory **null**; shipping ۲۵۰,۰۰۰; fees آرمان/نیلوا — پیشتاز ۱۲۵,۰۰۰ each |
| Recipient incomplete | only Continue blocker «نام و نام خانوادگی…» |
| Partial one-group | summary `partial`; advisory names remaining seller |
| No selection PUT / checkout / payment | |

## SHAs

Expected-Base: `a70afe3c5a352c0ad233e6ac4632f504e39dda82`  
Base (issue FF): `b4d6cedf4325620d789035b1e470db54124590f6`  
Code-SHA: `99fc13663ff03b30265f9635d718db96f5b28a7f`  
Evidence-SHA: (filled in pin commit)
