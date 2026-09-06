# Runtime smoke (TB-P09-T001)

Host `:5088`, FE `:3000` — focused checks:

1. PendingPayment → cancel projected; return not eligible.
2. Paid / ReadyToFulfill → mark_processing / cancel (zero shipments) when permitted.
3. Delivered inside 30d → request_return when eligible.
4. Delivered outside window → `window_expired`; return action absent; direct create rejected.
5. Settlement completed does not remove eligibility when window still open.
6. Invalid POST → `order.operation.invalid|failed` with FA detail (no stack).
