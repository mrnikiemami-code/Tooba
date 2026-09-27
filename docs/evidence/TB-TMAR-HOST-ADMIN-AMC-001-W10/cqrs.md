# CQRS — W10 Product Attribute Editor/Readiness

| Request | Handler | Validator |
|---|---|---|
| `GetProductAttributeEditorStateQuery` | GetProductAttributeEditorStateHandler | NO_VALIDATOR_REQUIRED |
| `GetProductAttributeReadinessQuery` | GetProductAttributeReadinessHandler | NO_VALIDATOR_REQUIRED |
| `SetProductAttributesCommand` | SetProductAttributesHandler | VALIDATOR_REQUIRED |
| `SetProductAttributeCommand` | SetProductAttributeHandler | NO_VALIDATOR_REQUIRED |

Pipeline: HTTP → `ICatalogAdminAuthorizer` → actor bind → `ISender` → Handler → `IProductAttributeDirectory` → `Result` → `ApiResponseFactory`.

Endpoints inject neither directory nor DbContext.
