# TB-STOREFRONT-NAVIGATION-LATENCY-AND-PENDING-FEEDBACK-245-R5 — WORKER RESULT

Status: **PASS** within scope. Stop-State: STOP_FOR_ARCHITECT. automaticNextImplementationTask: NONE.  
Worker ≠ Architect / Visual ACCEPT.

## Defect

`storefront-shipping.tsx` hard-coded captured summary shipping and payable to `—` while `capturedPreviewActive`, with a note that the final amount is calculated only after completion. Authoritative method fees (`payableAtCheckoutAmountText`) and group goods (`finalGoodsAmountText`) were already present on the confirmed preview.

## Repair

- New pure projector `storefront-shipping-captured-summary.ts`:
  - exact decimal BigInt addition (`addExactDecimalTexts` / `sumExactDecimalTexts`); no `Number`/`parseFloat`/exponents;
  - match methods by service/option only;
  - `ready` whole-cart totals only when every group has a lawful method;
  - `partial` shows selected fee rows + localized note (no fake cart total);
  - `hidden` while destination unconfirmed (245-R3); `unavailable` on invalid/mismatched currency/amount;
  - carrier collect summed separately and never added into payable; exact-zero collect omitted from display total.
- Summary DOM wired to projector with testids `shipping-summary-goods|shipping|payable`, fee rows, notes.
- `resolveShippingContinueState` remains the sole Continue authority (recipient incompleteness still blocks).

## Proof

Focused: captured-summary + captured-selection + continue-state + destination-choice/refresh + navigation-pending/cart-navigation/cart-ui → **73/73**; typecheck clean; production build exit 0.

Live FE3000→Host5088 (buyer `01a0996c…`, cart `01a17b2d…` Active v48 qty18):

| Fact | Value |
| --- | --- |
| Recipient incomplete | Continue disabled; blocker «نام و نام خانوادگی گیرنده را کامل کنید.» |
| After both methods/dates/slots | shipping `۲۵۰,۰۰۰ ریال`; payable `۸۱,۶۶۱,۴۶۴ ریال`; goods `۸۱,۴۱۱,۴۶۴ ریال` |
| Fee rows | two × پیشتاز `۱۲۵,۰۰۰ ریال` |
| Pending destination | note «مبالغ ارسال پس از تأیید مقصد…»; money hidden |
| After filling first/last name | amounts unchanged |
| Selection PUT / checkout / payment | 0 |

Exact projector on live decoded Host projection: shipping `250000.00`, goods `81411464`, payable `81661464.00`.

## SHAs

Expected-Base: `caf49ef762a04c8bb83a25a86a1294e7b81b0c57`  
Base (issue/docs FF at claim): `d6e07528022ed998aa19e6062f5e84b7081b7b2b`  
Code-SHA: `44c82ae030b16455df2a2495bdef7a71b3bd07a0`  
Evidence-SHA: (pin commit after this RESULT)
