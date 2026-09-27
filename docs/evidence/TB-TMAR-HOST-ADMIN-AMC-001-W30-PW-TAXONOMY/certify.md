# Certify — W30 ProductWorkspace taxonomy

- CQRS: AssignProductCategory / AddAdditionalCategory / RemoveAdditionalCategory / AssignProductBrand → IProductTaxonomyDirectory → Result
- HTTP: ProductWorkspace.Endpoints + MutateAsync + GetProductWorkspaceQuery; DELETE validates expectedUpdatedAt
- Codes: schema-impact / assign.rejected / brand.invalid + catalog.category.assignment.* registered; reuse product missing, catalog stale, assignment level
- Host Admin count 31; StoreAppearance deferred
- Guards: HostAdminAmcW30PwTaxonomyGuardTests; prior W19/W19R1/W20/W26/W27/W28/W29 route asserts 6→2 Host, 11→15 module
