# R2 Failure / Idempotency — TB-P09-T020-R2

Inventory reacquire failure: `order.restore.inventory_failed`; Order stays Cancelled; no forward Fulfillment capability with stale reservation.

Rebind after successful restore is idempotent (same handoff reservation applied twice).

Inventory invariant unchanged: only Held can MoveTo Released/Consumed; machine code `inventory.reservation.not_active`.

No distributed transaction; existing composer try/catch + Inventory release compensation on restore failure retained.
