# TB-P04-T010 — Payment vs Order state

## Success path

| Layer | State |
| --- | --- |
| Payment | `Succeeded` |
| Checkout `paymentState` | `Paid` |
| SellerOrder | `Paid` |

Frontend polls Host; it never invents Paid from sandbox button text.

## Failure path

| Layer | State |
| --- | --- |
| Payment | `Failed` |
| Checkout `paymentState` | `PendingPayment` |
| SellerOrder | `PendingPayment` |

Failed verify does not mark Order paid.

## Durable path

```text
Verify → CustomerPayment.Succeeded + domain event
→ payment.outbox_messages (payment.succeeded.v1)
→ OutboxDispatcher
→ MassTransit PostgreSQL SQL Transport
→ OrderPaymentSucceededHandler
→ SellerOrder Paid
```

Development Host enables `Tooba:Messaging` against ConnectionReference `messaging` / DB `tooba_messaging` so the live Order projection can run. Silent in-process fallback remains forbidden.
