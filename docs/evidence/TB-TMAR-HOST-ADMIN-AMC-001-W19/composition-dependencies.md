# W19 — Composition dependencies

| Consumer | Dependency | Kind |
|---|---|---|
| GetProductWorkspaceHandler | ICatalogAdminProductWorkspaceReadGateway | Catalog.Contracts |
| GetProductWorkspaceHandler | IOfferQueryGateway | Offer.Contracts |
| GetProductWorkspaceHandler | IPriceQueryGateway | Pricing.Contracts |
| GetProductWorkspaceHandler | IInventoryQueryGateway | Inventory.Contracts |
| GetProductWorkspaceHandler | ITaxQueryGateway | Tax.Contracts |
| GetProductWorkspaceHandler | IPartyLookup | Party.Contracts |

## Explicit zeros

- ProductWorkspace.Application → foreign Application = ZERO
- ProductWorkspace.Application → foreign Infrastructure = ZERO
- ProductWorkspace.Infrastructure → foreign DbContext = ZERO
- ProductWorkspace → Host = ZERO
- ProductWorkspace → Party.Application = ZERO
- ProductWorkspace.Endpoints → Infrastructure = ZERO
