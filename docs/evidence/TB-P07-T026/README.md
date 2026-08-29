# TB-P07-T026 evidence

## Scope
Admin Catalog UX Repair I — central errors, Category attribute create+attach, Filters/Facets human UX, hide technical Schema nav, fix double-active nav.

## Root cause (create attribute / Bad Request)
Host JSON binding rejected string enums (`valueKind: "Text"`, `displayType: "SearchableSelect"`) with raw ASP.NET `Bad Request` (no `errorCode`).

## Fixes
- Shared `parseAdminProblemErrorCode` / `normalizeAdminClientError` in `admin-error-map.ts`; facet/mega-menu/category/attribute clients use it (never toast `title`).
- Extended map: `catalog.schema.invalid`, `catalog.facet.invalid`, `catalog.facet.missing`.
- FE wires numeric enums on create/facet upsert; Host `JsonStringEnumConverter` for string enums.
- Category attribute create+bind: strip invalid variant axis; fa/en behavior explanations; inherited/local human sections.
- Filters tab renamed + helper copy; mapAdminErrorMessage on save/load.
- Technical `/admin/catalog/category-schema` removed from live nav (deferred deep-link only).
- `isActiveAdminNavItem` query/sibling-aware; product list vs `?create=1` no longer double-active.

## Live verification (2026-08-29)
- CREATE attribute definition: 201 (numeric + string after Host rebuild)
- BIND to category: 201
- FACET upsert: 204; effective reload contains definition
- Duplicate: 409 + `catalog.attribute.code.duplicate` + localized title
- FE `/fa/admin/products` 200; Shopeiva `:3001` 200; Host health 200
- Schema label not present in normal categories page chrome

## Validation
- Backend Host.Tests: 342 passed, 0 failed, 0 skipped
- Frontend: typecheck, lint, test:admin, test:category-tree, test:product-workspace, production build — FE_OK

## USER_VISUAL_ACCEPTED
NO
