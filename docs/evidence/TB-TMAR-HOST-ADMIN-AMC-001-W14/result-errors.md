# Result / errors — W14 ProductSeo

## Stable codes (preserved workspace.*)

| Code | HTTP | Use |
|---|---|---|
| workspace.permission.denied | 403 | view-scope PUT deny |
| workspace.product.missing | 404 | product missing |
| workspace.catalog.stale | 409 | ExpectedUpdatedAt mismatch |
| workspace.product.slug.duplicate | 409 | slug uniqueness |
| workspace.product.slug.invalid | 400 | invalid/blank auto-slug source |
| workspace.product.seo.rejected | 400 | reserved generic SEO reject |

Registered in CatalogErrorCodes + CatalogErrorCatalogContributor + CatalogErrors.resx/.fa.resx.

## Forbidden on migrated surface

- PlatformHttpException expected flow
- InvalidOperationException catch-to-code on HTTP/directory
- ex.Message.Contains classification

Legacy ICatalogDirectory `UnwrapSeo` maps codes back to prior IOE messages for ProductSeoTests/publish callers only.
