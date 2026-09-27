# Certify — W26 ProductWorkspace lifecycle

- CQRS: Publish/Unpublish/Archive/RestoreProductCommand → IProductLifecycleDirectory → Result
- HTTP: ProductWorkspace.Endpoints + IProductWorkspaceAdminAuthorizer + ApiResponseFactory
- Post-command composition: GetProductWorkspaceQuery
- Contracts: CatalogErrorCodes lifecycle reject codes registered in CatalogErrorCatalogContributor
- ProductWorkspace.Application remains Catalog.Contracts-only (no Application ref)
- Host Admin count 31; StoreAppearance deferred
- Guards: HostAdminAmcW26PwLifecycleGuardTests (+ W16/W19/W20 route-count updates)
