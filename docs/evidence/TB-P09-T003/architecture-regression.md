# TB-P09-T003 — Architecture regression

Verified / unchanged locks:
- Product ≠ Offer boundaries untouched
- No Order→Identity SQL JOIN (actor resolution remains Host batch contracts from T002-R1)
- MassTransit + PostgreSQL SQL transport (`MassTransit.SqlTransport.PostgreSQL`); RabbitMQ forbidden (`MessagingHostOptions`)
- Return eligibility independent of Settlement
- Settlement ledger not duplicated; detail financial summary still composed from settlement snapshots
- Invoice uses order/payment snapshots
- Internal notes order-owned append-only; not storefront-facing
