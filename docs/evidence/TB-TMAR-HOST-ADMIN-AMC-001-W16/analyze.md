# Analyze — W16 Product Workspace Publish Readiness GET Admin slice

## Structured state (pre-migrate)

| Field | State |
|---|---|
| Foundation-State | FOUNDATION_READY (Catalog certified; add ProductPublishing capability folders) |
| Ownership-State | MUST_SPLIT (Publish Readiness GET vs remaining ProductWorkspace) |
| File-Cohesion-State | MULTI_RESPONSIBILITY_COHESION_VIOLATION on ProductWorkspace* (readiness route members only in scope) |
| Localization-State | CANONICAL Domain labels via ProductPublishRules + focused media/seo MessageFa; Host maps missing product via PlatformHttpException |
| API-Result-Pattern-State | AD_HOC (Results.Json + PlatformHttpException catch) |
| Stable-Error-Code-State | workspace.product.missing already CatalogErrorCodes; Host still throws PlatformHttpException |
| CQRS-State | MISSING on publish-readiness HTTP |
| Validator-Coverage-State | NO_VALIDATOR_REQUIRED expected (Guid route + optional locale normalized by ProductSeoRules) |
| Contracts-Boundary-State | CLEAN target (Application models/ports; no *Contracts.cs bundle) |
| Cross-Module-Coupling-State | Host → Catalog.Application ICatalogDirectory for readiness |
| Cross-Module-Join-State | NONE (Catalog-only composition) |
| Persistence-Ownership-State | CORRECT (Catalog DbContext) |
| Endpoint-Ownership-State | HOST_OWNED → MODULE_OWNED |
| Schema-Migration-State | UNCHANGED |
| Behavior-Preservation-Risk | LOW–MEDIUM (missing order, locale, ProductPublishRules, focused seam reuse, view scope) |
| Final-Disposition | READY_TO_MIGRATE |

## Target analyzed

Single Admin route `GET /v1/admin/products/{productId}/publish/readiness` with optional `locale`.
Composer `GetPublishReadinessAsync` + `MapPublishReadiness`. Aggregate `GetAsync` publication/readiness retained in Host via `MapPublishReadiness` + `ICatalogDirectory.GetProductPublishReadinessAsync`.

## Workspace scope

`X-Tooba-Workspace-Scope=view` → ReadPermissions.CanView=true → GET publish/readiness allowed. No edit-scope gate. Do not call `CatalogWorkspaceScope.AllowsCatalogEdit` on this GET.

## Readiness authority (preserve)

- categoryReady = primary category assignable (CatalogCategoryTreeRules)
- translationReady = localized product identity (seoDetail.ProductName)
- attributeReady / variantReady / mediaReady / seoReady from focused Catalog readiness seams
- Missing order: category → identity → attributes → variants → media → seo
- ProductPublishRules messages + SummarizeMissingFa / MessageReadyFa
- No Offer/price/stock checks

## Canonical references

W13 ProductMedia / W14 ProductSeo / W15 ProductHistory evacuate; BuildingBlocks Result + ApiResponseFactory; ICatalogAdminAuthorizer.
