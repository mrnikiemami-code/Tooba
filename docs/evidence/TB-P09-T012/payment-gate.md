# Payment-Gate — TB-P09-T012

Banner FA (locked):
`پرداخت این بخش از سفارش هنوز تأیید نشده است؛ پس از تأیید پرداخت، عملیات پردازش و ارسال فعال می‌شود.`

Projector sets `AdminSellerCapability.PaymentLocked` + `InfoMessageFa`. FE renders `admin-order-seller-payment-locked-*`. Checkboxes, kebab, bulk, and shipment CTA are suppressed (`selectable=false`, `shipmentCreationPossible=false`).

Runtime multi `01a07ec5-81ad-7000-b77d-4f60962d6d50`:
- actions: `confirm_deposit,reject_deposit,cancel` only
- 3 sellers `paymentLocked=true` `selectionAllowed=false` `shipmentCreationPossible=false`
- all line caps `selectable=false` + lockedReason `order.payment.pending` + exact banner FA
