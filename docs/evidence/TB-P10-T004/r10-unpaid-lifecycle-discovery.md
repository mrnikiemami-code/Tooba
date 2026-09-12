# TB-P10-T004-R10 — Unpaid lifecycle discovery

Reused `PaymentStatus.Expired` (already distinct from `Cancelled`). Order stays `PendingPayment`. Capability `PaymentExpired` / `AwaitingPaymentExpired` is the payment lifecycle, not SupplyStatus.

| Payment/Order state | Money received? | Evidence submitted? | Hold policy | On timeout | Retry allowed? | Auto-reacquire allowed? |
| --- | --- | --- | --- | --- | --- | --- |
| Online Created/Pending/Failed | No | No | OnlinePaymentHoldHours | Expire payment + release Order hold | Yes if EnsureOrderSupply available | No (view is CheckOnly) |
| Manual Pending, no reference | No | No | ManualPaymentInitialHoldHours | Same unpaid expire | Yes if supply available | No |
| Manual Pending + evidence | Claimed for review | Yes | ManualPaymentReviewHoldHours (R5) | Skip unpaid worker | N/A (review path) | R5 reacquire on confirm |
| Succeeded / Paid | Yes | n/a | Durable / EnsurePaidDurable | Never unpaid-expire | N/A | R7 EnsureOrderSupply |
| User Cancelled | No | n/a | Close/refund path | Not unpaid timeout | No silent retry | No |
| Expired unpaid | No | No | Released hold | Idempotent no-op | Same Order + NEW hold | Only on explicit retry |

Clock: `CustomerPayment.UnpaidTimeoutAt` set at initiation from `ICommerceHoldPolicy`. Precedence: payment method override > store row > `Payment:Gateway` / `Cart:PersistenceHours`.
