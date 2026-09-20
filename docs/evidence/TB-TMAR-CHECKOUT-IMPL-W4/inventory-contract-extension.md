# Inventory.Contracts extension
Added OrderInventoryLifecycleContracts.cs:
- IOrderInventoryLifecyclePort
- ReleaseHeldReservationAsync / TryReleaseReservationAsync
- ReacquireDurableHoldFromPreviousAsync (StockItemId stays inside Inventory)
- PromoteOrReacquireForManualPaymentReviewAsync
- ReleaseIfHeldAsync
- EnsurePaidDurableSupplyAsync + OrderInventoryPaidSupply* DTOs
No Domain entities; no Application types; no DbContext leakage.
