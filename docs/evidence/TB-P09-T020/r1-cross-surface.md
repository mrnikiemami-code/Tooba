# R1 Cross-Surface — TB-P09-T020-R1

Orders Detail and Fulfillment Queue agree on composed status and capabilities.

After KG partial dispatch on checkout `01a0864a-f248-7000-92d9-426915151d95`:

- Order Detail seller/line: `PartialDispatched`
- Queue `needs_action` row: `PartialDispatched`
- FE both surfaces: ارسال جزئی
- `pack_selected` present; cancel absent/400
- Seller B queue/detail stay independent (`ReadyToFulfill` → `Packed`)

No second status engine. Projection is `ComposeOperationalStatus` + quantity-aware capabilities.
