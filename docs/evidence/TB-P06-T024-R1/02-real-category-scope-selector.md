# 02 — Real Category scope selector

Task: TB-P06-T024-R1

## Owner / API

| Layer | Implementation |
|-------|----------------|
| Domain owner | Catalog module (`Categories` table + localized names) |
| Gateway | `ICatalogLookupGateway.ListCategoriesForAccessControlAsync(search, ct)` |
| HTTP (Admin) | `GET /v1/admin/access-control/scope-resources/categories?q=` |
| HTTP (Seller) | `GET /v1/seller/access-control/scope-resources/categories?q=` |
| HTTP (Admin seller ceiling) | `GET /v1/admin/sellers/{sellerId}/access-control/scope-resources/categories?q=` |

Handler: `AdminListCategoriesAsync` / `SellerListCategoriesAsync` in `AccessControlEndpoints.cs` — calls Catalog gateway only; returns `{ deferred: false, items: [{ categoryId, parentCategoryId, name, status }] }`.

## UI wiring

- Shared `ScopeEditor` (`src/frontend/app/access-control/scope-editor.tsx`) — ScopeKind `2` = «دسته».
- Loaded via `access-control-api.ts` → `fetchScopeResources` → `SCOPE_KIND_PATH[2] = "categories"`.
- Embedded in `access-control-center.tsx` for role permissions and seller ceiling rows (`supportsScopedEditor` + `loadResources`).

## Behaviors proven in code

| Requirement | Status |
|-------------|--------|
| Real Category IDs from Catalog | YES — DB `category_id` |
| Real localized names | YES — `GetCategoryNamesAsync` / item `name` |
| Hierarchy hint | YES — `parentCategoryId` returned (flat searchable list in UI; no foreign tree widget) |
| Search | YES — server-side filter on name + GUID substring; client debounce via `q` state |
| Select / remove / clear | YES — list click sets resource; badge + X clears |
| Loading / empty / error | YES — `loading`, «موردی یافت نشد», error message |
| No free-form string scope | YES — pick from list only |
| No raw UUID entry in normal UI | YES — search may match GUID but selection is named row |
| Persist `PermissionId + ScopeKind=Category + CategoryId` | YES — `SetRolePermissionsAsync` / ceiling PUT |

## Persistence validation

- Unknown category ID rejected: `AccessControlRuntimeScopeTests.Unknown_category_scope_is_rejected` → `access.scope.unknown_resource`.
- Catalog lookup in `AccessControlDirectory.ValidateGrantsAsync` via `FindCategoryAsync`.

## Tenant / seller filtering

- Seller and Admin pickers share the same Catalog list (platform catalog). Seller-specific category ownership filtering is **not** implemented at picker level — scope resource must exist in Catalog globally.
