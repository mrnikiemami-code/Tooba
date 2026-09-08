# Capability-Projection — TB-P09-T012

Backend: `AdminFulfillmentCapabilityProjector.Project` inside existing `AdminOrderOperationsComposer.ListAsync` (fulfillments already loaded). No `ListForCheckoutAsync` / N+1.

`AdminOrderOperationsPage` now carries `lineCapabilities` + `sellerCapabilities`.

Rules:
- PendingPayment / Submitted / ReservationRequested / no fulfillment → `paymentLocked`, no selectable lines.
- Cancelled → no fulfillment capability.
- ReadyToFulfill → `mark_processing` only (even if seller also has `pack_selected`).
- Processing/Packed + packable → `pack_selected`.
- Packed unallocated → `unpack` + `shipmentEligibleQuantity`.

FE maps both casings and falls back to `deriveLineCapability` only when API caps are missing.

Runtime (single Paid/Processing `01a07ec5-7b94-7000-8e14-9b81ae2261d1`):
- L1 Packed 2/2 → row `unpack`, ship 2
- L2 Processing 0/3 → row `pack_selected`, ship 0
- L3 Packed 1/1 → row `unpack`, ship 1
