# Final Admin Errors — TB-P07-T031

## Central map
- `admin-error-map.ts` — ~50 stable `errorCode` entries with human FA/EN copy
- Unit gates in `admin-error-map.test.ts` (included in FE full suite)

## Live representative error
- `GET /v1/admin/products/00000000-0000-0000-0000-000000000001`
- HTTP 404 body: `{"title":"Not Found","errorCode":"workspace.product.missing"}`
- Evidence: `live-error-sample.json`
- FE normalizes via map → operator-readable message (no raw stack / opaque GUID-only toast as primary copy)

## Other covered codes (spot)
- `workspace.product.category.level.invalid`
- `workspace.product.category.schema-impact`
- `catalog.schema.invalid`
- authorization denied paths
