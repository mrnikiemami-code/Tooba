# Shipment-Eligibility — TB-P09-T012

CTA only in shipment aside (`admin-order-seller-create-shipment-*`).
Label without selection: `+ ایجاد مرسوله جدید`.
With exact eligible selection: `ایجاد مرسوله از انتخاب‌شده‌ها`.

Shown when `create_shipment` projected AND (`selectedShippable` or `sellerCap.shipmentCreationPossible`).

Runtime F on `01a07f1b-41de-7000-83ff-b2484761140e` after pack qty 1 of 2:
- `shipmentEligibleQuantity=1`
- POST `create_shipment` post + metadata + exact qty 1 → 200 shipment `5731f80f-eab0-4911-8735-f3dc8c758acc`
- after allocate: eligible=0, `shipmentCreationPossible=false`
- seller stays Processing
