# 10 — Seller settlement endpoints (TB-P06-T012)

## Route group

`/v1/seller` — auth via `SellerPanelAccess.RequireAuthorizedAsync`

## Endpoints

| Method | Path | Purpose |
|---|---|---|
| GET | `/settlement/balance` | Seller available balance + ledger totals |
| GET | `/settlement/entries` | Posted accrual/adjustment entries |
| GET | `/settlement/statements` | Periodic statements |
| GET | `/settlement/payout-requests` | Seller payout history |
| POST | `/settlement/payout-requests` | Request withdrawal from available balance |

## Response shapes (JSON)

Balance: `settlementAccountId`, `sellerPartyId`, `currency`, `postedCredits`, `postedDebits`, `reservedPayouts`, `availableBalance`

Entry row: `entryId`, `entryType`, `grossAmount`, `commissionAmount`, `netAmount`, `sourceType`, `sellerOrderId`, `postedAt`

## Error codes

- `settlement.account.missing` — 404 when no account for seller
- `settlement.payout.rejected` — 400 on invalid withdrawal (insufficient balance, etc.)

## Implementation

`SettlementEndpoints.cs` + `SettlementPanelComposer.cs` in `src/backend/Host/Tooba.Host/Settlement/`

Mapped in `Program.cs` via `app.MapSettlementEndpoints()`.

## Dev headers

Frontend sends `X-Tooba-Seller-Party-Id` and dev actor header (same pattern as other seller panel APIs).
