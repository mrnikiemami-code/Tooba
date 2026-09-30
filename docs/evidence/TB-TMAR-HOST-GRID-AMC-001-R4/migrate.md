# TB-TMAR-HOST-GRID-AMC-001-R4 — Migrate

## Scope

Move Sellers admin grid policy + engine + `AdminSellerListItem` into Party; Host AdminPanelComposer consumes Party Contracts port.

## Disposition executed

| Item | Before | After |
| --- | --- | --- |
| `AdminListGridPolicies.Sellers` | Host | `Party.Infrastructure/Grid/PartyAdminSellersGridPolicies` |
| `AdminSellersGridQueryEngine` | Host/Grid | `Party.Infrastructure/Grid` |
| `AdminSellerListItem` | Host Admin Panel models | `Party.Contracts` |
| Grid port | Host DI engine | `IAdminSellersGridPort` + `AdminSellersGridAdapter` |
| `AdminPanelComposer` | Host Grid types | Party Contracts only |
| Program DI Host Sellers engine | registered | **Removed** |

## Ownership notes

- FOUNDATION_PARTIAL Party: Infrastructure gained Grid/Adapters + Offer/Order Contracts refs; Contracts gained BuildingBlocks for grid DTO/port.
- Offer/Order Contracts-only metrics sort compromise preserved (in-memory metric sort after Contracts filtering).
- Canon/Offer/Order/HostSeller guards updated to Party sellers path.

## Behavior parity

- Field whitelist / default sort `name` asc / offers/orders metric filtering preserved.

## Guards

- New: `HostGridAmcR4GuardTests`
- Updated: HostAdminCanon001, HostAdminCanonicalCertification, AdminDbNativeGridQueryTests, OrderAdminPanelResidual, OfferArchitecture (sellers+storefront paths), HostOrderReverseAudit, HostSellerAmcR5

## Explicit non-goals

- Full Party ARCH-COMPLETE-002
- Schema change
- Commit / push / Bridge POST
