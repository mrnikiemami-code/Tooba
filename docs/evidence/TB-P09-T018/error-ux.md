# Error UX

Stable codes + centralized FA map. Added:
`fulfillment.dispatch.invalid_state|tracking_required|already_dispatched`
`fulfillment.shipment.void_after_dispatch|void_invalid_state`
`fulfillment.allocation.conflict`
`fulfillment.work_queue.row_mismatch`
`fulfillment.missing`

Composer maps English domain leftovers (`dispatch از این وضعیت…`) to FA. Stale post-dispatch actions that drop from projection return «این عملیات در وضعیت فعلی سفارش مجاز نیست.» — no JSON/status leakage.
