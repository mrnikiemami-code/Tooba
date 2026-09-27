# Result / errors — W15 Product History read

## Canonical path

`Result` / `Result<T>` + `ApiResponseFactory.From` on Catalog endpoint.

## Stable code

| Code | HTTP | Surface |
|---|---|---|
| `workspace.product.missing` | 404 | ProductHistoryReader → SemanticError → ApiResponseFactory |

Already registered in CatalogErrorCodes + CatalogErrorCatalogContributor + resx (W13/W14).

## Absent on moved surface

- PlatformHttpException
- Expected InvalidOperationException control flow in HTTP/reader
- Exception.Message / Contains classification

## Legacy outside HTTP seam

`CatalogDirectory.UnwrapHistory` may throw IOE with prior Persian message for thin `ListProductHistoryAsync` callers only.
