# TB-P04-T010 — duplicate callback / verify

Two `POST /v1/storefront/payments/{paymentId}/sandbox/complete` with the same attempt + provider reference after success.

| Call | Payment status |
| --- | --- |
| 1st complete (`outcome=success`) | `Succeeded` |
| 2nd complete (identical body) | `Succeeded` (idempotent; no second Order transition) |

PaymentId: `d46138d7-24d1-4d47-8e5f-37dd0697fa7c`

Order became `Paid` once via outbox `payment.succeeded.v1` → MassTransit SQL Transport → `OrderPaymentSucceededHandler`.
