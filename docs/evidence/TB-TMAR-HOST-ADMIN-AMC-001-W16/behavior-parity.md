# Behavior parity — W16

| Surface | Before | After |
|---|---|---|
| Route | Host `GET /v1/admin/products/{id}/publish/readiness` | Catalog same route once |
| Auth | AdminPanelAccess | ICatalogAdminAuthorizer |
| View scope | Allowed (CanView) | Allowed; no AllowsCatalogEdit |
| Locale | Host passed `locale`; directory NormalizeLocale | Query Locale → reader NormalizeLocale |
| Missing product | PlatformHttpException 404 workspace.product.missing | Result + ApiResponseFactory 404 same code |
| JSON shape | ProductPublishReadinessView fields | Catalog ProductPublishReadinessView same names |
| Aggregate GetAsync readiness | MapPublishReadiness + directory | Unchanged (wrapper) |
| Lifecycle publish gate | directory GetProductPublishReadinessAsync | Unchanged via unwrap wrapper |
