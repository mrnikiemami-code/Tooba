# platform-error-matrix — TB-TMAR-HOST-ERRORS-AMC-001-W2-CERT

| Code | HTTP | Owner | EN | FA | Uniqueness |
|---|---|---|---|---|---|
| `platform.edition.unconfigured` | 503 | Foundation | YES | YES | exactly once |
| `platform.connection.unconfigured` | 503 | Foundation | YES | YES | exactly once |
| `platform.resolution.failed` | 404 | Foundation | YES | YES | exactly once |

Descriptors: `FoundationErrorCatalogContributor`  
Resources: `FoundationErrors.resx` + `FoundationErrors.fa.resx`  
Producer (touched seam): `TenantResolutionMiddleware` via `SemanticException(SemanticError(FoundationErrorCodes.*))`
