# Certify — W28 product DELETE

- CQRS: DeleteProductCommand → IProductDeletionDirectory → Result
- HTTP: Catalog.Endpoints + ApiResponseFactory (204 success / 409 referenced)
- Offer boundary: Infrastructure → Offer.Contracts only; Application has zero Offer refs
- Code: workspace.product.delete.referenced registered (Conflict 409)
- Host Admin count 31; StoreAppearance deferred
- Guards: HostAdminAmcW28PwDeleteGuardTests
