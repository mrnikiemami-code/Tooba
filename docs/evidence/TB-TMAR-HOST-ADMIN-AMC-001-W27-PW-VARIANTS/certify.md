# Certify — W27 ProductWorkspace variants

- CQRS: Create/PatchProductWorkspaceVariantCommand → IProductVariantDirectory → Result
- HTTP: ProductWorkspace.Endpoints + CanEditCatalog + ApiResponseFactory; create returns 201 JSON
- Post-command composition: GetProductWorkspaceQuery
- Codes: workspace.variant.* registered in CatalogErrorCatalogContributor
- Host Admin count 31; StoreAppearance deferred
- Guards: HostAdminAmcW27PwVariantsGuardTests (+ prior route-count updates)
