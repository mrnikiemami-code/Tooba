# R3 production manual provider

- `ManualPaymentGateway` registered in Production and non-Prod.
- `Payment:Gateway:ManualCardToCardEnabled` controls storefront availability (default **false** in Production appsettings).
- No card/account numbers in source.
- Admin confirm uses Payment domain `ApplyVerifiedSuccess` (canonical outbox `payment.succeeded.v1`); gateway Verify alone never marks Succeeded.
- Reject uses `ApplyVerifiedFailure` → Failed (not refund semantics).
