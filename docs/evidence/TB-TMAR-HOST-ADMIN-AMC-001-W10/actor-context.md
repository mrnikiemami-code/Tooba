# Actor / context — W10

## Host baseline

Products group used `CatalogActorHttpBinding.BindAsync` (Host filter) to set `ICatalogActorContext` from session + OperatorProfile directory. Bulk `SetProductAttributesAsync` writes `ProductHistory` with ActorUserId/DisplayName.

## W10 design

- Do **not** copy Host filter into Catalog.Endpoints.
- Module-owned `CatalogActorRequestBinding` at Catalog.Endpoints boundary:
  - uses `actorUserId` from `ICatalogAdminAuthorizer.RequireAuthorizedAsync`
  - resolves display via `IActorDisplayLookup` (OperatorProfile.**Contracts** only)
  - defaults display name to «اپراتور» when profile missing
- Applied on all four product-attribute routes (parity with Host group filter).
- Catalog → Host remains ZERO; Endpoints → OperatorProfile.Application ZERO.

## Retained Host filter

`CatalogActorHttpBinding` remains on Host products group for variant/category-change routes.
