# 11 — Admin settlement endpoints (TB-P06-T012)

## Route group

`/v1/admin` — auth via `AdminPanelAccess.RequireAuthorizedAsync`

## Endpoints

| Method | Path | Purpose |
|---|---|---|
| GET | `/settlement/balances` | All seller settlement balances |
| GET | `/settlement/payout-queue` | Pending/processing payout requests |
| POST | `/settlement/payout-requests/{payoutRequestId}/process` | Process payout via gateway |
| POST | `/settlement/payout-requests/{payoutRequestId}/retry` | Retry failed payout |

## Admin operations

- **Process**: transitions request to Processing, calls `IPayoutGateway`, marks Succeeded/Failed
- **Retry**: re-attempts failed payout with same idempotency safeguards

## Authorization

SpiceDB ReBAC admin panel gate (existing P06 authz baseline). No cross-seller data leak — admin sees marketplace-wide operational view only when authorized.

## Frontend binding

Consumed by `settlement-api.ts`:

- `loadAdminSettlementBalances()`
- `loadAdminPayoutQueue()`
- `processAdminPayout(payoutRequestId)`

Admin screens at `/admin/settlement` and `/admin/payouts`.
