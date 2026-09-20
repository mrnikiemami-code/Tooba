# Contracts Boundary Recovery

## Current weakness

Cross-module interfaces live in `*.Application` (16 App→App edges).

## Target

`Tooba.<Module>.Contracts` for gateways, read/query interfaces, boundary DTOs, justified commands, integration event contracts.

Forbidden across boundary: Infrastructure, internal Application classes, Domain entities, DbContext, EF models.

## Example target

Cart.Application → Pricing.Contracts (not Pricing.Application)

## Modules needing Contracts first (migration order)

1. Pricing, Inventory, Offer, Catalog (highest fan-in)
2. Cart, Tax, Promotion
3. Party, Payment, Order
4. Remaining modules as touched

Compatibility: temporary type-forward adapters; architecture test gate activates after Foundation.

Effort: L · Risk: Medium
