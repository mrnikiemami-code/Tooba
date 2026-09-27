# Result / errors — W13 ProductMedia

## Stable codes (preserved workspace.*)

| Code | HTTP | Use |
|---|---|---|
| workspace.product.missing | 404 | list/readiness product missing |
| workspace.permission.denied | 403 | view-scope write deny |
| workspace.media.asset.missing | 400 | empty MediaAssetId (validator + directory) |
| workspace.media.attach.rejected | 400 | attach failures incl. Host-mapped product-missing/dup |
| workspace.media.placeholder.rejected | 400 | all placeholder failures |
| workspace.media.empty | 404 | reorder empty gallery |
| workspace.media.order.invalid | 400 | reorder set mismatch |
| workspace.media.order.rejected | 400 | reorder other (incl. Host-mapped product-missing) |
| workspace.media.missing | 404 | primary/patch/detach target (incl. Host-mapped product-missing) |

Registered in CatalogErrorCodes + CatalogErrorCatalogContributor + CatalogErrors.resx/.fa.resx.

## Forbidden on migrated surface

- PlatformHttpException expected flow
- InvalidOperationException catch-to-code
- ex.Message.Contains classification

Legacy ICatalogDirectory thin Unwrap wrappers remain for bootstrap/tests (throw code-as-message IOE).
