# Disposition map — W13 Product Workspace Media Admin (member-level)

Source Host files (RETAINED_PARTIAL after W13):
- `Admin/ProductWorkspaceEndpoints.cs`
- `Admin/ProductWorkspaceComposer.cs`
- `Admin/ProductWorkspaceModels.cs`

Host/Admin production `*.cs` count: **52 → 52**.
Media Host route count: **8 → 0**.

## Endpoint route registrations (Host)

| Member | Classification | Destination |
|---|---|---|
| `MapGet("/{productId}/media", ListMediaAsync)` | MOVE_W13 | Catalog.Endpoints ProductMedia |
| `MapGet("/{productId}/media/readiness", GetMediaReadinessAsync)` | MOVE_W13 | Catalog.Endpoints ProductMedia |
| `MapPost("/{productId}/media", AttachMediaAsync)` | MOVE_W13 | Catalog.Endpoints ProductMedia |
| `MapPost("/{productId}/media/placeholder", AttachPlaceholderMediaAsync)` | MOVE_W13 | Catalog.Endpoints ProductMedia |
| `MapPut("/{productId}/media/order", ReorderMediaAsync)` | MOVE_W13 | Catalog.Endpoints ProductMedia |
| `MapPut("/{productId}/media/{assetId}/primary", SetPrimaryMediaAsync)` | MOVE_W13 | Catalog.Endpoints ProductMedia |
| `MapPatch("/{productId}/media/{assetId}", PatchMediaAsync)` | MOVE_W13 | Catalog.Endpoints ProductMedia |
| `MapDelete("/{productId}/media/{assetId}", DetachMediaAsync)` | MOVE_W13 | Catalog.Endpoints ProductMedia |
| All other ProductWorkspace route maps | RETAIN | Host (later waves) |
| `ReadPermissions` / `X-Tooba-Workspace-Scope` | MOVE_POLICY (media writes only) | Catalog.Endpoints `CatalogWorkspaceMediaScope` |
| `AdminPanelAccess.RequireAuthorizedAsync` on 8 routes | REPLACE | `ICatalogAdminAuthorizer` |
| `CatalogActorHttpBinding` group filter | RETAIN on Host group; media routes leave Host | W13 endpoints: bind on writes (history actor) |
| `ToError` / PlatformHttpException catch | DELETE from media surface | `ApiResponseFactory` |
| `ListMediaAsync` … `DetachMediaAsync` endpoint methods | DELETE from Host | Catalog endpoints |

Exact route base preserved: `/v1/admin/products/{productId:guid}` (NOT `/v1/admin/catalog/...`).

## Composer members

| Member | Classification | Destination |
|---|---|---|
| `ListMediaAsync` | MOVE_W13 then DELETE Host public | `GetProductMediaQuery` |
| `GetMediaReadinessAsync` | MOVE_W13 then DELETE Host public | `GetProductMediaReadinessQuery` |
| `AttachMediaAsync` | MOVE_W13 then DELETE Host public | `AttachProductMediaCommand` |
| `AttachPlaceholderMediaAsync` | MOVE_W13 then DELETE Host public | `AttachPlaceholderProductMediaCommand` |
| `ReorderMediaAsync` | MOVE_W13 then DELETE Host public | `ReorderProductMediaCommand` |
| `SetPrimaryMediaAsync` | MOVE_W13 then DELETE Host public | `SetPrimaryProductMediaCommand` |
| `PatchMediaAltAsync` | MOVE_W13 then DELETE Host public | `PatchProductMediaCommand` |
| `DetachMediaAsync` | MOVE_W13 then DELETE Host public | `DetachProductMediaCommand` |
| `MapMediaViews` | DELETE (media-only) | Application mapping in handlers/directory |
| `EnsureProductExistsAsync` | RETAIN (non-media callers) | Host Composer |
| `EnsureCatalogEdit` | RETAIN (non-media callers) | Host Composer; media uses module scope policy |
| Inline media mapping in `GetAsync` workspace composition | RETAIN | Host Composer (ProductWorkspaceView.Media) |

## Models

| Type | Classification | Destination |
|---|---|---|
| `AdminProductMediaAttachRequest` | MOVE_W13 then DELETE Host | Application Models write body |
| `AdminProductMediaPlaceholderRequest` | MOVE_W13 then DELETE Host | Application Models write body |
| `AdminProductMediaOrderRequest` | MOVE_W13 then DELETE Host | Application Models write body |
| `AdminProductMediaPatchRequest` | MOVE_W13 then DELETE Host | Application Models write body |
| `ProductMediaView` | RETAIN Host | Still used by `ProductWorkspaceView.Media` |
| `ProductMediaReadinessView` | MOVE shape to Catalog; DELETE Host | Only media readiness route consumer |
| `ProductWorkspacePermissions` | RETAIN Host | Non-media workspace |

JSON parity for list items: property name **`Primary`** (Host record), not `IsPrimary`.

## ICatalogDirectory / CatalogDirectory media methods

| Method | Classification | Destination |
|---|---|---|
| `GetProductMediaEditorStateAsync` | EXTRACT → focused port; thin Unwrap wrapper retained | `IProductMediaDirectory` |
| `GetProductMediaReadinessAsync` | EXTRACT → focused port; thin Unwrap wrapper retained | same (also publish readiness caller) |
| `AttachMediaReferenceAsync` (both overloads) | EXTRACT → focused port; thin Unwrap wrapper retained | same (bootstrap/demo/tests) |
| `AttachGeneratedPlaceholderMediaAsync` | EXTRACT → focused port; thin Unwrap wrapper retained | same |
| `ReorderProductMediaAsync` | EXTRACT → focused port; thin Unwrap wrapper retained | same |
| `SetProductPrimaryMediaAsync` | EXTRACT → focused port; thin Unwrap wrapper retained | same |
| `PatchProductMediaAltAsync` | EXTRACT → focused port; thin Unwrap wrapper retained | same |
| `DetachProductMediaAsync` | EXTRACT → focused port; thin Unwrap wrapper retained | same |
| `LoadOrderedMediaAssignmentsAsync` | MOVE into focused directory | private |
| `BuildMediaReadiness` | MOVE into focused directory | private |
| `EnforcePrimaryUniqueness` | MOVE into focused directory | private |
| `QueueProductHistory` (media events) | MOVE into focused directory with actor | private |
| `TouchProductUpdatedAt` (media path) | MOVE into focused directory | private |
| Application DTOs `ProductMediaAssignment` / `EditorState` / `Readiness` | RETAIN in CatalogContracts.cs | Shared legacy models; wrappers return them |

Live thin-wrapper callers documented: ProductMediaGalleryTests, CatalogFoundationTests, ProductHistoryTests, ProductPublishTests, PrimaryCategoryMigrationTests, CatalogAttributeSchemaTests, CatalogDemo/ProductWorkspaceDevelopment/Storefront demo bootstraps, `GetProductPublishReadinessAsync`.

## Domain / schema

| Concern | Classification |
|---|---|
| `CatalogProductMediaReference.Link` | PRESERVE Domain |
| Primary uniqueness when media.Count > 0 | PRESERVE `EnforcePrimaryUniqueness` |
| Reorder exact-set (no dups, full coverage) | PRESERVE |
| Detach = unassign reference only | PRESERVE |
| Placeholder = UuidV7 asset id + attach | PRESERVE |
| First attach is primary | PRESERVE |
| Detach primary → repair via EnforcePrimaryUniqueness | PRESERVE |
| Reorder does not change primary selection (unless uniqueness repair) | PRESERVE |
| ProductHistory EventMediaChanged / SummaryMedia* | PRESERVE with actor |
| Schema / migrations | UNCHANGED |

## Error / result (typed — preserve workspace.* codes)

| Outcome | Stable code | HTTP |
|---|---|---|
| Product missing | `workspace.product.missing` | 404 |
| Workspace view-scope write | `workspace.permission.denied` | 403 |
| Attach empty MediaAssetId | `workspace.media.asset.missing` | 400 |
| Attach rejected (dup / other) | `workspace.media.attach.rejected` | 400 |
| Placeholder rejected | `workspace.media.placeholder.rejected` | 400 |
| Reorder empty gallery | `workspace.media.empty` | 404 |
| Reorder set mismatch | `workspace.media.order.invalid` | 400 |
| Reorder other reject | `workspace.media.order.rejected` | 400 |
| Primary/patch/detach target missing | `workspace.media.missing` | 404 |

No PlatformHttpException / IOE message-as-code on migrated HTTP or ProductMediaDirectory.

## Validation matrix

| Request | Classification |
|---|---|
| `GetProductMediaQuery` | NO_VALIDATOR_REQUIRED |
| `GetProductMediaReadinessQuery` | NO_VALIDATOR_REQUIRED |
| `AttachProductMediaCommand` | VALIDATOR_REQUIRED — MediaAssetId != Guid.Empty (`workspace.media.asset.missing`) |
| `AttachPlaceholderProductMediaCommand` | NO_VALIDATOR_REQUIRED |
| `ReorderProductMediaCommand` | VALIDATOR_REQUIRED — OrderedMediaAssetIds non-null |
| `SetPrimaryProductMediaCommand` | NO_VALIDATOR_REQUIRED |
| `PatchProductMediaCommand` | NO_VALIDATOR_REQUIRED |
| `DetachProductMediaCommand` | NO_VALIDATOR_REQUIRED |

## Auth / actor / pipeline

| Concern | Destination |
|---|---|
| Admin auth | `ICatalogAdminAuthorizer` on all 8 |
| Workspace scope view write deny | `CatalogWorkspaceMediaScope` on 6 write routes |
| Actor binding | `CatalogActorRequestBinding` on writes (history); reads optional/bind for consistency |
| Endpoint inject Composer / ICatalogDirectory / IProductMediaDirectory / DbContext | FORBIDDEN |
| Endpoints → Infrastructure | ZERO |
| Catalog → Host | ZERO |

## Out of scope (DO NOT TOUCH)

workspace list/get/grid/history, create/core/title/quantity, category/brand, lifecycle/publish/readiness/delete, SEO, workspace variants, StoreAppearance, Seller, W14, next Host folder.
