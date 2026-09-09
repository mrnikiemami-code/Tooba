# R1 Capability Semantics — TB-P09-T020-R1

Capabilities stay quantity-based and win over persisted aggregate status.

After first KG dispatch (0.50 of 1.25):

- `pack_selected` still present (`hasPackAfter: true`)
- whole-order cancel hidden; POST cancel 400
- remainder packable / shippable / dispatchable

Seller B on the same checkout stays `ReadyToFulfill` then independently `Packed`. Dispatch of seller A does not project seller B as dispatched and does not share a shipment.

LOCK-OPS-009 remainder + LOCK-OPS-002 cancel-after-dispatch preserved.
