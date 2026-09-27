# Disposition map — W10 Product Attribute Editor/Readiness (member-level)

Source: `Admin/CatalogAttributeEndpoints.cs` (partial evacuation).  
Host file is **RETAINED_PARTIAL_VARIANT_CATEGORY_CHANGE_ONLY** (53 → 53).

## Map registration

| Member | Classification | Destination |
|---|---|---|
| `MapGet("/attributes", …)` | MOVE_W10 | Catalog.Endpoints ProductValues |
| `MapPut("/attributes", …)` | MOVE_W10 | Catalog.Endpoints ProductValues |
| `MapGet("/attributes/readiness", …)` | MOVE_W10 | Catalog.Endpoints ProductValues |
| `MapPut("/attributes/{definitionId}", …)` | MOVE_W10 | Catalog.Endpoints ProductValues |
| `products.AddEndpointFilter(CatalogActorHttpBinding)` | ANALYZE → MODULE_OWNED_BINDING | Catalog.Endpoints actor bind for history writes; Host filter retained for remaining product routes |
| `MapPut("/variant-axes", …)` + variants + category-change | RETAIN_FOR_LATER_WAVES | Host |
| Program `MapCatalogAttributeEndpoints()` | RETAIN | Host still maps retained groups |
| W8 Definitions / W9 Schema | PRESERVED | Catalog.Endpoints |

## Host HTTP methods (MOVE_W10)

| Member | Classification | Destination |
|---|---|---|
| `GetProductAttributeEditorStateAsync` | MOVE_W10 | `GetProductAttributeEditorStateQuery` + handler |
| `SetProductAttributesAsync` | MOVE_W10 | `SetProductAttributesCommand` + handler |
| `GetProductAttributeReadinessAsync` | MOVE_W10 | `GetProductAttributeReadinessQuery` + handler |
| `SetProductAttributeAsync` | MOVE_W10 | `SetProductAttributeCommand` + handler |

## Transport records

| Record | Classification | Notes |
|---|---|---|
| `SetProductAttributeRequest` | RETAIN | Still consumed by Host Seller `SellerPanelEndpoints` |
| `ProductAttributeValueRequest` | MOVE_W10 | → Application Models |
| `SetProductAttributesRequest` | MOVE_W10 | → Application Models / Command body |
| Variant/category-change request records | RETAIN | Host |

## Helpers

| Member | Classification | Notes |
|---|---|---|
| `MapAttributeInvalid` | RETAIN_DEAD_HELPER | Still asserted by W8/W9; unused by retained Host routes (definitions already vacated) |
| `MapCategoryChangeInvalid` | RETAIN | Host category-change |
| `ToError` | RETAIN | Host retained routes |
| `EnrichVariantEditorWithOfferCountsAsync` | RETAIN | Offer.Contracts for retained variants |

## ICatalogDirectory / private helpers

| Method | Classification | Destination |
|---|---|---|
| `GetProductAttributeEditorStateAsync` | EXTRACT → focused port; thin Unwrap wrapper | `IProductAttributeDirectory` |
| `SetProductAttributesAsync` | same | same → Result + transaction |
| `GetProductAttributeReadinessAsync` | same | same |
| `SetProductAttributeAsync` | same | same |
| `ApplyProductAttributeValueAsync` | MOVE into focused directory | private |
| `EnsureDefinitionAllowedForProductSchemaAsync` | MOVE into focused directory (product-attr surface) | private; CatalogDirectory keeps copy for retained callers if still needed |
| `EnsureNotEffectiveVariantAxisOnProductAsync` | same | private in focused directory; CatalogDirectory retains for variants if shared |
| `ResolvePrimaryCategoryIdAsync` | SHARED_HELPER_DUPLICATE_OK | ProductAttributeDirectory private; CatalogDirectory retains for variants/category-change |
| `BuildCategoryPathAsync` / name helpers | SHARED_HELPER_DUPLICATE_OK | ProductAttributeDirectory private |
| `GetAttributeDefinitionNamesAsync` / `GetAttributeOptionNamesAsync` | SHARED_HELPER_DUPLICATE_OK | ProductAttributeDirectory private |
| `FormatAttributeDisplay` | MOVE copy into focused directory | private static |
| `BuildReadiness` | MOVE copy into focused directory | private static |
| `CatalogAttributeCanonicalizer` (+ EnforceValidationBounds) | DOMAIN_AUTHORITY | preserved; catch→typed Result (no message parse) |
| `ResolveEffectiveBindingsAsync` | RETAIN in CatalogDirectory | still needed by variant/category-change; ProductAttributeDirectory owns its own private resolve (domain resolver shared) |
| Variant / category-change methods | RETAIN | CatalogDirectory |

## Domain / semantics

| Concern | Classification |
|---|---|
| Product existence | Typed `catalog.product.missing` |
| Definition missing | Typed `catalog.attribute.missing` |
| Definition inactive | Typed `catalog.attribute.definition.inactive` |
| Not in effective schema | Typed `catalog.attribute.schema.not_allowed` |
| Effective variant-axis on product | Typed `catalog.attribute.variant_axis.on_product_forbidden` |
| Enum option required / mismatch / inactive | Typed enum_option.* codes |
| Canonicalization / bounds | Typed value.invalid / validation.bounds |
| Clear required forbidden | Typed `catalog.attribute.clear.required_forbidden` |
| Bulk transaction all-or-nothing | PRESERVE BeginTransaction |
| Product history EventAttributesChanged | PRESERVE with actor context |
| Locale default fa-IR | PRESERVE |
| `AdminPanelAccess` on 4 routes | REPLACE → `ICatalogAdminAuthorizer` |
| PlatformHttpException / IOE catch on 4 routes | REMOVE_DEAD on moved surface → Result + ApiResponseFactory |
| Offer on 4 routes | NONE |

## Actor / context

| Concern | Classification |
|---|---|
| Host `CatalogActorHttpBinding` on products group | Required for bulk history ActorUserId/DisplayName |
| Module copy of Host filter | FORBIDDEN |
| Lawful Catalog.Endpoints binding | CREATE — bind `ICatalogActorContext` after auth via `IActorDisplayLookup` (OperatorProfile.Contracts) + default «اپراتور» |
| Reads | Bind actor (parity with Host filter); no history write |
| Writes | Bind actor before mutation so history captures actor |

## Host metrics

| Metric | Before | After |
|---|---|---|
| Host/Admin recursive `*.cs` | 53 | 53 |
| Product-attribute route mappings in Host | 4 | 0 |
| `CatalogAttributeEndpoints.cs` | present | RETAINED_PARTIAL_VARIANT_CATEGORY_CHANGE_ONLY |

## Validator matrix (W10)

| Request | Classification |
|---|---|
| `GetProductAttributeEditorStateQuery` | NO_VALIDATOR_REQUIRED |
| `SetProductAttributesCommand` | VALIDATOR_REQUIRED (Values non-null; DefinitionId non-empty per item) |
| `GetProductAttributeReadinessQuery` | NO_VALIDATOR_REQUIRED |
| `SetProductAttributeCommand` | NO_VALIDATOR_REQUIRED (route Guids + simple body; business rules in directory) |

## Final disposition

`READY_TO_MIGRATE` — four-route Product Attribute Editor/Readiness Admin slice only. Do not start W11.
