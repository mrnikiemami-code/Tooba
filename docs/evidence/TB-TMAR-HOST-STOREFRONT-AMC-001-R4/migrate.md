# TB-TMAR-HOST-STOREFRONT-AMC-001-R4 — Migrate (FINAL)

## Scope

Evacuate Host/Storefront residual demo seed + AccountIdentity; delete empty endpoints stub; certify **HOST_ZERO**.

## Disposition executed

| Item | Before | After |
| --- | --- | --- |
| `StorefrontDemoCatalogBootstrap.cs` | Host/Storefront (foreign Application/Domain) | `Catalog.Infrastructure/Development/StorefrontDemo/` via Contracts-only Development seed gateways |
| `StorefrontDemoCatalogMatrix.cs` | Host/Storefront | Same Catalog Development folder |
| `StorefrontAccountIdentity.cs` | Host/Storefront | `Host/Authentication/StorefrontAccountIdentity.cs` (`KEEP_AS_HOST_AUTH_PLATFORM`) |
| `StorefrontEndpoints.cs` (empty R3 stub) | Host/Storefront | **Deleted** |
| Host/Storefront directory | 4 residual files | **ABSENT** |
| Host/Development allowlist | 5 files | **Unchanged** (no sink) |

## Ownership notes

- Demo orchestration is Catalog-owned (sentinel product `demo-mobile-1`); Offer/Pricing/Inventory/Tax/Party writes use existing `I*DevelopmentSeedGateway` Contracts (same pattern as `WorkspaceDemoMarketplaceSeed` / `CatalogAttributeSchemaSellableEnricher`).
- `ApplyAsync` no longer touches Host-only `ControlPlaneRegistry` or foreign Content/ProductQnA Infrastructure; caller assigns commerce context first (Catalog Development seed precedent).
- AccountIdentity remains Host Authentication platform plumbing; `AuthenticationHttpBoundary` updated.

## Behavior parity

- Sentinel idempotency slug: `demo-mobile-1`
- Matrix counts / ExpectedOfferCount preserved
- Seed writes still Catalog directory + foreign Development Contracts only (no schema change)

## Guards

- New: `HostStorefrontAmcR4GuardTests`
- Updated: R1–R3 Host Storefront stubs → ABSENT assertions; Cart/Order/Payment/Media/CheckoutIdentity tests that previously read Host `StorefrontEndpoints.cs`

## Explicit non-goals

- Full Catalog ARCH-COMPLETE-002
- Schema/migration change
- Commit / push / Bridge POST
