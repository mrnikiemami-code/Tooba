# Certify — W31 ProductWorkspace list/grid

- HTTP: ProductWorkspace.Endpoints GET `/` + POST `/query` via ListProductWorkspaceQuery / QueryProductWorkspaceGridQuery
- Catalog list Contracts port + AdminProductGridQueryEngine in Catalog.Infrastructure; Application Contracts-only enrich
- Policy: no PlatformHttpException on module surface (GridQueryValidationException → SemanticError)
- Host PW Map routes **0**; module **17**; Admin **31**; StoreAppearance deferred
- Guards: HostAdminAmcW31PwListGridGuardTests; prior W19–W30 route asserts 2→0 Host, 15→17 module
