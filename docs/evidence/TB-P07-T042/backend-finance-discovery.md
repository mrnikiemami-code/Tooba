# TB-P07-T042 — Backend finance discovery

Reused modules (no duplicate domain):

| Concern | Module | Host surface |
|---------|--------|--------------|
| Customer payment | `Tooba.Payment` (`IPaymentAdminDirectory`, `PaymentDbContext`) | `GET /v1/admin/payments/{id}`, enriched order detail payment block |
| Settlement accrual | `Tooba.Settlement` (`ISettlementDirectory`, `SettlementEntry`) | `ListEntriesBySellerOrderIdsAsync`, seller financial rows |
| Seller payout queue | `Tooba.Settlement` (`PayoutRequest`) | existing `/v1/admin/settlement/payout-queue/query` + display-name enrich |

New Host read surfaces only:

- `POST /v1/admin/payments/query` → `AdminReceiptListItem` via `AdminPaymentsGridQueryEngine`
- Extended `GET /v1/admin/orders/{checkoutId}` → finance breakdown + history + summary

No new Payment/Settlement aggregates or schemas beyond batch lookup helpers.
