# W19 — Analyze (disposition before edits)

## Mission

Migrate **only** `GET /v1/admin/products/{productId:guid}` from Host ProductWorkspace into `Tooba.ProductWorkspace` using Contracts-only composition.

## Parent (W18) preserved

- ProductWorkspace 5-project skeleton exists
- Route count before this wave: Host 19 / ProductWorkspace 0
- Host/Admin `*.cs` = 52
- No ARCH-COMPLETE-002
- StoreAppearance deferred

## Target inventory (read before move)

| Source | Role | Disposition |
|---|---|---|
| `ProductWorkspaceEndpoints.GetAsync` | Host HTTP GET aggregate | **MOVE** → ProductWorkspace.Endpoints |
| `ProductWorkspaceEndpoints.MapGet("/{productId:guid}")` | Route ownership | **REMOVE** from Host; **ADD** in module |
| `ProductWorkspaceComposer.GetAsync` | Catalog + commercial composition | **SPLIT**: Catalog slice → `ICatalogAdminProductWorkspaceReadGateway`; commercial composition → `GetProductWorkspaceHandler`; Host method **RETAIN** as temporary post-write compatibility residue |
| `ProductWorkspaceModels` GET response DTOs | Response contract | **AUTHORITY** → ProductWorkspace.Application.Composition.Models; Host write routes re-point to module types |
| `ReadPermissions` / `X-Tooba-Workspace-Scope` | Transport scope | **MOVE** parser for GET into ProductWorkspace.Endpoints; Host keeps copy for remaining write routes |
| `AdminPanelAccess` on GET | Auth | **REPLACE** with `IProductWorkspaceAdminAuthorizer` |
| `CatalogActorHttpBinding` on Host group | Actor | **NOT** applied to moved GET (read-only; not proven necessary) |
| `ToError` / `PlatformHttpException` on GET | Errors | **ELIMINATE** on moved GET; use `Result` + `ApiResponseFactory` + `workspace.product.missing` |

## Foreign Contracts (composition)

| Port | Module | Usage |
|---|---|---|
| `ICatalogAdminProductWorkspaceReadGateway` (new) | Catalog.Contracts | Catalog-owned aggregate snapshot |
| `IOfferQueryGateway` | Offer.Contracts | Offers by variant ids |
| `IPriceQueryGateway` | Pricing.Contracts | Prices by offer ids |
| `IInventoryQueryGateway` | Inventory.Contracts | Positions + locations |
| `ITaxQueryGateway` | Tax.Contracts | Classifications + categories |
| `IPartyLookup` | Party.Contracts | Seller display names (NOT Party.Application) |

## Forbidden

- CatalogDbContext / EF / Domain entities in ProductWorkspace
- Party.Application / foreign Application / Infrastructure in ProductWorkspace.Application
- Migrating list/grid/brand-options/writes
- Starting W20 / another Host folder / ARCH-COMPLETE-002 claim

## Expected counts

- Host ProductWorkspace routes: 19 → 18
- ProductWorkspace endpoint routes: 0 → 1
- Host/Admin `*.cs`: 52 → 52
