# Row Actions

Capability-driven via backend projection + operations composer (fulfillment-queue scope).

ReadyToFulfill → `mark_processing` only.
Processing after pack 0.50 → `mark_packed` / `unpack` / `create_shipment`.
Created+tracking → `correct_tracking` / `cancel_shipment` / `dispatch_shipment`.
Cancelled / no codes → kebab hidden.
No payment / whole-order cancel on this page.
