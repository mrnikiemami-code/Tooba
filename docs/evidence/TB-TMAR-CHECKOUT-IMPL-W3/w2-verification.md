# W2 verification
- CheckoutProcessManager owns submit orchestration
- ICheckoutInventoryReservationPort (Inventory.Contracts) active
- shared TransactionScope present in CheckoutProcessManager
- process-state / idempotency via ICheckoutProcessTracker active
- no async Saga/messaging/compensation runtime
- Cart conversion previously via Cart.Application / ICheckoutSubmitHost.ConvertCartAsync — W3 target
