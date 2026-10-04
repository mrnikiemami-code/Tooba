# Cross-module boundary — AccessControl (W3)

## Classification

| Aspect | State |
| --- | --- |
| Cross-module boundary | `CONTRACTS_ONLY` |
| `FOREIGN_ACCESS` | `ZERO` |
| Foreign Domain project reference | `ZERO` |
| Foreign Application project reference | `ZERO` |
| Foreign Infrastructure project reference | `ZERO` |
| Foreign `DbContext` / `DbSet` | `ZERO` |
| Foreign repository implementation | `ZERO` |
| Cross-module SQL/EF join | `ZERO` |
| Direct table/schema reach-through | `ZERO` |
| Shared mutable aggregate | `ZERO` |

## Project reference inventory

### `Tooba.AccessControl.Application.csproj`

| Reference | Kind |
| --- | --- |
| `Tooba.BuildingBlocks` | platform |
| `Tooba.AccessControl.Domain` | same module |
| `Tooba.AccessControl.Contracts` | same module |
| `Tooba.Identity.Contracts` | foreign **Contracts** ✅ |
| `Tooba.OperatorProfile.Contracts` | foreign **Contracts** ✅ |
| `Tooba.Catalog.Contracts` | foreign **Contracts** ✅ |

### `Tooba.AccessControl.Infrastructure.csproj`

| Reference | Kind |
| --- | --- |
| `Tooba.AccessControl.Application` | same module |
| `Tooba.AccessControl.Contracts` | same module |
| `Tooba.AccessControl.Domain` | same module |
| `Tooba.Catalog.Contracts` | foreign **Contracts** ✅ |
| `Tooba.Identity.Contracts` | foreign **Contracts** ✅ |
| `Tooba.Party.Contracts` | foreign **Contracts** ✅ |
| `Tooba.ModuleContracts` | platform |
| `Tooba.BuildingBlocks` | platform |
| `Tooba.Persistence` | platform |

### `Tooba.AccessControl.Domain.csproj`

| Reference | Kind |
| --- | --- |
| `Tooba.BuildingBlocks` | platform |
| `Tooba.AccessControl.Contracts` | same module |

### `Tooba.AccessControl.Contracts.csproj`

| Reference | Kind |
| --- | --- |
| `Tooba.BuildingBlocks` | platform |

### `Tooba.AccessControl.Endpoints.csproj`

| Reference | Kind |
| --- | --- |
| `Tooba.AccessControl.Application` | same module |
| `Tooba.AccessControl.Contracts` | same module |
| `Tooba.BuildingBlocks` | platform |

`Endpoints` deliberately does **not** reference `Tooba.AccessControl.Domain` — asserted by
`AccessControlModuleAmcW4StructureGuardTests` and `AccessControlStructureRecert001GuardTests`.

## Foreign namespace consumption (Contracts namespaces only)

```text
using Tooba.Catalog.Contracts;
using Tooba.Catalog.Contracts.Ports;
using Tooba.Identity.Contracts;
using Tooba.Identity.Contracts.Actors;
using Tooba.Identity.Contracts.Auth;
using Tooba.Identity.Contracts.Contacts;
using Tooba.Identity.Contracts.Problems;
using Tooba.OperatorProfile.Contracts.Ports;
using Tooba.Party.Contracts.Ports;
```

Every foreign namespace is a `*.Contracts` namespace. No foreign
`*.Application` / `*.Infrastructure` / `*.Domain` namespace is imported anywhere in the module.

## Synchronous cross-module lookups

| Port | Owner | Shape |
| --- | --- | --- |
| `ICatalogCartPresentationLookup` (category existence) | `Catalog.Contracts` | narrow Contracts port, in-process DI |
| `IPartySellerDirectory` | `Party.Contracts` | narrow Contracts port |
| `IOperatorProfileDirectory` | `OperatorProfile.Contracts` | narrow Contracts port |
| `IIdentity*` actor/auth/contact/problem contracts | `Identity.Contracts` | narrow Contracts DTOs/ports |

None reads a foreign table, schema or `DbSet`; none performs a cross-module EF join.

## Microservice extraction readiness

Removing `AccessControl` from the Host would require only:
- repointing the four foreign `*.Contracts` ports to HTTP/gRPC adapters (already Contracts-shaped);
- hosting `AccessControlModule` + `AccessControlEndpointModule` in a new composition root;
- pointing the module's own `AccessControlDbContext` at its own database.

No business logic, persistence or endpoint ownership must be extracted from Host.
`microserviceExtractable = true`.
