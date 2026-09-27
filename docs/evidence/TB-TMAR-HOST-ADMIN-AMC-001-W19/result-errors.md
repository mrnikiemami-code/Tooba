# W19 — Result / errors

## Pipeline

HTTP → authorizer → ISender → GetProductWorkspaceQuery → Result&lt;ProductWorkspaceView&gt; → ApiResponseFactory.From

## Missing product

`Result.Failure` with `CatalogErrorCodes.WorkspaceProductMissing` (`workspace.product.missing`) → 404 via existing Catalog error catalog contributor.

## Forbidden on moved GET

- PlatformHttpException
- expected InvalidOperationException as transport flow
- ex.Message classification / parsing
