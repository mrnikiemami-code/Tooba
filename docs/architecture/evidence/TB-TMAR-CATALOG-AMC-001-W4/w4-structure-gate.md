# TB-TMAR-CATALOG-AMC-001-W4 — Structure gate

## Structure-State

`READY_FOR_CERTIFY`

## Folder-Granularity-State

`PROFESSIONAL_SHALLOW`

- Application: capability-first trees (Attributes, Categories, Settings, Storefront, StoreLandingPages, StoreMenus, Variants, …) + Ports/Models/Shared; no technical-axis-first Commands/Queries roots
- Domain: Aggregates/Enums/Events/Rules/ValueObjects + capability folders
- Infrastructure: Directories/Outbox/Persistence/Development/Storefront/Adapters/…
- Endpoints: Admin/Seller/Storefront audiences; composition root only
- Contracts: Ports/Errors/Resources/Cart/Checkout/Reservation

## Solution-Explorer-State

`CANONICAL` — `/Modules/Catalog/` in `Tooba.slnx` (5 projects).

## Path-Namespace-State

`EXACT`

## Physical-Copy-State

`CLEAN`

## Root-Allowlist-State

`ENFORCED` — GlobalUsings (+ CatalogModule / CatalogEndpointModule) only; see manifest Catalog entry.
