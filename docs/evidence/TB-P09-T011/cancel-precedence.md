# Cancel precedence

Cancelled checkout (`IsCheckoutCancelled`): `ProjectPaymentActions` skipped. Cancelled seller: fulfillment forward projection skipped (`order.Status != Cancelled`).

`ExecuteAsync` rejects `CancelledBlockedCodes` (`confirm_deposit`, `reject_deposit`, `restore_deposit`, `mark_processing`, `mark_packed`, `pack_selected`, `unpack`, shipment/tracking/dispatch/deliver) with `order.cancelled.blocks_action` → «سفارش لغوشده است؛ این عملیات مجاز نیست.»

`restore_cancelled_order` stays independently evaluated (T009-R1 gates unchanged).

Runtime on cancelled multi-seller `01a07ec5-81ad-7000-b77d-4f60962d6d50`:
- GET operations → `[restore_cancelled_order]` only
- POST `confirm_deposit` / `reject_deposit` / `mark_processing` → 400 `order.cancelled.blocks_action`
