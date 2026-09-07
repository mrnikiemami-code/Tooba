# R1 action matrix

Backend `AdminOrderOperationsComposer` remains SoT; FA labels updated:

| code | FA |
|------|-----|
| mark_packed | بسته‌بندی |
| create_shipment | ایجاد مرسوله |
| assign_tracking | ثبت کد رهگیری |
| dispatch_shipment | ارسال مرسوله |
| deliver_shipment | ثبت تحویل |
| retry_refund | تلاش مجدد برای بازگشت وجه |

Runtime on `01a0429e-…`: ReadyToFulfill → processing → packed → shipment → tracking → dispatch → deliver → request_return → approve_return (200).
