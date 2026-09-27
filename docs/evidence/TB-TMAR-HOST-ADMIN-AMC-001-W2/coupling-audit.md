# Coupling audit — W2

- Catalog.Endpoints ProjectReferences: Application, Contracts, BuildingBlocks only (no Host/Infrastructure).
- Catalog *.cs: no Tooba.Host.Storefront / StoreAppearanceProjector.
- Quantity endpoints: ISender + ApiResponseFactory only; no ICatalogLookupGateway/DbContext.
- StoreAppearance remains Host with documented Host.Storefront coupling (deferred).
