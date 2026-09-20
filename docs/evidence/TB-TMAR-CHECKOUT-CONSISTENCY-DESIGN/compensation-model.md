# Compensation model

| Original | Compensation | Owner | Exact? | Idempotent? |
|---|---|---|---|---|
| Inventory ReserveAsync | ReleaseAsync | Inventory | exact release | yes (best-effort) |
| Order Checkout persist | Cancel unpaid / RestoreCancelledCheckoutAsync paths | Order | business cancel | must be |
| Cart ConvertAsync | restore Active if policy / reconcile | Cart | correction | must be |
| Promotion quota (if later written) | restore quota | Promotion | exact/correction | must be |
| Wallet debit | credit/refund | Wallet | ledger correction | must be |
| Payment capture | refund | Payment | PSP correction | must be |
| Fulfillment start | cancel shipment (domain) | Fulfillment | correction | must be |

Unsafe without design: compensating after external capture without refund API; double-release races (handled softly in code today).
