# Disposition map — W12 Category-Change Admin (member-level)

Source: `Admin/CatalogAttributeEndpoints.cs` (FINAL evacuation).  
Host file after W12: **DELETED**. Host/Admin count **53 → 52**.

## Map registration / Host HTTP

| Member | Classification | Destination |
|---|---|---|
| `MapPost("/category-change-preview", …)` | MOVE_W12 | Catalog.Endpoints `CategoryChanges` |
| `MapPut("/primary-category", …)` | MOVE_W12 | Catalog.Endpoints `CategoryChanges` |
| `products.AddEndpointFilter(CatalogActorHttpBinding)` | DELETE with Host file | Replaced by per-route `CatalogActorRequestBinding` |
| `MapCatalogAttributeEndpoints()` | DELETE | remove Program call |
| Program `MapCatalogAttributeEndpoints()` | REMOVE | Host Program.cs |
| `PreviewCategoryChangeAsync` (Host HTTP) | MOVE_W12 | `PreviewCategoryChangeQuery` + handler |
| `ReplacePrimaryCategoryAsync` (Host HTTP) | MOVE_W12 | `ReplacePrimaryCategoryCommand` + handler |
| `MapCategoryChangeInvalid` | DELETE | Typed Result + CatalogErrorCodes (no message compare) |
| `ToError` | DELETE | ApiResponseFactory |
| `CategoryChangeRequest` | MOVE_W12 | Application Models write body |
| `CategoryChangePreviewRequest` | MOVE_W12 | Application Models write body |
| CatalogAttributeEndpoints.cs file | DELETE entirely | — |

## ICatalogDirectory / CatalogDirectory methods

| Method | Classification | Destination |
|---|---|---|
| `PreviewCategoryChangeAsync` | EXTRACT → focused port; thin Unwrap wrapper retained for legacy | `ICategoryChangeDirectory` |
| `PreviewCategoryChangeReportAsync` | EXTRACT → focused port; thin Unwrap wrapper | same |
| `ReplaceProductPrimaryCategoryAsync` | EXTRACT → focused port (+ transaction); thin Unwrap wrapper | same |
| `EnsureAssignableProductCategoryAsync` | SHARED — category-change uses typed Result; CatalogDirectory retains for Additional category CRUD | CategoryChangeDirectory private typed check; CatalogDirectory retains throw-style for non-W12 callers |
| `ResolveEffectiveBindingsAsync` | SHARED_HELPER_DUPLICATE_OK | CategoryChangeDirectory private |
| `ResolvePrimaryCategoryIdAsync` | SHARED_HELPER_DUPLICATE_OK | same |
| `BuildCategoryPathAsync` | SHARED_HELPER_DUPLICATE_OK | same |
| `GetAttributeDefinitionNamesAsync` | SHARED_HELPER_DUPLICATE_OK | same |
| `GetAttributeOptionNamesAsync` | SHARED_HELPER_DUPLICATE_OK | same |
| `FormatOrphanDisplay` | MOVE into focused directory | private static |
| `ClearDefaultFlags` | SHARED_HELPER_DUPLICATE_OK | CategoryChangeDirectory private (replace path) |
| `QueueProductHistory` | MOVE into focused directory for category-change events | with actor context |
| `ToPersianDigits` | MOVE into focused directory | private static |
| `AddProductAdditionalCategoryAsync` / Remove / List | RETAIN | CatalogDirectory (out of W12 scope) |

## Domain / schema authority (DO NOT MOVE into validators)

| Concern | Classification |
|---|---|
| `CatalogCategoryTreeRules.ProductAssignableLevel` (=3) | PRESERVE Domain authority |
| `CatalogCategoryTreeRules.AssignmentLevelInvalidErrorCode` | PRESERVE canonical stable code on Result path |
| `CatalogCategoryTreeRules.ProductAssignableLevelRequiredMessageFa` | Domain message retained; **NOT** used for HTTP classification |
| `CatalogCategorySchemaResolver.PreviewCategoryChange` | PRESERVE Domain |
| Variant axis compatibility / orphan / newly-required | PRESERVE in directory report logic |
| Additional membership promotion | PRESERVE |
| Safety unpublish + EventUnpublished | PRESERVE |
| EventCategoryChanged + history summaries | PRESERVE via ProductHistoryRules |
| ProductPublishRules lifecycle labels | PRESERVE |
| Replace transaction all-or-nothing | PRESERVE BeginTransaction |
| Variants not hard-deleted | PRESERVE Draft downgrade only |

## Error / result classification (typed)

| Outcome | Stable code |
|---|---|
| Product missing | `catalog.product.missing` |
| Target category missing | `catalog.category.missing` |
| Level ≠ 3 assignability | `catalog.category.assignment.level.invalid` (canonical) |
| Other invalid category-change state | `catalog.category_change.invalid` |
| Preview invalid (generic) | `catalog.category_change.invalid` |
| Replacement invalid (generic) | `catalog.category_change.invalid` |

No `ex.Message == ProductAssignableLevelRequiredMessageFa`. No PlatformHttpException expected flow on moved surface.

## Validation matrix

| Request | Classification |
|---|---|
| `PreviewCategoryChangeQuery` | VALIDATOR_REQUIRED — NewCategoryId non-empty |
| `ReplacePrimaryCategoryCommand` | VALIDATOR_REQUIRED — NewCategoryId non-empty |
| Route `productId` Guid | NO_VALIDATOR_REQUIRED (route constraint) |
| Locale | optional; defaulted fa-IR in handler/directory |

## Auth / actor / pipeline

| Concern | Destination |
|---|---|
| AdminPanelAccess | REPLACE → `ICatalogAdminAuthorizer` |
| CatalogActorHttpBinding group filter | REPLACE → `CatalogActorRequestBinding` per route |
| ICatalogDirectory in endpoint | FORBIDDEN → ISender only |
| ICategoryChangeDirectory in endpoint | FORBIDDEN |
| CatalogDbContext in endpoint | FORBIDDEN |
| Success DTO Persian fields | PRESERVE (success payload, not transport errors) |

## Out of scope (DO NOT TOUCH)

Seller panel, ProductWorkspace*, StoreAppearance*, Merchandising*, StoreLanding*, StoreMenu*, checkout/reservation/hold, unrelated category CRUD, W13, next Host folder.
