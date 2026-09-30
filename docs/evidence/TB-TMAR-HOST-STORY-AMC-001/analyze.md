# Analyze — Host/Story AMC-001

## Target

`src/backend/Host/Tooba.Host/Story/` (2 files: StoryEndpoints.cs, StoryPanelComposer.cs)

## True ownership

| Responsibility | Owner |
|---|---|
| Story aggregate + schema | Story.Domain / Infrastructure |
| Use cases (public/admin/seller) | Story.Application CQRS |
| HTTP routes | Story.Endpoints |
| Admin grid | Story.Application port + Infrastructure Grid |
| Dev seed | Already Story.Infrastructure |
| Admin auth mechanics | BuildingBlocks `IAdminPanelAccess` via module authorizer |
| Seller auth mechanics | Host adapter → `ISellerPanelAccess` via `IStorySellerAuthorizer` |
| Host | composition + HostStorySellerAuthorizer only |

## Coupling / blockers

1. Host endpoints use `AdminPanelAccess` / `SellerPanelAccess` Host static helpers + `CurrentAuthenticatedSession`.
2. Host composer is a pass-through over `IStoryDirectory` / `IAdminStoryGridPort`.
3. Module missing Endpoints + Contracts.Errors; Application is flat `StoryContracts.cs` (FOUNDATION_PARTIAL).
4. Message-parsing residue: mutation errors derived from Persian exception text.

## Migration plan

1. Story.Endpoints (Storefront/Admin/Seller) + error catalog for stable codes.
2. CQRS handlers wrapping directory/grid; move composer → Application Presentation or inline.
3. Host ZERO: delete Host/Story; register Endpoints + seller authorizer adapter.
4. Guards + SoT + focused validate.

## Behavior locks

- All public/seller/admin routes and verbs unchanged.
- Error codes: `story.missing`, `story.cta.rejected`, `story.mutation.rejected`, `story.tenant.missing`, `story.reviewStatus.invalid`.
- Grid POST `/v1/admin/stories/query` unchanged.
