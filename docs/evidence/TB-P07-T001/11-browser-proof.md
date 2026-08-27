# TB-P07-T001 — Browser / HTTP proof

| Surface | URL | Status |
| --- | --- | --- |
| Admin Attribute Definitions | http://localhost:3000/admin/catalog/attributes | 200 |
| Admin Category Schema | http://localhost:3000/admin/catalog/category-schema | 200 |
| Admin Product attributes | http://localhost:3000/admin/catalog/products/01a0455c-53c8-7000-a110-061ffa1f936e/attributes | 200 |
| Storefront seeded PDP | http://localhost:3000/fa/products/schema-mobile-demo-phone | (FE route; Host API 200 with 2 variants) |
| Host definitions API | GET `/v1/admin/catalog/attribute-definitions` | 200 |
| Host effective schema | GET `/v1/admin/catalog/categories/01a043f3-30c5-7000-9c2d-2e96d8da1439/attribute-schema/effective` | 200 |
| Host seeded PDP | GET `/v1/storefront/products/schema-mobile-demo-phone` | 200 variants=2 |
| Existing sellable PDP | storefront listing first product | 200 non-regressed |
| Shopeiva comparison | http://localhost:3001/vendor-panel/products/new | 200 |

Shopeiva lock: Admin screens use Admin shell + card/input patterns; no foreign schema-builder chrome. FULL_VARIANT_MATRIX UI not present.
