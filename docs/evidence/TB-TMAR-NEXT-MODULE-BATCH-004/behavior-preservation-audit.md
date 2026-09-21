# Behavior preservation audit

| Flow | Classification |
|---|---|
| payment.succeeded → CreateFromPaid + inbox dedup | structural-only + foundation (clock/ids) |
| Inventory Consume/Commit via gateway | intentional boundary abstraction (IFulfillmentInventoryLifecyclePort) |
| Seller cancel HasDispatchedQuantity gate | structural-only (Contracts move) |
| Dispatch / tracking / packages | structural-only + exception code remap |
| Shipping service write/read | intentional behavior repair: PlatformHttpException → InvalidOperationException codes |
| Return eligibility window | structural-only + IClock |
| Return create idempotent | structural-only + ids |
| Approve refund PSP / Wallet | intentional boundary abstraction (IPaymentReturnReader; Status string) |
| Inventory restock after refund | structural-only (Contracts.Returns) |
| Settlement refund snapshot | structural-only (Contracts.Settlement shape unchanged) |

Accidental behavior change: 0 (codes remapped; Status compared as "Succeeded" string matching enum name).
