# Disposition map — W11 Variant Axes + Variant Matrix Admin (member-level)

Source: `Admin/CatalogAttributeEndpoints.cs` (partial evacuation).  
Host file after W11: **RETAINED_CATEGORY_CHANGE_ONLY** (53 → 53).

## Map registration

| Member | Classification | Destination |
|---|---|---|
| `MapPut("/variant-axes", …)` | MOVE_W11 | Catalog.Endpoints Variants |
| `MapGet("/variants/editor", …)` | MOVE_W11 | Catalog.Endpoints Variants |
| `MapPost("/variants/preview", …)` | MOVE_W11 | Catalog.Endpoints Variants |
| `MapPut("/variants/apply", …)` | MOVE_W11 | Catalog.Endpoints Variants |
| `MapGet("/variants/readiness", …)` | MOVE_W11 | Catalog.Endpoints Variants |
| `products.AddEndpointFilter(CatalogActorHttpBinding)` | RETAIN | Host category-change group still needs actor bind |
| `MapPost("/category-change-preview", …)` | RETAIN_W12 | Host |
| `MapPut("/primary-category", …)` | RETAIN_W12 | Host |
| Program `MapCatalogAttributeEndpoints()` | RETAIN | Host until W12 |
| W8 Definitions / W9 Schema / W10 ProductValues | PRESERVED | Catalog.Endpoints |

## Host HTTP methods (MOVE_W11)

| Member | Classification | Destination |
|---|---|---|
| `SetProductVariantAxesAsync` | MOVE_W11 | `SetProductVariantAxesCommand` + handler |
| `GetProductVariantEditorStateAsync` | MOVE_W11 | `GetProductVariantEditorStateQuery` + handler (+ Offer enrich) |
| `PreviewProductVariantsAsync` | MOVE_W11 | `PreviewProductVariantsQuery` + handler (+ Offer enrich) |
| `ApplyProductVariantsAsync` | MOVE_W11 | `ApplyProductVariantMatrixCommand` + handler (+ Offer enrich) |
| `GetProductVariantReadinessAsync` | MOVE_W11 | `GetProductVariantReadinessQuery` + handler |
| `EnrichVariantEditorWithOfferCountsAsync` | MOVE_W11 | Application handler via `IVariantOfferLookup` |

## Transport records

| Record | Classification | Notes |
|---|---|---|
| `SetProductVariantAxesRequest` | MOVE_W11 → Application Models; **RELOCATE Seller copy** | Seller currently imports Host.Admin type — relocate Seller-local record (W10-R1 pattern) |
| `ProductVariantSelectedAxisRequest` | MOVE_W11 | → Application Models |
| `ProductVariantPreviewRequest` | MOVE_W11 | → Application Models / Query body |
| `ProductVariantPatchRequest` | MOVE_W11 | → Application Models (Status as string; parse in validator/handler) |
| `ProductVariantApplyRequest` | MOVE_W11 | → Application Models / Command body |
| `CategoryChangeRequest` | RETAIN_W12 | Host |
| `CategoryChangePreviewRequest` | RETAIN_W12 | Host |

## Helpers

| Member | Classification | Notes |
|---|---|---|
| `EnrichVariantEditorWithOfferCountsAsync` | MOVE_W11 | Behind `IVariantOfferLookup`; remove Offer usings from Host Attribute file |
| `MapCategoryChangeInvalid` | RETAIN | Host category-change |
| `ToError` | RETAIN | Host retained routes |
| Patch `ParseStatus` IOE | MOVE_W11 | Validator / typed Result — no IOE |

## ICatalogDirectory / private helpers

| Method | Classification | Destination |
|---|---|---|
| `SetProductVariantAxesAsync` | EXTRACT → focused port; thin Unwrap wrapper | `IProductVariantDirectory` |
| `GetProductVariantEditorStateAsync` | same | same |
| `PreviewProductVariantCombinationsAsync` | same | same |
| `ApplyProductVariantMatrixAsync` | same | same (+ transaction + history) |
| `GetProductVariantReadinessAsync` | same | same |
| `BuildDesiredCombinationsAsync` | MOVE into focused directory | private; typed SemanticError (no ErrorFa-as-code) |
| `ResolveVariantAxisLabelsAsync` | MOVE into focused directory | private |
| `MapVariantListItemsAsync` | MOVE into focused directory | private |
| `MaxVariantCombinations` | MOVE into focused directory | const 200 preserved |
| `ResolvePrimaryCategoryIdAsync` | SHARED_HELPER_DUPLICATE_OK | ProductVariantDirectory private; CatalogDirectory retains for category-change |
| `ResolveEffectiveBindingsAsync` | SHARED_HELPER_DUPLICATE_OK | same |
| name/option/path helpers | SHARED_HELPER_DUPLICATE_OK | same |
| `ClearDefaultFlags` / `EnforceSingleDefault` | SHARED_HELPER_DUPLICATE_OK | ProductVariantDirectory private; CatalogDirectory retains for category-change |
| default-variant / patch / archive-deactivate | MOVE into focused directory | preserve semantics |
| `QueueProductHistory` EventVariantsChanged | MOVE into focused directory | with actor context |
| category-change methods | RETAIN | CatalogDirectory |

## Domain / semantics

| Concern | Classification |
|---|---|
| Product existence | Typed `catalog.product.missing` |
| Duplicate axis ids | Typed `catalog.variant.axes.duplicate` |
| Definition missing | Typed `catalog.attribute.missing` |
| Definition inactive | Typed `catalog.attribute.definition.inactive` |
| Variant-axis capability disabled | Typed `catalog.attribute.variant_axis.capability_disabled` |
| Axis not enabled in effective schema | Typed `catalog.variant.axis.schema_not_enabled` |
| No effective variant axes (ops requiring them) | Typed `catalog.variant.effective_axes.missing` |
| Free-text axis unsupported | Reuse `catalog.attribute.variant_axis.value_kind.invalid` |
| Option missing/mismatch | Typed `catalog.attribute.enum_option.mismatch` |
| Option inactive | Typed `catalog.attribute.enum_option.inactive` |
| Combination limit exceeded | Typed `catalog.variant.combination.limit_exceeded` |
| Patch target missing | Typed `catalog.variant.patch.target_missing` |
| Invalid patch status | Typed `catalog.variant.patch.status_invalid` (+ validator shape) |
| Default variant missing | Typed `catalog.variant.default.missing` |
| Archived cannot be default | Typed `catalog.variant.default.archived_forbidden` |
| Apply transaction all-or-nothing | PRESERVE BeginTransaction |
| Product history EventVariantsChanged | PRESERVE with actor context |
| Locale default fa-IR | PRESERVE |
| Success DTO messageFa/warningFa | PRESERVE (success payload, not transport errors) |
| OfferCount / ReferencedByOffers | PRESERVE via Contracts lookup |
| AdminPanelAccess on 5 routes | REPLACE → `ICatalogAdminAuthorizer` |
| PlatformHttpException / IOE catch on 5 routes | REMOVE on moved surface → Result + ApiResponseFactory |

## Actor / context

| Concern | Classification |
|---|---|
| Host `CatalogActorHttpBinding` on products group | RETAIN for category-change |
| Module `CatalogActorRequestBinding` | REUSE W10 for all five Catalog variant routes |
| Apply history | Bind actor before mutation |

## Offer boundary

| Concern | Classification |
|---|---|
| Host `IOfferLookupGateway` injection | REMOVE from Host Attribute file |
| Application `IVariantOfferLookup` | CREATE narrow port |
| Infrastructure adapter | Offer.Contracts only |
| Catalog→Offer.App/Infra/Domain | ZERO |
| Endpoints→Offer | ZERO |
| Catalog DB join to Offer | NONE |

## Host metrics

| Metric | Before | After |
|---|---|---|
| Host/Admin recursive `*.cs` | 53 | 53 |
| Variant route mappings in Host | 5 | 0 |
| `CatalogAttributeEndpoints.cs` | present | RETAINED_CATEGORY_CHANGE_ONLY |

## Validator matrix (W11)

| Request | Classification |
|---|---|
| `SetProductVariantAxesCommand` | VALIDATOR_REQUIRED (OrderedDefinitionIds non-null; no empty Guid items — duplicate primitive optional) |
| `GetProductVariantEditorStateQuery` | NO_VALIDATOR_REQUIRED |
| `PreviewProductVariantsQuery` | VALIDATOR_REQUIRED (SelectedAxes non-null; DefinitionId non-empty per axis) |
| `ApplyProductVariantMatrixCommand` | VALIDATOR_REQUIRED (SelectedAxes non-null; patch status parse shape when Status supplied) |
| `GetProductVariantReadinessQuery` | NO_VALIDATOR_REQUIRED |

## Final disposition

`READY_TO_MIGRATE` — five-route Variant Axes + Variant Matrix Admin slice only. Do not start W12.
