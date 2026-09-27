# Validation matrix — W6 Facets

| Request | Classification | Validator | Reason |
|---|---|---|---|
| `GetEffectiveCategoryFacetsQuery` | NO_VALIDATOR_REQUIRED | — | Guid route + optional locale |
| `ListLocalCategoryFacetsQuery` | NO_VALIDATOR_REQUIRED | — | Guid route only |
| `UpsertCategoryFacetCommand` | VALIDATOR_REQUIRED | `UpsertCategoryFacetCommandValidator` | Input not null (transport) |
| `RemoveCategoryFacetOverrideCommand` | NO_VALIDATOR_REQUIRED | — | Two Guids from route |
| `ReorderCategoryFacetsCommand` | VALIDATOR_REQUIRED | `ReorderCategoryFacetsCommandValidator` | OrderedDefinitionIds not null |
| `GetStorefrontCategoryFacetsQuery` | NO_VALIDATOR_REQUIRED | — | Guid + optional locale |

Business rules (existence, schema membership, filterable, display type, exact reorder set) stay in Domain/FacetDirectory — not FluentValidation.

Validation codes:

- `catalog.validation.facet_input_required`
- `catalog.validation.facet_ordered_definition_ids_required`
