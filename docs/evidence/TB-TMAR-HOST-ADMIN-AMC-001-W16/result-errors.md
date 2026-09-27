# Result / errors — W16 Publish Readiness

| Concern | State |
|---|---|
| Success path | `ApiResponseFactory.From(Result<ProductPublishReadinessView>)` |
| Missing product | `CatalogErrorCodes.WorkspaceProductMissing` (`workspace.product.missing`) → 404 |
| PlatformHttpException | ABSENT on moved HTTP + reader |
| InvalidOperationException expected flow | ABSENT on moved HTTP + reader |
| Message-as-code / ex.Message parsing | ABSENT on moved seam |
| Legacy unwrap | CatalogDirectory `UnwrapHistory` maps missing Result → prior IOE outside HTTP |
| Edit-scope / permission denied on GET | Not applied (view scope allowed) |
