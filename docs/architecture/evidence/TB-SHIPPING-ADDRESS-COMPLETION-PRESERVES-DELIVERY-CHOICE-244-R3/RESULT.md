# TB-SHIPPING-ADDRESS-COMPLETION-PRESERVES-DELIVERY-CHOICE-244-R3 — WORKER RESULT

Status: **PASS** within scope. Stop-State: STOP_FOR_ARCHITECT. automaticNextImplementationTask: NONE.  
Worker result ≠ Architect / Visual / PSP acceptance.

## Measured clearing trigger

Root cause: on every `capturedSelectionContextKey` change, `storefront-shipping.tsx` wiped **all**
`capturedChoices` to `{}`. Recipient/contact/postal/notes are not part of that key, but any
routing/preview refresh (including Geography destination completion after province/city, or a
semantically identical re-projection) still changed the key and erased already-chosen date+hour.
Meanwhile destination `pending` also emptied card groups visually. Completing name/postal fields
often coincided with that wipe, so the user saw day/hour disappear.

## Final invalidation model

- Routing/pricing identity remains `capturedSelectionContextKey` (destination+coupon+cartVersion+server
  preview calendar/confirmation fingerprint). Recipient form completeness is **outside** that identity.
- On identity change: `reconcileCapturedChoicesAgainstPreview` keeps per-group method+date+slot when
  that exact choice still exists in the new calendar; clears **only** groups whose date/slot left.
- `resolveShippingContinueState` still reports incomplete address without mutating choices.
- Genuine province/city/coupon/cart/quote changes still refresh once and hide stale cards while pending.

## Focused proof

- `storefront-shipping-captured-selection.test.ts` + continue/destination suites: **41/41**
- `npm run typecheck`: clean
- New facts: recipient fields outside context key; reconcile keeps valid slots; per-group invalid slot
  clear; Continue blockers do not require clearing choices; routing key still changes on dest/coupon/cart

## Canonical actual-cart acceptance

| Fact | Value |
| --- | --- |
| Runtime | FE `3000` → Host `5088` / SingleStore / store-alpha / `tooba_alpha` |
| User / cart | `01a0996c-b8d9…` / `01a17b2d-e219…` Active v48 qty18 |
| Non-routing edits | first/last/mobile/address/postal/notes → dates+slots **retained**, `__projCount=0` |
| After province+city | pending once, then same 14:00–15:00 retained on both groups; Continue enabled without reselect |
| Selection | one PUT → payment `http://127.0.0.1:3000/fa/payment?cartId=01a17b2d-e219…` |
| Draft | selection_revision **3**, shipping 250000, windows 14:00-15:00 ×2 |
| Submissions | checkouts **0**; cart Active unsubmitted |
| 244-R2 merchant config | preserved (Platform/500g/rate coverage) |

## SHAs

Expected-Base-SHA / Base-SHA: `e9495b0388dae6e878cb8349bb58e25be8a69e18`  
Code-SHA: *(this implementation commit)*  
Evidence-SHA: *(same evidence commit or follow-up pin)*  
local==origin/main after push.

## Touched files

- `src/frontend/app/storefront/storefront-shipping-captured-selection.ts` — `reconcileCapturedChoicesAgainstPreview`
- `src/frontend/app/storefront/storefront-shipping.tsx` — use reconcile instead of blanket `{}`
- `src/frontend/app/storefront/storefront-shipping-captured-selection.test.ts` — retention facts
