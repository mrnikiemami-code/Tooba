# TB-TMAR-ORDER-GOLDEN-001-R4-R1 repair notes

- Injected canonical `IClock` into `OrderInventoryRecoveryService` and `OrderSupplyService`; removed direct `DateTimeOffset.UtcNow`.
- `RecoverAsync`: expected `ContractOperationException` codes map to typed `SemanticError`; unknown exceptions propagate after rollback.
- Rollback failures are not swallowed; surfaced via `AggregateException` (plus original fault).
- Inventory `ReserveAsync` insufficient path emits `ContractOperationException("inventory.supply.unavailable")` at source (typed, not Message parse).
- Host R4 removals unchanged. nextTask remains R5 (not started).
