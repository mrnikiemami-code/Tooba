# Cancelled Order Restore

`restore_cancelled_order` — «بازگردانی سفارش لغوشده»

Requires: every seller Cancelled, `CancelledFromStatus` present, no dispatch/delivery, no completed refund.

Restore reapplies snapshot status (PendingPayment or Paid). Inventory: find released reservation → `ReserveAsync` with stable idempotency `order-restore-{lineId}`. If reserve fails, newly acquired holds are released and the order stays cancelled.

Cancelled shipments are not resurrected. Payment is not rewritten.
