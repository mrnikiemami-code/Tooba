# TB-P07-T042 — Finance grids

| Grid | Engine | Endpoint | DTO |
|------|--------|----------|-----|
| Receipts | `AdminPaymentsGridQueryEngine` | `POST /v1/admin/payments/query` | `AdminReceiptListItem` |
| Settlement balances | bounded client (existing) | `GET /v1/admin/settlement/balances` | `AdminSettlementBalanceListItem` (+ seller name) |
| Payout queue | `AdminPayoutGridQueryEngine` | `POST /v1/admin/settlement/payout-queue/query` | `AdminPayoutListItem` (+ seller name) |

Receipt grid enriches checkout reference + customer via batch Order load (no cross-schema JOIN).
Policy: `AdminListGridPolicies.Payments`.
