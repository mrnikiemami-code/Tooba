# 10 — Error / dead-letter operations (TB-P06-T003-R1)

## SQL transport layer

Failed deliveries recorded in transport schema (e.g. `transport.message_delivery`). MassTransit SQL transport manages retry/skip semantics.

## Outbox layer

Publish failures → outbox retry → `dead_letter` state in module outbox table (dispatcher store).

## Operator identification

- Transport: inspect transport delivery tables + bus health
- Outbox: inspect module outbox dead-letter rows + structured logs/metrics (`tooba.outbox.dead_letters`)

## Replay cautions

- Do not replay side-effecting handlers without idempotency keys
- Prefer re-dispatch from outbox pending rows after root-cause fix

No RabbitMQ `_error` queue terminology — SQL transport topology differs.
