# TB-P07-T001 — USER-PREVIEW URLs

## Identities
| Role | Value |
| --- | --- |
| Admin actor | `01a036c2-970e-7000-8eb7-94bf5cc2d8db` (`X-Tooba-Dev-Actor-User-Id`, from `/v1/admin/dev-context`) |
| Seeded product | `01a0455c-53c8-7000-a110-061ffa1f936e` / slug `schema-mobile-demo-phone` |
| Mobile category | `01a043f3-30c5-7000-9c2d-2e96d8da1439` |

## Concrete URLs
| Surface | URL |
| --- | --- |
| Admin Attribute Definitions | http://localhost:3000/admin/catalog/attributes |
| Admin Category Schema | http://localhost:3000/admin/catalog/category-schema?categoryId=01a043f3-30c5-7000-9c2d-2e96d8da1439 |
| Admin Product workspace | http://localhost:3000/admin/products/01a0455c-53c8-7000-a110-061ffa1f936e |
| Admin Product attributes | http://localhost:3000/admin/catalog/products/01a0455c-53c8-7000-a110-061ffa1f936e/attributes |
| Storefront seeded PDP | http://localhost:3000/fa/products/schema-mobile-demo-phone |
| Host PDP API | http://127.0.0.1:5088/v1/storefront/products/schema-mobile-demo-phone |
| Original Shopeiva vendor product form | http://localhost:3001/vendor-panel/products/new |

## Preview steps
1. Open Admin Attribute Definitions — list includes `color`, `storage`, `ram`, `screen_size`.
2. Open Category Schema with Mobile categoryId — effective schema shows inheritance order Color/Storage/RAM/Screen.
3. Open Admin Product attributes — set values / variant axes (Color+Storage).
4. Open Storefront PDP — two variants (axis combinations) without full matrix generator.
5. Compare geometry with Shopeiva vendor product form (cards/inputs), not a foreign schema-builder chrome.

## Runtimes
- Backend `:5088` health/live + ready 200
- Tooba FE `:3000` (`-H localhost`)
- Shopeiva `:3001`
