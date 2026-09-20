# Proposed consistency model

## Choice
**Process Manager (orchestrated Saga) owned by Order checkout workflow** — hybrid with local ACID + Outbox per participant.

## Why not pure choreography
Checkout has ordered compensations (reserve → order → convert → pay) and explicit conflict reconcile. Evidence shows Order already orchestrates sync steps; an explicit coordinator preserves that ownership without distributed TX.

## Rules
- Coordinator owner: Order (Checkout Process Manager) — owns purchase consistency boundary (BOUNDARY-V1 + code)
- No distributed/shared TransactionScope
- Each participant: local ACID write + Outbox when emitting events
- Sync commands for reserve/create/convert/cancel during in-process stages; async integration events for payment-succeeded → fulfillment/settlement (already)
- Recovery: durable workflow state + retries + compensation commands
