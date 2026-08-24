# TB-P04-T010 — نقشهٔ معماری پرداخت

```text
Customer UI (Shopeiva confirmation / sandbox / result)
  → Host public Payment API
      POST /v1/storefront/checkout/{checkoutId}/payments
      GET  /v1/storefront/payments/{paymentId}
      POST /v1/storefront/payments/{paymentId}/sandbox/complete
  → IPaymentDirectory (P03)
  → IPaymentGateway / FakePaymentGateway sandbox adapter
  → Verify (callback claim is not truth)
  → CustomerPayment durable state
  → Outbox payment.succeeded.v1
  → OrderPaymentSucceededHandler
  → SellerOrder Paid
```

## اثبات مرزها

- Frontend never marks paid: result page polls Host checkout `paymentState` / payment `status`.
- Provider is replaceable: `IPaymentGateway` + registry (`fake`, `fake-fail`).
- Amount from durable backend snapshot: `InitiatePaymentCommand` has no Amount; `IPayableCheckoutReader` supplies totals.
- No cross-module SQL join: Payment uses PaymentDbContext; Order projection consumes outbox.

## محدودهٔ مبلغ

Payment owner = CheckoutGroup (`CheckoutId`). Allocations map to Seller Orders. One customer payment covers the group total.
