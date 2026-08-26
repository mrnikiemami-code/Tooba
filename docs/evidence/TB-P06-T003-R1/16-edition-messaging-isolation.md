# 16 — Edition messaging isolation (TB-P06-T003-R1)

## Envelope fields

- `TenantId`, `Edition`, `DeploymentId` persisted at outbox write time
- `PayloadJson` contains business IDs only (product, order, payment, party, etc.)

## Single-Store

- Consumer rebuilds tenant from envelope — not HTTP Host
- Proven: `MassTransitPostgresTests` tenant isolation + spoof host test

## Marketplace

- Business identifiers (seller/offer/product) — no DB connection data in payload

No cross-tenant leakage in transport tests.
