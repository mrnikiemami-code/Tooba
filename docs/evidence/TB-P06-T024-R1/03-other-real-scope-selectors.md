# 03 — Other real scope selectors

Task: TB-P06-T024-R1

## Product — LIVE

| Item | Detail |
|------|--------|
| Gateway | `ListProductsForAccessControlAsync` — published products, localized titles |
| HTTP | `…/scope-resources/products?q=` (admin + seller + admin/seller) |
| UI | ScopeKind `3` «محصول» in `ScopeEditor`; search + list select |
| Persist | `ScopeKind.Product` + real `productId` |

## Brand — LIVE

| Item | Detail |
|------|--------|
| Gateway | `ListBrandsForAccessControlAsync` — brands + localized names |
| HTTP | `…/scope-resources/brands?q=` |
| UI | ScopeKind `4` «برند» |
| Persist | `ScopeKind.Brand` + real `brandId` |

## Warehouse — DEFERRED

| Item | Detail |
|------|--------|
| Classification | `NO_REAL_RESOURCE_YET` — Inventory locations exist for writes but no ACC read picker contract |
| HTTP | `AdminDeferredScopeAsync` / `SellerDeferredScopeAsync` → `{ deferred: true, items: [] }` |
| UI | Option disabled with «(به‌زودی)»; amber message if forced |

## Store — DEFERRED

Same deferred endpoint pattern; no canonical Store read owner.

## OrderSegment — DEFERRED

Enum/domain concept only; deferred endpoint; not selectable in UI.

## Summary table

| ScopeKind | Selector status |
|-----------|-----------------|
| GlobalWithinOwner (1) | N/A — no resource picker |
| Category (2) | LIVE |
| Product (3) | LIVE |
| Brand (4) | LIVE |
| Warehouse (5) | DEFERRED |
| Store (6) | DEFERRED |
| OrderSegment (7) | DEFERRED |

All live selectors share the same `ScopeEditor` + `listScopeResources` path; no cross-module SQL JOIN.
