# TB-P07-T042 — Marketplace finance semantics

**Seller breakdown**

- When `SettlementEntry` credit exists for `SellerOrderId`: gross/commission/payable from entry; status `Settled`.
- Else: gross = order `SubtotalSnapshot`, commission = 0, payable = `GrandTotalSnapshot`; status `WaitingForSettlement` if Paid else `NotSettled`.

**Financial history**

- `CustomerReceipt` from latest checkout payment ops.
- `SellerSettlement` / `SettlementAdjustment` from settlement entries tied to seller orders.

**Summary totals**

- Seller share / commission / payable aggregated from seller financial rows.
- Customer receipt totals from order snapshots + payment amount when present.

All amounts are snapshot/read-model; no mutating finance actions in this task.
