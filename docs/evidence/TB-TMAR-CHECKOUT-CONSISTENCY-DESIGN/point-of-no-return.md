# Point of no return

Checkout-Point-Of-No-Return: MULTI_STAGE

## Stage A — After TransactionScope.Complete() on Submit
- Order CheckoutGroup + SellerOrders persisted
- Cart Converted
- Inventory reservations Held
- Reversible? Partially: inventory ReleaseAsync; cart restore exists (RestoreCancelledCheckoutAsync paths); order cancel paths exist for unpaid
- Externally observable? Yes (order id to buyer UI)
- Idempotent? Submit keyed by IdempotencyKey/CartId
- Not yet financially captured

## Stage B — Payment authorization/capture or full wallet debit (post-submit)
- Source: StorefrontPaymentComposer / PaymentDirectory / WalletDirectory
- Reversible? Only via refund/credit compensation
- Externally observable? PSP / wallet ledger
- True financial PONR for OnlinePurchase

## Stage C — Fulfillment dispatch after PaymentSucceeded
- Integration-event driven; further irreversible logistics

Payment/wallet are NOT inside the shared Submit TransactionScope (code evidence).
