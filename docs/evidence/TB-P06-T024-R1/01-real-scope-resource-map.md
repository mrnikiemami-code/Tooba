# 01 — Real scope resource map

Task: TB-P06-T024-R1  
Parent: TB-P06-T024 (repair)

| ScopeKind | Classification | Owner module | Read contract / HTTP |
|-----------|----------------|--------------|----------------------|
| Category | **LIVE** | Catalog | `ICatalogLookupGateway.ListCategoriesForAccessControlAsync` → Admin/Seller `GET …/scope-resources/categories?q=` |
| Product | **LIVE** | Catalog | `ListProductsForAccessControlAsync` → `…/scope-resources/products?q=` |
| Brand | **LIVE** | Catalog | `ListBrandsForAccessControlAsync` → `…/scope-resources/brands?q=` |
| Warehouse | **DEFERRED** | Inventory (write-only locations) | `…/scope-resources/warehouses` returns `{ deferred: true }` |
| Store | **DEFERRED** | none | `…/scope-resources/stores` returns `{ deferred: true }` |
| OrderSegment | **DEFERRED** | enum only | `…/scope-resources/order-segments` returns `{ deferred: true }` |

## Rules enforced

- No fabricated scope resources; deferred kinds are disabled in `ScopeEditor` (`live: false`).
- No Order→Catalog SQL JOIN for authorization; order lines use `CategoryIdSnapshot` + batch `GetPrimaryCategoryIdsByVariantIdsAsync` backfill only.
- Catalog is the sole Category/Product/Brand read owner for scope pickers.

## Code anchors

- `AccessControlEndpoints.cs` — scope-resources routes (admin, seller, admin/sellers/{id})
- `CatalogDirectory.cs` — `ListCategoriesForAccessControlAsync`, brands, products
- `scope-editor.tsx` — `SCOPE_OPTIONS` live/deferred flags
