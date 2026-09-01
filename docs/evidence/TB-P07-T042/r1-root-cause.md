# TB-P07-T042-R1 — Root cause

Task: `TB-P07-T042-R1` (Order Detail Financial Data Wiring Repair)

## Symptom

Paid checkout showed correct top-level `payableAmount` and payment state, but:

- `lineCount` / `sellerCount` = 0
- Seller financial breakdown empty
- Customer receipt / financial summary zeros
- Financial history empty
- Payment card showed raw `wallet` / pipe-delimited provider reference

## Cause

1. **Stale Host runtime** — `:5088` was serving a pre-T042 `AdminOrderDetailPage` DTO (no `LineCount`, `SellerFinancials`, `FinancialEvents`, `FinancialSummary`). Frontend mapped missing ints as `0` and empty lists.
2. **Frontend projection gap** — `mapAdminOrderDetail` did not synthesize finance projections from `sellerOrders` + `payment` when backend fields were absent.
3. **Provider display** — operational payment snapshot exposed raw `providerCode` / internal wallet pipe reference without humanization.

## Fix strategy

- Keep T042 visual baseline and user-directed commits (`9d350a9a`) untouched.
- Repair data projection/mapping only (backend enrich + frontend `enrichAdminOrderDetail` fallback).
- Humanize provider labels; show `—` for unsettled commission instead of misleading zero.
