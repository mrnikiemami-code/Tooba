# Runtime smoke — TB-P09-T012

Host `:5088` `Host: alpha.localhost` health 200.
FE `:3000` `/fa/admin/orders/{id}` 200.
DevActor: `X-Tooba-Dev-Actor-User-Id: 01a036c2-970e-7000-8eb7-94bf5cc2d8db`

Script: `docs/evidence/TB-P09-T012/runtime-smoke.mjs` + `runtime-smoke.log`

## Orders

- Multi waiting payment (A): `01a07ec5-81ad-7000-b77d-4f60962d6d50`
- Single mixed Packed+Processing (E + shipment CTA): `01a07ec5-7b94-7000-8e14-9b81ae2261d1`
- Ready→Process→partial pack→shipment (B/C/D/F): `01a07f1b-41de-7000-83ff-b2484761140e`
- Cancelled (G): `01a07f1b-4c41-7000-8778-5c75aef9ae6d`

## A — waiting payment

GET operations: `confirm_deposit,reject_deposit,cancel` only. All sellerCaps `paymentLocked=true`, lines not selectable, exact banner FA.
FE: seller badge «در انتظار پرداخت», blue banner exact text, no checkboxes/kebab/bulk/shipment CTA.

## B — ReadyToProcess

New checkout confirmed. Caps `rowActionCodes=["mark_processing"]` only. POST `pack_selected` before start → 400 `order.operation.invalid`.

## C — Start then pack exact line

POST `mark_processing` 200. Caps switch to `pack_selected`. POST pack qty 1 of that line 200. Packed=1/2.

## D — seller not fully Packed

`fulfillmentStatus=Processing` after packing 1 of 2.

## E — mixed

Single L1 unpack ∩ L2 pack_selected = empty. POST mixed pack_selected → 400 `fulfillment.bulk.incompatible`.

## F — shipment CTA + exact qty

Aside `+ ایجاد مرسوله جدید` on single (seller-level packed unallocated).
On ready order: `shipmentEligibleQuantity=1` → POST create_shipment post+metadata qty 1 → 200 shipment `5731f80f-eab0-4911-8735-f3dc8c758acc`. After allocate eligible=0 / `shipmentCreationPossible=false`.

## G — cancelled

POST cancel 200. Operations `[restore_cancelled_order]` only. POST confirm/reject/mark_processing → 400 `order.cancelled.blocks_action`. Grid query row `status=Cancelled`.
