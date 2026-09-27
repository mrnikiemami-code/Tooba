# Certify — W29 ProductWorkspace identity

- CQRS: CreateWorkspaceProductCommand (`Result<Guid>`) + UpdateProductCatalogTitle/Core/QuantityPolicy → IProductIdentityDirectory → Result
- HTTP: ProductWorkspace.Endpoints + MutateAsync / CreateProductAsync + GetProductWorkspaceQuery; create returns 201 JSON
- Codes: title/category/create/slug.missing/quantity.rejected registered; reuse slug invalid/duplicate, product missing, catalog stale, assignment level
- Host Admin count 31; StoreAppearance deferred
- Guards: HostAdminAmcW29PwIdentityGuardTests; prior W19/W19R1/W20/W26/W27/W28 route asserts 10→6 Host, 7→11 module
