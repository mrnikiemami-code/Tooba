# Disposition map — W9 Category Attribute-Schema Admin (member-level)

Source: `Admin/CatalogAttributeEndpoints.cs` (partial evacuation).  
Host file is **RETAINED_PARTIAL_PRODUCT_VARIANT_ONLY** (53 → 53).

## Map registration

| Member | Classification | Destination |
|---|---|---|
| `MapGroup(".../attribute-schema")` + 5 route maps | MOVE_W9 / REPLACE_WITH_MODULE_ENDPOINT | `Catalog.Endpoints/Admin/Attributes/Schema/CatalogCategoryAttributeSchemaAdminEndpoints.cs` |
| `MapGroup(".../products/{productId}")` + 11 routes | RETAIN_FOR_LATER_WAVES | Host |
| Program `MapCatalogAttributeEndpoints()` | RETAIN | Host still maps retained product/variant groups |
| W8 Definition routes | PRESERVED | Catalog.Endpoints Attributes/Definitions |

## Schema HTTP methods (MOVE_W9)

| Member | Classification | Destination |
|---|---|---|
| `GetEffectiveSchemaAsync` | MOVE_W9 | `GetEffectiveCategorySchemaQuery` + handler |
| `BindAsync` | MOVE_W9 | `BindCategoryAttributeCommand` + handler |
| `UpdateBindingAsync` | MOVE_W9 | `UpdateCategoryAttributeBindingCommand` + handler |
| `UnbindAsync` | MOVE_W9 | `UnbindCategoryAttributeCommand` + handler |
| `ReorderBindingsAsync` | MOVE_W9 | `ReorderCategoryAttributeBindingsCommand` + handler |

## Transport records

| Record | Classification | Notes |
|---|---|---|
| `BindCategoryAttributeRequest` | MOVE_W9 | → Application Models / Command body |
| `UpdateCategoryAttributeBindingRequest` | MOVE_W9 | → Application Models / Command body |
| `ReorderCategoryBindingsRequest` | MOVE_W9 | → Command `OrderedDefinitionIds` |
| Product/variant/category-change request records | RETAIN | Host |

## Helpers

| Member | Classification | Notes |
|---|---|---|
| `MapAttributeInvalid` | RETAIN | Still required by retained Host product Attribute routes |
| `MapCategoryChangeInvalid` | RETAIN | Host |
| `ToError` | RETAIN | Host retained routes still catch PlatformHttpException |
| `EnrichVariantEditorWithOfferCountsAsync` | RETAIN | Offer.Contracts for retained variant enrichment only |

## ICatalogDirectory schema members

| Method | Classification | Destination |
|---|---|---|
| `GetEffectiveCategorySchemaAsync` | EXTRACT → focused port; thin Unwrap wrapper | `ICategoryAttributeSchemaDirectory` |
| `BindCategoryAttributeAsync` | same | same → Result |
| `UpdateCategoryAttributeBindingAsync` | same | same |
| `UnbindCategoryAttributeAsync` | same | same |
| `ReorderCategoryAttributeBindingsAsync` | same | same |
| `ResolveEffectiveBindingsAsync` (private) | RETAIN in CatalogDirectory | Still needed by product/variant Host callers via CatalogDirectory |
| Product attribute / variant / category-change methods | RETAIN | CatalogDirectory |

## Domain / semantics

| Concern | Classification |
|---|---|
| `CatalogCategoryAttributeAssignmentRules.ValidateVariantAxis` | DOMAIN_AUTHORITY preserved; directory pre-checks → typed Result (no message parse) |
| `CatalogCategorySchemaResolver` inheritance/override | DOMAIN_AUTHORITY preserved |
| Category existence | Typed `catalog.category.missing` / schema category missing |
| Definition existence | Typed `catalog.attribute.missing` |
| Duplicate binding | Typed `catalog.schema.binding.duplicate` |
| Missing binding | Typed `catalog.schema.binding.missing` |
| Reorder exact-set | Typed `catalog.schema.reorder.invalid` |
| Variant-axis rule invalid | Reuse `catalog.attribute.variant_axis.*` codes |
| Offer on schema 5 routes | NONE |
| `AdminPanelAccess` on 5 routes | REPLACE → `ICatalogAdminAuthorizer` |
| PlatformHttpException / IOE catch on 5 routes | REMOVE_DEAD on moved surface → Result + ApiResponseFactory |

## Host metrics

| Metric | Before | After |
|---|---|---|
| Host/Admin recursive `*.cs` | 53 | 53 |
| Schema route mappings in Host | 5 | 0 |
| `CatalogAttributeEndpoints.cs` | present | RETAINED_PARTIAL_PRODUCT_VARIANT_ONLY |

## Validator matrix (W9)

| Request | Classification |
|---|---|
| `GetEffectiveCategorySchemaQuery` | NO_VALIDATOR_REQUIRED |
| `BindCategoryAttributeCommand` | VALIDATOR_REQUIRED (DefinitionId non-empty) |
| `UpdateCategoryAttributeBindingCommand` | NO_VALIDATOR_REQUIRED |
| `UnbindCategoryAttributeCommand` | NO_VALIDATOR_REQUIRED |
| `ReorderCategoryAttributeBindingsCommand` | VALIDATOR_REQUIRED (OrderedDefinitionIds non-null) |

## Final disposition

`READY_TO_MIGRATE` — five-route Category Attribute-Schema Admin slice only. Do not start W10.
