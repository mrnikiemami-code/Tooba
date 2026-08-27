# 09 — Restock regression proof (TB-P06-T011-R2)

No new backend work in R2. Re-ran R1 tests:

```
dotnet test src/backend/Tooba.slnx --filter FullyQualifiedName~Return_restock_increases
dotnet test src/backend/Tooba.slnx --filter FullyQualifiedName~ReturnFoundation
Full suite: 236/236 passed, 0 skipped, 0 failed
```

| Case | Test / behavior | Status |
| --- | --- | --- |
| Exactly once | `Return_restock_increases_on_hand_idempotently_after_consumed_reservation` OnHand 3→5 | PASS |
| Duplicate safe | same test replays idempotency key, OnHand stays 5, inbox count 1 | PASS |
| Partial restock | gateway rejects quantity > reservation | code path in `InventoryReturnGateway` |
| Rejected/cancelled | no restock call in `ReturnDirectory` except on refund success | ReturnFoundationTests lifecycle |
| Non-restockable | Released reservation no-op branch | `InventoryReturnGateway` |

Cross-module boundary preserved: Returns → `IReturnInventoryGateway` → Inventory schema only.
