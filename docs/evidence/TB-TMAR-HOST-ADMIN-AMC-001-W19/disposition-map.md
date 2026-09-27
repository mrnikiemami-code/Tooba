# W19 — Disposition map

| Artifact | Keep / Move / Split / New | Owner after W19 |
|---|---|---|
| GET `/{productId:guid}` route map | Move | ProductWorkspace.Endpoints |
| Host `GetAsync` endpoint method | Remove | — |
| Host remaining 18 routes | Keep | Host.Admin |
| `ProductWorkspaceComposer.GetAsync` | Retain temporary (writes) | Host (compatibility residue) |
| Catalog EF reads inside Get | New Contracts gateway | Catalog.Infrastructure via Catalog.Contracts |
| Offer/Price/Inventory/Tax reads | Compose in handler | ProductWorkspace.Application |
| Party seller labels | Switch to Contracts | `IPartyLookup` |
| Response DTOs (aggregate GET) | Authority move | ProductWorkspace.Application.Composition.Models |
| List/create/write request DTOs | Keep in Host | Host.Admin |
| Workspace scope header parse (GET) | Move | ProductWorkspace.Endpoints |
| Auth for GET | Move | `IProductWorkspaceAdminAuthorizer` |
| 404 missing product | Canonical Result | `workspace.product.missing` via ApiResponseFactory |
| List/grid/brand-options/writes | Do not migrate | Host |

## Dual-model risk control

ProductWorkspace.Application models are authoritative for aggregate GET JSON.
Host write routes compile against the same module model types (no second evolving shape).
Host composer GetAsync remains for post-write responses only until later waves.
