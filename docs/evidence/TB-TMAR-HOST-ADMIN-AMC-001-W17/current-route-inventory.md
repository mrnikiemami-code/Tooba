# W17 — Remaining ProductWorkspace route inventory (post W16-R1)

Source of truth: `src/backend/Host/Tooba.Host/Admin/ProductWorkspaceEndpoints.cs` (`MapProductWorkspaceEndpoints`).
Verified against repo at parent `d4088103743693bfb6fa8a775aca95ef16aa6d0b`.
Host/Admin production `*.cs` count (recursive): **52** (unchanged).

**Remaining Host ProductWorkspace routes: 19** (exact match to Architect known list).

Already evacuated (NOT in Host ProductWorkspaceEndpoints; Catalog.Endpoints owns):

| Route | Owner (post W10–W16) |
|---|---|
| GET/PUT `/v1/admin/products/{id}/seo`, GET `.../seo/readiness` | Catalog ProductSeo |
| media routes under `/v1/admin/products/{id}/media*` | Catalog ProductMedia |
| GET `/v1/admin/products/{id}/history` | Catalog ProductHistory |
| GET `/v1/admin/products/{id}/publish/readiness` | Catalog ProductPublishing |

---

## Exact remaining inventory

| # | Method | Path | RW | Host method | Composer method | Business owner(s) | Persistence | Foreign Contracts | Response | Aggregate? | Move direct? | Blocked by composition? | Future wave |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | GET | `/v1/admin/products/` | R | `ListAsync` | `ListAsync` → `BuildListItemsForProductIdsAsync` | Catalog identity + Offer/Price/Inventory enrichment | `CatalogDbContext` Products/Variants/ProductCategories/MediaReferences/LocalizedTexts | Offer, Pricing, Inventory | `AdminProductListItem[]` | cross-module list | NO | YES | W19 |
| 2 | GET | `/v1/admin/products/brand-options` | R | `ListBrandOptionsAsync` | `ListBrandOptionsAsync` | Catalog Brands | `CatalogDbContext` Brands + LocalizedTexts | none | `AdminBrandOption[]` | Catalog-only | YES → Catalog | NO | W18b / with Catalog ports |
| 3 | POST | `/v1/admin/products/query` | R | `QueryGridAsync` | `QueryGridAsync` → `AdminProductGridQueryEngine` + list enrich | Catalog + Offer/Price/Inventory grid metrics | CatalogDbContext + gateways via engine | Offer, Pricing, Inventory | `GridPageResponse<AdminProductListItem>` | cross-module grid | NO | YES | W19 |
| 4 | POST | `/v1/admin/products/` | W | `CreateAsync` | `CreateSimpleProductAsync` → `GetAsync` | Catalog create | CatalogDbContext + `ICatalogDirectory.CreateProductAsync/AssignCategoryAsync` | none on write; **GetAsync** uses Offer/Price/Inv/Tax/Party | `ProductWorkspaceView` 201 | cross-module aggregate | NO (write alone OK; response blocked) | YES (post-write Get) | W20 |
| 5 | GET | `/v1/admin/products/{productId}` | R | `GetAsync` | `GetAsync` | Catalog + Offer + Pricing + Inventory + Tax + Party | CatalogDbContext many sets + Directory readiness/history | Offer, Pricing, Inventory, Tax, Party | `ProductWorkspaceView` or 404 | cross-module aggregate | NO | YES | W19 |
| 6 | PATCH | `.../catalog-title` | W | `PatchTitleAsync` | `UpdateCatalogTitleAsync` → `GetAsync` | Catalog localized title | CatalogDbContext LocalizedTexts/Products + history | via GetAsync | `ProductWorkspaceView` | post-write aggregate | NO | YES | W20 |
| 7 | PATCH | `.../core` | W | `PatchCoreAsync` | `UpdateProductCoreAsync` → `GetAsync` | Catalog core/translations | CatalogDbContext + history | via GetAsync | `ProductWorkspaceView` | post-write aggregate | NO | YES | W20 |
| 8 | PATCH | `.../quantity-policy` | W | `PatchQuantityPolicyAsync` | `UpdateQuantityPolicyAsync` → `GetAsync` | Catalog quantity policy | CatalogDbContext Products/Units | via GetAsync | `ProductWorkspaceView` | post-write aggregate | NO | YES | W20 |
| 9 | PUT | `.../category` | W | `AssignCategoryAsync` | `AssignProductCategoryAsync` → `GetAsync` | Catalog category assignment | CatalogDbContext + Directory Replace/Assign | via GetAsync | `ProductWorkspaceView` | post-write aggregate | NO | YES | W20 |
| 10 | POST | `.../categories/additional` | W | `AddAdditionalCategoryAsync` | `AddAdditionalCategoryAsync` → `GetAsync` | Catalog additional category | Directory + SaveChanges | via GetAsync | `ProductWorkspaceView` | post-write aggregate | NO | YES | W20 |
| 11 | DELETE | `.../categories/additional/{categoryId}` | W | `RemoveAdditionalCategoryAsync` | `RemoveAdditionalCategoryAsync` → `GetAsync` | Catalog additional category | Directory + SaveChanges | via GetAsync | `ProductWorkspaceView` | post-write aggregate | NO | YES | W20 |
| 12 | PUT | `.../brand` | W | `AssignBrandAsync` | `AssignProductBrandAsync` → `GetAsync` | Catalog brand | CatalogDbContext Brands/Products + history | via GetAsync | `ProductWorkspaceView` | post-write aggregate | NO | YES | W20 |
| 13 | POST | `.../publish` | W | `PublishAsync` | `PublishAsync` → `RequireWorkspaceAsync` | Catalog lifecycle | `ICatalogDirectory.PublishProductAsync` | via GetAsync | `ProductWorkspaceView` | post-write aggregate | NO | YES | W21 |
| 14 | POST | `.../unpublish` | W | `UnpublishAsync` | `UnpublishAsync` → `RequireWorkspaceAsync` | Catalog lifecycle | Directory | via GetAsync | `ProductWorkspaceView` | post-write aggregate | NO | YES | W21 |
| 15 | POST | `.../archive` | W | `ArchiveAsync` | `ArchiveAsync` → `RequireWorkspaceAsync` | Catalog lifecycle | Directory | via GetAsync | `ProductWorkspaceView` | post-write aggregate | NO | YES | W21 |
| 16 | POST | `.../restore` | W | `RestoreAsync` | `RestoreAsync` → `RequireWorkspaceAsync` | Catalog lifecycle | Directory | via GetAsync | `ProductWorkspaceView` | post-write aggregate | NO | YES | W21 |
| 17 | DELETE | `.../{productId}` | W | `DeleteAsync` | `DeleteOrSoftArchiveAsync` | Catalog delete + Offer reference gate | CatalogDbContext cascade remove / archive | **Offer** `AnyOffersForCatalogVariantIdsAsync` | **204 NoContent** (or 409 after soft-archive) | Catalog write + Offer Contracts check | YES to Catalog (NoContent; Offer Contracts OK) | NO for response shape | W23 |
| 18 | POST | `.../variants` | W | `CreateVariantAsync` | `CreateVariantAsync` → `RequireWorkspaceAsync` | Catalog variants | `ICatalogDirectory.CreateVariantAsync` | via GetAsync | `ProductWorkspaceView` 201 | post-write aggregate | NO | YES | W22 |
| 19 | PATCH | `.../variants/{variantId}` | W | `PatchVariantAsync` | `PatchVariantAsync` → `RequireWorkspaceAsync` | Catalog variants | CatalogDbContext Variants | via GetAsync | `ProductWorkspaceView` | post-write aggregate | NO | YES | W22 |

### Shared Host HTTP semantics (all 19)

- Auth: `AdminPanelAccess.RequireAuthorizedAsync`
- Actor: `CatalogActorHttpBinding` group filter
- Workspace scope: `X-Tooba-Workspace-Scope: view` → view-only `ProductWorkspacePermissions`; else full edit flags
- Errors: `PlatformHttpException` → `{ title, errorCode }` JSON (Host residual; not ApiResponseFactory yet)
- Grid policy: `AdminProductGridQueryPolicy.Normalize` (Host `Grid/`)

### Stable error codes observed (non-exhaustive)

`workspace.permission.denied`, `workspace.product.missing`, `workspace.product.title.missing`, `workspace.product.category.missing|invalid`, `workspace.product.slug.invalid|duplicate`, `workspace.product.create.rejected`, `workspace.catalog.stale`, `workspace.product.brand.invalid`, `workspace.product.publish|unpublish|archive|restore.rejected`, `workspace.product.delete.referenced`, `workspace.variant.axes.missing`, `workspace.variant.create.rejected`, `workspace.variant.missing`, `workspace.variant.status.invalid`, `workspace.quantity.rejected`, `catalog.category.assignment.stale`, plus category assignability code from `CatalogCategoryTreeRules`.
