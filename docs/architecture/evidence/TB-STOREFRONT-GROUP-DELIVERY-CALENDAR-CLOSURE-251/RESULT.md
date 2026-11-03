# RESULT — TB-STOREFRONT-GROUP-DELIVERY-CALENDAR-CLOSURE-251

Status: **PASS** within issued calendar closure + currency residual + canonical deploy scope  
Decision: **GROUP_DELIVERY_CALENDAR_VISIBLE_AND_CURRENCY_IDENTITY_GUARDS_DEPLOYED**  
NOT Architect ACCEPT, NOT Visual/User ACCEPT, NOT Tipax ACCEPT

Parent: `TB-STOREFRONT-CART-RIAL-TOTALS-AND-SELLER-GROUPING-250`  
Base-SHA: `7d916bc7a8154f77ff2ee734f0d78c6ca54459cd`  
Code-SHA: `CODE_SHA_PENDING`  
Evidence-SHA: `EVIDENCE_SHA_PENDING`  
Stop-State: `STOP_FOR_ARCHITECT`  
automaticNextImplementationTask: `NONE`

## Measured root cause

1. **Host CapturedPreview local gates were OFF** after 250 Host retain → shipping projection lacked `capturedPreview` (legacy only) so groups had no server calendars. Checked-in gates remain OFF; task restored **local** CapturedPreview env ON for Host5088 (operational, not checked-in flip).
2. **FE silent seam**: `decodeCapturedSlots` collapsed absent/non-array/malformed → `[]`; method row showed hours only when slots.length>0 with **no owning explanation**; Continue could leave date-chosen empty slots unexplained.

## Repair

- Decoder `slotsStatus`: `ok|absent|empty|malformed`; owning localized blocker; Continue blocked for that group when calendar unavailable/malformed.
- Currency residual: exact currency match for goods subtotal (never label USD as IRR); seller sections never sum mixed currencies.
- Preserve per-group independence; no invented frontend hours.

## Before → after (Active cart `01a1a1b2-…`)

| Group | Before (gates OFF / silent) | After |
|-------|-----------------------------|-------|
| آرمان `cap|…|01a030d1…|01a16f43…` | 0 captured calendars | 7 dates × 8 hourly slots |
| نیلوا `cap|…|01a0ffff…|01a16f43…` | 0 captured calendars | 7 dates × 8 hourly slots |

DOM (isolated CDP9371): both groups 7 date chips + 8 slot controls after method select; Continue blocker recipient-name only (not calendar). Snapshot `01a186f7-dd7a…` method پیشتاز post/null.

## Proof

Focused shipping/calendar/currency **79** + cart-money residual + calendar-251; currency node **17/17**; cart-mount **10/10**; typecheck/build **0**. No user checkout/payment/cart-line mutation.

## Canonical

FE PID **7632** :3000 → Host PID **23472** :5088 SingleStore/store-alpha/tooba_alpha. DataProtection preserved. No migration. Tipax deferred. Cart seller grouping (250) ≠ shipping calendar proof (disclosed).

## Unmet

- Visual/User ACCEPT not claimed  
- Live Tipax deferred  
- Checked-in CapturedPreview remains OFF (local Host env ON for this runtime)

STOP_FOR_ARCHITECT; automaticNextImplementationTask: NONE
