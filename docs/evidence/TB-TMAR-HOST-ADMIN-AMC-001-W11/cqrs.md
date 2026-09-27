# CQRS — W11

| Request | Handler | Validator |
|---|---|---|
| `SetProductVariantAxesCommand` | SetProductVariantAxesHandler | REQUIRED |
| `GetProductVariantEditorStateQuery` | GetProductVariantEditorStateHandler | NO_VALIDATOR_REQUIRED |
| `PreviewProductVariantsQuery` | PreviewProductVariantsHandler | REQUIRED |
| `ApplyProductVariantMatrixCommand` | ApplyProductVariantMatrixHandler | REQUIRED |
| `GetProductVariantReadinessQuery` | GetProductVariantReadinessHandler | NO_VALIDATOR_REQUIRED |

Pipeline: HTTP → `ICatalogAdminAuthorizer` → `CatalogActorRequestBinding` → `ISender` → handler → ports → `Result` → `ApiResponseFactory`.

Endpoints inject no directory/DbContext/Offer gateway.
