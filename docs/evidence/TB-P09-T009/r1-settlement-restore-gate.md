# T009-R1 Settlement restore gate

Reuse: `ISettlementDirectory.GetRestoreSettlementGatesAsync` (read-only). No Checkout→Settlement SQL, no payout rewrite.

Ledger: `SettlementEntry` (order-attributed accrual/debit). Payout: seller-level `PayoutRequest` Succeeded only.

Policy: `SellerOrderRestoreSettlementPolicy` FIFO-consumes Succeeded payouts against seller credits in PostedAt order.

- Accrued / payable, no Succeeded payout → restore allowed
- Succeeded payout consumed this order’s net credit → `order.restore.seller_payout_completed`
- Refund / dispatch / delivery still outrank payout
- Payment restore path unchanged
