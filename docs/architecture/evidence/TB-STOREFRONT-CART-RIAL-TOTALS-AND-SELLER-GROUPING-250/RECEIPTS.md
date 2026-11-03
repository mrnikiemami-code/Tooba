# RECEIPTS — TB-STOREFRONT-CART-RIAL-TOTALS-AND-SELLER-GROUPING-250

## Identity

- Task: `TB-STOREFRONT-CART-RIAL-TOTALS-AND-SELLER-GROUPING-250`
- Bridge row: `3bdbb075-f387-4f35-a7a0-c5235b3d2818`
- Base-SHA: `4ce605905af623edcbce96ca5c46decfdc2d4817`
- ClaimedAtUtc: `2026-11-03T14:02:45.6120266`
- Channel: `tooba-main` / Worker: `tooba-worker-01`

## Before / after money (sanitized)

| Cart | Before FE | After Host root IRR | After display |
|------|-----------|---------------------|---------------|
| Active `01a1a1b2-…` v22 | silent 0 (missing scalar) | 13824519.72 via totalsByCurrency | ۱۳٬۸۲۴٬۵۲۰ ریال |
| Expired `01a17b2d-…` v49 | silent 0 | 74689416.16 | integer IRR glyph |

Seller sections live DOM: **2** (آرمان `01a030d1-…` 2 lines; نیلوا `01a0ffff-…` 1 line).

Rounding authority: backend `FinancialRounder.MoneyPlaces` (IRR/JPY/KRW=0 else 2); FE `storefront-money.ts`.

## Tests

| Suite | Result |
|-------|--------|
| storefront cart-api + money | 21 passed |
| `npm run test:cart-mount` | 10 passed |
| FE typecheck / production build | 0 / 0 |

## Canonical runtime

- Host PID **18428** :5088/health 200
- FE PID **29912** :3000/fa 200
- SingleStore / store-alpha / tooba_alpha
- DataProtection key-d82e31a6 preserved
- Mutations: none
- Migrations: none

## Code-SHA / Evidence-SHA

- Code-SHA: f5570306bf8cc2244b30b34dcf6792f5ece82dac
- Evidence-SHA: 9b756f81e092657648db54ee7db9ec5b62a533b8
