# CQRS — W14 ProductSeo

| HTTP | Auth | Scope | Actor | Request | Directory |
|---|---|---|---|---|---|
| GET `/seo` | ICatalogAdminAuthorizer | (view allowed) | no | GetProductSeoQuery | GetAsync |
| PUT `/seo` | ICatalogAdminAuthorizer | CatalogWorkspaceScope edit | CatalogActorRequestBinding | UpdateProductSeoCommand | UpdateAsync |
| GET `/seo/readiness` | ICatalogAdminAuthorizer | (view allowed) | no | GetProductSeoReadinessQuery | GetReadinessAsync |

Flow: HTTP → authorizer → (scope/actor) → ISender → handler → IProductSeoDirectory → Result → ApiResponseFactory.

Endpoints inject neither Composer, ICatalogDirectory, IProductSeoDirectory, nor CatalogDbContext.
