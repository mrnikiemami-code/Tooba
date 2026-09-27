# W21-STORE-LANDING analyze

## Source
- `Host/Admin/StoreLandingPageEndpoints.cs` (HTTP_ENDPOINT — ILLEGAL_ENDPOINT_OWNERSHIP)
- `Host/Admin/StoreLandingPageComposer.cs` (APPLICATION_USE_CASE + PRESENTATION_COMPOSITION + cross-module shell)

## Destination
- Catalog — FOUNDATION_READY (Endpoints/Application/Infrastructure/Domain/Contracts exist; write CQRS + Directory already Catalog-owned)

## Disposition
| Responsibility | Class | Destination |
|---|---|---|
| Admin `/v1/admin/pages*` routes | HTTP_ENDPOINT | Catalog.Endpoints/Admin/StoreLandingPages |
| Storefront `/v1/storefront/pages*` + home-selection | HTTP_ENDPOINT | Catalog.Endpoints/Storefront/StoreLandingPages |
| Landing read/write orchestration + cache | APPLICATION_USE_CASE | Catalog.Application ports + Infrastructure service |
| Request/response models | APPLICATION models | Catalog.Application/StoreLandingPages/Models |
| Existing write Commands/Directory | already Catalog | reuse |
| Storefront shell cards/articles/reviews | CROSS_MODULE | Catalog port + Host adapter (StorefrontComposer) |
| Merchandising campaign members | CROSS_MODULE | Catalog port + Host adapter (IMerchandisingCampaignQuery) |
| StoreAppearance* / ProductWorkspace* / Merchandising* Host files | OUT OF SCOPE | deferred |

## BLOCK leftovers in scope
- None for Host StoreLandingPage* evacuation.
- Pre-existing Directory/Domain `PlatformHttpException` throws remain transitional (mapped to Result at Application facade); full Directory→Result conversion deferred.
