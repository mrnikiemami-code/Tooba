# Discovery — TB-P09-T016

Orchestration already existed from T009/T011. T016 is the completion gate.

Reuse:

- `SellerOrderCancellationPolicy` — open statuses or Paid + `!HasDispatchedQuantity`
- `SellerOrder.Cancel` / `CancelPaidBeforeShipment`
- `CheckoutDirectory.CancelSellerOrderAsync` + `IInventoryDirectory.ReleaseAsync`
- `AdminOrderOperationsComposer.CancelAsync` + `ProjectWholeOrderCancel`
- `FulfillmentUnit.AbortForOrderCancel` / `ReactivateAfterOrderRestore`
- Payment `CloseOrStartRefundForOrderCancelAsync`
- Settlement `NeutralizeUnpaidAccrualForCancelAsync`
- Restore gates (dispatched, completed refund, seller payout)
- FE `filterOperationsForScope` whole-order cancel dedupe
- Grid/detail `reloadToken` + menu `refresh()` after execute

Residual completed here:

- Human dispatch-block FA
- Confirm Dialog covers shipment cancel + inventory release + refund start
- Gate unit-status alignment with composer/domain
- History FA lines
- LOCK-OPS-002..005
- Focused tests + runtime A–E
