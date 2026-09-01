# TB-P07-T042-R1 — Projection repair

## Backend (`AdminPanelComposer.cs`)

- `GetOrderAsync` already projects `LineCount` / `SellerCount` from order lines (not settlement).
- `BuildSellerFinancials` emits rows from order snapshots when settlement credit missing.
- `BuildFinancialSummary` uses payment amount for `TotalReceivedFromCustomer`.
- **R1 add:** `HumanizeProviderCode` for financial history payment method.
- **R1 add:** `IsSuccessfulPaymentStatus` gate before emitting customer receipt event.

## Frontend (`admin-api.ts`)

- `enrichAdminOrderDetail()` fallback when server omits finance fields:
  - derive counts from `sellerOrders`
  - synthesize seller financial rows from order snapshots
  - synthesize customer receipt event from successful `payment`
  - recompute financial summary from seller rows + payment
- `formatAdminPaymentProvider()` / `formatAdminPaymentReference()` for operator-safe labels.

## UI (`admin-order-detail-screen.tsx`)

- Payment card uses humanized provider + safe transaction reference.
- Commission column shows `—` when unsettled and commission is zero.
