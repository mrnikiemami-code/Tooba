# Validation — W11

| Request | Classification | Notes |
|---|---|---|
| SetProductVariantAxesCommand | VALIDATOR_REQUIRED | OrderedDefinitionIds non-null; Guid.Empty forbidden |
| GetProductVariantEditorStateQuery | NO_VALIDATOR_REQUIRED | route Guid + optional locale |
| PreviewProductVariantsQuery | VALIDATOR_REQUIRED | SelectedAxes non-null; DefinitionId non-empty |
| ApplyProductVariantMatrixCommand | VALIDATOR_REQUIRED | SelectedAxes + patch Status TryParse shape |
| GetProductVariantReadinessQuery | NO_VALIDATOR_REQUIRED | route Guid |

Validators emit `CatalogValidationCodes.*` only; no DB/domain duplication.
Status parse also guarded in Apply handler with typed `VariantPatchStatusInvalid`.
