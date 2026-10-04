# TB-TMAR-CART-AMSC-001-W0 — Coupling and Boundaries

## `Cross-Module-Coupling-State = LEGAL_CONTRACTS_ONLY`

Verified by scanning every non-generated `.cs` in `Tooba.Cart.*` and every `*.csproj`
`ProjectReference` for foreign module namespaces.

## Declared project references (current HEAD)

### `Tooba.Cart.Contracts.csproj`

```xml
<ProjectReference Include="..\..\Offer\Tooba.Offer.Contracts\Tooba.Offer.Contracts.csproj" />
```

Usage: `SalesChannel` (Contracts DTO enum) — **USED** (1 site, `Checkout/CartContracts.cs`).

### `Tooba.Cart.Domain.csproj`

```xml
<ProjectReference Include="..\..\..\BuildingBlocks\Tooba.BuildingBlocks\Tooba.BuildingBlocks.csproj" />
<ProjectReference Include="..\..\Offer\Tooba.Offer.Contracts\Tooba.Offer.Contracts.csproj" />
```

Usage: `IHasDomainEvents`, `DomainEventCollector`, `IDomainEvent` (BuildingBlocks) — **USED**;
`SalesChannel`, `OfferStatus` (Offer.Contracts) — **USED** (11 sites).

### `Tooba.Cart.Application.csproj`

```xml
Tooba.BuildingBlocks                 -> USED  (Result, SemanticError, IClock, IIdGenerator, Security)
Tooba.Cart.Contracts                 -> USED  (snapshots, ports, gateway)
Tooba.Cart.Domain                    -> USED  (aggregates/entities/events via GlobalUsings.Domain.cs)
Tooba.Catalog.Contracts              -> USED  (ICatalogCartPresentationLookup, ICatalogCartQuantityPolicyGateway)
Tooba.Party.Contracts                -> USED  (IPartyLookup)
Tooba.Offer.Contracts                -> USED  (SalesChannel)
Tooba.Pricing.Contracts              -> UNUSED  <-- defect G3
Tooba.Inventory.Contracts            -> UNUSED  <-- defect G3
```

### `Tooba.Cart.Infrastructure.csproj`

```xml
Tooba.Cart.Application       -> USED
Tooba.ModuleContracts        -> USED  (IToobaModule)
Tooba.StoreContext.Contracts -> USED  (ICurrentStoreCommerceContext)
Tooba.Persistence            -> USED  (ToobaNpgsql, IOutboxModuleRegistration, OutboxSaveChangesInterceptor)
Tooba.Catalog.Contracts      -> USED  (ICatalogCartQuantityPolicyGateway, IStoreCartPersistenceHoursReader)
Tooba.Inventory.Contracts    -> USED  (availability + hold ports)
```

### `Tooba.Cart.Endpoints.csproj`

```xml
Microsoft.AspNetCore.App (FrameworkReference)
Tooba.Cart.Application  -> USED
Tooba.BuildingBlocks    -> USED
```

### `Tooba.Cart.Tests.csproj`

All 5 Cart projects + `Catalog.Contracts`, `Party.Contracts`, `BuildingBlocks`. Test-only.

## Forbidden-coupling scan — ZERO hits

| Forbidden form | Result |
| --- | --- |
| `Tooba.<Other>.Application` reference (csproj or `using`) | **0** |
| `Tooba.<Other>.Infrastructure` reference | **0** |
| `Tooba.<Other>.Domain` reference | **0** |
| foreign `DbContext` (`CatalogDbContext`, `InventoryDbContext`, `OfferDbContext`, `PricingDbContext`) | **0** |
| foreign `DbSet` access | **0** |
| foreign repository / directory implementation usage | **0** |
| EF navigation crossing a module boundary | **0** |
| cross-module SQL/EF join | **0** |
| shared mutable entity | **0** |
| cross-module transaction assumption | **0** |

`CartDirectory` touches exactly one persistence source: its own `_db.Carts`.

## Cross-module SQL inventory

Only one raw SQL statement exists in the module
(`CartDirectory.ExpireDueBatchAsync`, lines 278–290):

```sql
SELECT c.cart_id AS "Value"
FROM cart.carts AS c
WHERE c.status = 'Active'
  AND c.expires_at IS NOT NULL
  AND c.expires_at <= {utcNow}
ORDER BY c.expires_at
LIMIT {batchSize}
FOR UPDATE SKIP LOCKED
```

It reads only `cart.carts` — **Cart's own schema**. No foreign table is referenced. This is the
canonical "claim due rows" pattern and must be preserved byte-for-byte through W1's decomposition.

## Reverse edges (foreign → Cart, Contracts only)

| Consumer | Cart contract consumed | Lawful? |
| --- | --- | --- |
| `Order.Contracts` (csproj) | `Cart.Contracts` | YES — Contracts |
| `Order.Application` (csproj) | `ICartPresentationGateway`, `CartAccess` | YES — Contracts |
| `Order.Infrastructure` (csproj) | `ICartQueryGateway`, `CartAccess` | YES — Contracts |
| `Catalog.Application` (csproj) | `ICartPersistenceHoursSource` (Lifetime) | YES — Contracts |
| `Fulfillment.Endpoints` (csproj) | `ICartQueryGateway`, `CartAccess` | YES — Contracts |
| Host `Order/HostOrderStorefrontActor.cs` | `CartAccess` | YES — Contracts, Host composition/security adapter |
| `Tooba.Host.Tests` | many | YES — test-only |

**No consumer references `Cart.Application`, `Cart.Domain` or `Cart.Infrastructure`.**

## Boundary shape assessment

| Port | Location | Cross-module? | Verdict |
| --- | --- | --- | --- |
| `ICartQueryGateway` | `Contracts/Checkout/CartContracts.cs` | YES (Order.Infrastructure, Fulfillment.Endpoints) | correctly in Contracts |
| `ICartPresentationGateway` | `Contracts/Presentation/CartPresentationContracts.cs` | YES (Order.Application) | correctly in Contracts |
| `ICartConversionPort` | `Contracts/Checkout/CartContracts.cs` | reserved seam | correctly in Contracts |
| `ICartPersistenceHoursSource` | `Contracts/Lifetime/ICartPersistenceHoursSource.cs` | YES (Catalog.Application) | correctly in Contracts |
| `ICartDirectory` | `Application/Ports/ICartDirectory.cs` | NO | correctly Application-internal |
| `ICartExpiryReconciler` | `Application/Lifetime/ICartExpiryReconciler.cs` | NO | correctly Application-internal |
| `ICartCommerceContextResolver` | `Application/Ports/ICartCommerceContextResolver.cs` | NO | correctly Application-internal |
| `ICartPersistenceHoursResolver` | `Application/Ports/ICartPersistenceHoursResolver.cs` | NO | correctly Application-internal |
| `ICartPersistenceHoursSource` (Application) | `Application/Ports/ICartPersistenceHoursSource.cs` | NO | **redundant empty alias** of the Contracts port (G6-adjacent hygiene) |
| `ICartUseCaseGuard` | `Application/Ports/ICartDirectory.cs` | NO | correctly Application-internal |

## Microservice-extractability verdict

| Requirement | State |
| --- | --- |
| Own schema (`cart`) + own migrations | YES |
| Own DbContext, no foreign `DbSet` | YES |
| Module-owned routes (`/v1/storefront/cart…`) | YES |
| Own outbox registration | YES |
| Own background worker (expiry) | YES |
| Zero Host business/persistence authority | YES |
| Cross-module reads only via Contracts | YES |
| Declared dependency surface == actual usage | **NO** — 2 unused edges (G3) |
| Contracts bundle split by capability | **NO** — G7 |
| Fault identity type-safe | **NO** — G2 |

The module is **structurally extractable today**; W1/W2 close the remaining hygiene and
declared-surface defects.
