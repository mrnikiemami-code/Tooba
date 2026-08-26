# 08 — Consumer idempotency map (TB-P06-T003-R1)

| Handler | Event | Module | Idempotency |
|---|---|---|---|
| `OrderPaymentSucceededHandler` | `payment.succeeded.v1` | Order | `order.payment_inbox` by `EventId` |
| `PartyMembershipProjectionHandler` | `party.membership_established.v1` | Party | SpiceDB tuple write (no inbox added in R1) |

At-least-once assumed. Order path has durable dedup. Party partial inbox from stopped T003 **removed** — not incomplete.

MassTransit Consumer Outbox/Inbox: not added (module patterns sufficient for current handler count).
