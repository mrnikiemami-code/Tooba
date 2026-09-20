# Process Manager boundary
- `CheckoutProcessManager` in Order.Application
- Coordinates via ICheckoutSubmitHost + ICheckoutInventoryReservationPort + ICheckoutProcessTracker
- No EF/DbContext; no background/messaging/compensation
