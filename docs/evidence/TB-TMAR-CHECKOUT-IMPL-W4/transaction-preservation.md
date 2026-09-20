# Transaction preservation
- CheckoutProcessManager shared TransactionScope unchanged (ARCH-TX-001)
- W4 Cancel/Restore/PaymentBridge do not introduce new cross-context TransactionScope
- No split commits for checkout submit participants
