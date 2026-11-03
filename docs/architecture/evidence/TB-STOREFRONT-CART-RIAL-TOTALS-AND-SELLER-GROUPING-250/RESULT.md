# RESULT — TB-STOREFRONT-CART-RIAL-TOTALS-AND-SELLER-GROUPING-250

Status: **PASS** within issued cart money/grouping + mounted proof + canonical deploy scope  
Decision: **CART_TOTALS_BY_CURRENCY_IRR_INTEGER_AND_SELLER_SECTIONS_DEPLOYED**  
NOT Architect ACCEPT, NOT Visual/User ACCEPT, NOT Tipax ACCEPT

Parent: `TB-SHIPPING-ORDER-GROUP-CARRIER-EXECUTION-249-R8`  
Base-SHA: `4ce605905af623edcbce96ca5c46decfdc2d4817`  
Code-SHA: `CODE_SHA_PENDING`  
Evidence-SHA: `EVIDENCE_SHA_PENDING`  
Stop-State: `STOP_FOR_ARCHITECT`  
automaticNextImplementationTask: `NONE`

## Root cause (measured)

Host cart DTO exposes goods money via `totalsByCurrency[]` + `defaultCurrency`, not scalar `subtotalExclusiveOfTax`. FE `mapStorefrontCart`/`?? 0` treated missing scalar as **0** while priced lines rendered. `formatOfferAmount` used `Intl.NumberFormat("fa-IR")` without currency places → IRR fractional glyphs. Cart UI listed flat lines with per-line seller labels (no seller sections).

## Repair

- Map authoritative goods total from `totalsByCurrency` for cart currency; missing/invalid → unavailable/pending (never silent 0); lawful empty zero preserved. Pre-shipping labels identify goods totals (not final payable).
- `formatOfferAmount` mirrors `FinancialRounder.MoneyPlaces` (IRR/JPY/KRW → 0 places). Non-IRR retain 2. No toman relabel.
- Group lines by stable `sellerPartyId`; one section header + optional seller subtotal; same-name distinct IDs separate; unknown never merge. Not shipping groups.
- Refresh generation/version guard; no fetch loops.

## Proof

- Focused money+API **21/21**; `test:cart-mount` **10/10**; typecheck **0**.
- Canonical Active cart `01a1a1b2-0c5a-7000-8b82-2cc2e119dbe9` v22 READ-ONLY: Host IRR total **13824519.72** → display **۱۳٬۸۲۴٬۵۲۰ ریال**; DOM `cart-seller-section` **2** (آرمان 2 lines + نیلوا 1); no IRR fractional glyph. Expired `01a17b2d` still readable IRR **74689416.16**. No cart/checkout mutation.

## Canonical

FE PID **29912** :3000 → Host PID **18428** :5088 SingleStore/store-alpha/tooba_alpha. DataProtection preserved. No migration. Tipax remains deferred (249 chain).

## Unmet

- Visual/User ACCEPT not claimed  
- Live Tipax deferred (external-only; unrelated to this cart slice)

STOP_FOR_ARCHITECT; automaticNextImplementationTask: NONE
