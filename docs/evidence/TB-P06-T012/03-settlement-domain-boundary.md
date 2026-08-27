# 02 — Settlement domain boundary (TB-P06-T012)

## Module layout

```
src/backend/Modules/Settlement/
  Tooba.Settlement.Domain/          — domain types, commission policy, entry/statement/payout aggregates
  Tooba.Settlement.Application/     — contracts (ISettlementDirectory, IPayoutGateway, bridge readers)
  Tooba.Settlement.Infrastructure/  — SettlementDirectory, handlers, bridges, EF persistence, gateways
```

## Boundary rules (enforced)

| Rule | Implementation |
|---|---|
| Order ≠ Payment ≠ Settlement | Separate DbContext; schema `settlement` |
| No cross-module SQL JOIN | `SettlementOrderBridge`, `SettlementPaymentBridge`, `SettlementReturnsBridge` expose snapshot readers only |
| Payment owns money movement | Settlement accrues/adjusts ledger entries; payout gateway is separate boundary |
| Commission immutable on post | `CommissionPolicySnapshot` stored on each `SettlementEntry` |

## Static boundary proof

`SettlementFoundationTests.Settlement_module_boundary_static_checks`:

- `SettlementDbContext.Schema == "settlement"`
- `SettlementEntry` ≠ `CustomerPayment` (type isolation)
- `Tooba.Settlement.Infrastructure.csproj` does **not** reference Order/Payment Infrastructure projects directly

## Domain enums

- `EntryType`: Credit (payment accrual) / Debit (refund adjustment)
- `StatementStatus`: Open / Closed
- `PayoutStatus`: Pending → Processing → Succeeded / Failed
