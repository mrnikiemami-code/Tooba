# Fulfillment status semantics — TB-P09-T020

`Dispatched` means at least one shipment dispatched (enum comment). Warehouse ops now quantity-aware: remainder pack/process allowed until `Delivered` (all shipped). Queue `needs_action` includes packable remainder while Dispatched/InTransit.
