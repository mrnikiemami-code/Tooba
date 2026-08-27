# 12 — Effective access real resource preview

Task: TB-P06-T024-R1

## Backend

**Method:** `AccessControlDirectory.GetEffectiveAccessAsync`

For Category-scoped permissions:

1. Collect distinct `ScopeResourceId` from role grants.
2. Batch `ICatalogLookupGateway.GetCategoryNamesAsync(categoryIds)`.
3. Emit `EffectivePermissionDto.ScopeDisplayName` per grant (e.g. «موبایل»).

Ceiling-denied grants excluded from effective payload (`DeniedByCeiling` filtered out).

## Frontend

**Access Control Center — Effective Access tab**

- Renders `permissionId`, module, scope badge via `formatScopeLabel(scopeKind, scopeDisplayName, scopeResourceId)`.
- Shows human-readable name when `scopeDisplayName` present — not raw UUID alone.
- Example display: `order.view · دسته: موبایل`

**Role permission rows**

- Selected scope badge uses `grant.scopeDisplayName` from API after save/reload.

## Test proof

`AccessControlRuntimeScopeTests.Seller_order_list_and_detail_respect_category_scope`:

```text
effective.Permissions contains order.view + ScopeResourceId=mobile + ScopeDisplayName=="موبایل"
```

## Not shown

- SpiceDB tuple syntax — not exposed in UI.
- Raw UUID-only labels when Catalog name missing — falls back to truncated id in `ScopeEditor` badge only.
