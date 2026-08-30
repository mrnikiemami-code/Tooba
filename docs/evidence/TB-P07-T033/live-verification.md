# Live verification — TB-P07-T033

## Runtime
- Host `:5088` Development, FE `:3000`, Shopeiva `:3001`

## Endpoint
`POST /v1/admin/catalog/demo/reset-and-seed` (opt-in `Tooba:CatalogDemo:AllowResetAndSeed=true`)

## First run (200)
| Metric | Count |
|--------|------:|
| Roots | 15 |
| L2 | 28 |
| L3 | 73 |
| Brands | 22 |
| Tags | 36 |
| AttributeDefinitions | 41 |
| AttributeOptions | 78 |
| Category bindings | 190 |
| Facets | 117 |
| Category media assignments | 247 |
| MegaMenu placements | 99 |
| Products | 0 |

## Second run (200)
Same counts — no duplicate foundations. Reset+seed clears demo seam entities then reseeds.

## Status
`GET /v1/admin/catalog/demo/status` → rootsDemo=15, brandsDemo=22, tagsDemo=36, attributesDemo=41, products=0 (foundation only).

## Guards
Production / missing opt-in → 403 (covered by unit tests).
