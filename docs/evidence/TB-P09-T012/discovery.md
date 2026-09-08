# Discovery — TB-P09-T012

Pre-T012 (after T011):

- Selection/kebab/bulk inferred packable qty and seller-level action codes. ReadyToFulfill lines could still surface Pack if `pack_selected` existed on the seller.
- Payment-pending sellers had no dedicated capability lock. Checkboxes/kebab/shipment CTA could appear from leftover derived packable math.
- Mixed hint used older T011 copy in places; contextual Start-selected vs Pack-selected was incomplete.
- Row kebab recovered seller-level pack/unpack (T011) but had no `mark_processing` row code / «شروع پردازش این قلم».
- Shipment CTA lived in the seller header in earlier waves; T012 moves it to the shipment aside and gates it on `shipmentEligibleQuantity` / `shipmentCreationPossible`.
- Line/seller capabilities were not part of `AdminOrderOperationsPage`. `ListAsync` already loaded fulfillments; no extra directory round-trip was required.

Locked reference (task): `D:\Users\User\source\repos\SarvNewVerRequirment\reference\Image\ChatGPT Image Sep 8, 2026, 06_45_30 AM.png` — seller groups, line checkboxes, kebab, shipment aside with `+ ایجاد مرسوله جدید`.
