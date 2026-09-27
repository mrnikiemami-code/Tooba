# Validation matrix — W10

| Request | Classification | Reason |
|---|---|---|
| GetProductAttributeEditorStateQuery | NO_VALIDATOR_REQUIRED | Guid route + optional locale defaulted in handler |
| GetProductAttributeReadinessQuery | NO_VALIDATOR_REQUIRED | Guid route only |
| SetProductAttributesCommand | VALIDATOR_REQUIRED | Values collection non-null; each DefinitionId non-empty |
| SetProductAttributeCommand | NO_VALIDATOR_REQUIRED | Route Guids + simple body; business rules in directory |

FluentValidation does **not** duplicate: product/definition existence, schema eligibility, enum ownership, canonicalization, bounds.
