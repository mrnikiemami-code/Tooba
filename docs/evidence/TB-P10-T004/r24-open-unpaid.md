# TB-P10-T004-R24 — Open unpaid

Canonical `OpenUnpaidOrderPredicate`: `PendingPayment`, `Submitted` only. Store + `PlacedByUserId`. Payment Attempts never queried.

| State | Counts | Runtime |
| --- | --- | --- |
| PendingPayment | yes | A-order-1 / B-order-2 open=2 |
| Failed/expired retryable (Order still PendingPayment) | yes | hide after expire still 409 |
| Manual AwaitingAdmin | yes | P-manual-counts-open=1 (Order stays PendingPayment) |
| Hidden pending | yes | D-hide-not-in-count |
| Cancelled | no | E-slot-freed open=1 |
| Paid | no | K-paid-frees-slot |
| Payment Attempts | no | I-retry-no-churn did not change open predicate |

Third commit 409 `checkout.open_unpaid_limit_reached` currentCount=2 maxCount=2 before reservation.
