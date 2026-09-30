# Analyze — Host/PageComposition AMC-001

## Target

`src/backend/Host/Tooba.Host/PageComposition/` (2 files: PageCompositionEndpoints.cs, PageCompositionPanelComposer.cs)

## True ownership

| Responsibility | Owner |
|---|---|
| Page/section aggregates + schema | PageComposition.Domain / Infrastructure |
| Use cases (storefront/admin) | PageComposition.Application CQRS |
| HTTP routes | PageComposition.Endpoints |
| Dev seed | Already PageComposition.Infrastructure |
| Admin auth mechanics | BuildingBlocks `IAdminPanelAccess` via module authorizer |
| Host | composition root only |

## Coupling / blockers

1. Host endpoints used `AdminPanelAccess` Host static helpers + `CurrentAuthenticatedSession`.
2. Host composer was a pass-through over `IPageCompositionDirectory` + Domain tenant ids.
3. Module missing Endpoints + Contracts.Errors; Application was flat `PageCompositionContracts.cs` (FOUNDATION_PARTIAL).
4. Message-parsing residue: mutation errors derived from Persian exception text in Host endpoints.

## Final disposition

`READY_TO_MIGRATE` → evacuate Host folder to module Endpoints/CQRS (HOST_ZERO).

## Behavior locks

- Routes/verbs unchanged (storefront home composition + admin page-composition/home).
- Error codes: `page-composition.tenant.missing`, `page-composition.section.missing`, `page-composition.section-type.rejected`, `page-composition.config.rejected`, `page-composition.mutation.rejected`.
