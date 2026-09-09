# Migration final — TB-P09-T015

No new migration added.

Existing:

- T013 `20260909130400_DecimalInventoryQuantity` (SQL `numeric(18,6)`)
- T014 `20260909140000_AddInvoiceHeaderAggregates` (Order current)

T015 aligned `InventoryDbContextModelSnapshot` OnHand / Reserved / Quantity to `numeric(18,6)` so the snapshot matches the applied SQL migration. No duplicate columns.

`tooba_alpha` apply: pending 0. No wipe / no destructive reset.

Fresh-model path: same HEAD migrations; T006 / T013 / T014 coexist.
