# R2 — Marketplace settlement wiring

## Events

| Event | Type name | Producer | Consumer (Marketplace only) | Settlement op |
|-------|-----------|----------|-----------------------------|---------------|
| Payment succeeded | `payment.succeeded.v1` | Payment domain → `PaymentOutboxRegistration` | `SettlementPaymentSucceededHandler` | `AccrueFromPaymentAsync` |
| Refund succeeded | `refund.succeeded.v1` | Returns domain → `ReturnsOutboxRegistration` | `SettlementRefundSucceededHandler` | `AdjustFromRefundAsync` |

## Edition gate

`SettlementModule.AddServices`: handlers registered **only** when `Tooba:Edition=Marketplace`.

SingleStore: no settlement payment/refund handler DI registrations (verified by tests).

## Transport

- Outbox interceptor on module SaveChanges
- `OutboxDispatcher` → `MassTransitIntegrationEventPublisher` → PostgreSQL SQL Transport (`tooba_messaging` / schema `transport`)
- `ToobaIntegrationTransportConsumer` resolves `IIntegrationEventHandler<T>` and invokes Marketplace settlement handlers
- No RabbitMQ; `Tooba:Messaging:Transport=PostgreSql`

## Idempotency

- Accrual: payment inbox by `EventId` + entry key `payment-accrual:{paymentId}:{sellerOrderId}`
- Adjustment: refund inbox by `EventId` + entry key `refund-adjustment:{returnRequestId}`
