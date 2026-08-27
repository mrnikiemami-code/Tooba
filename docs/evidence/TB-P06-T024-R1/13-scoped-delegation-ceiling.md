# 13 — Scoped delegation ceiling

Task: TB-P06-T024-R1

## Schema

**Migration:** `20260827181000_AddSellerCeilingScope`

Adds to `platform_seller_ceilings`:

- `scope_kind` (int, default GlobalWithinOwner)
- `scope_resource_id` (uuid, nullable)
- Unique index on `(seller_party_id, permission_id, scope_kind, scope_resource_id)`

## Intersection logic

**Method:** `AccessControlDirectory.CeilingAllows`

| Platform ceiling | Seller grant | Allowed? |
|------------------|----------------|----------|
| GlobalWithinOwner for permission | Any scope for same permission | YES |
| Category = Mobile | Category = Mobile | YES |
| Category = Mobile | Category = Books | NO |
| Category = Mobile | GlobalWithinOwner | NO |
| No enabled ceiling row | Any | NO |

Applied in:

- `ValidateGrantsAsync` on `SetRolePermissionsAsync` → `access.escalation.ceiling`
- `GetEffectiveAccessAsync` → marks denied grants, excludes from effective payload

## UI

- Permission catalog marks `disabledByCeiling` when platform seller ceiling blocks delegation.
- ScopeEditor `disabled` when ceiling blocks.
- Admin seller ACC ceiling tab uses ScopeEditor for Category-scoped ceiling rows.

## Tests

| Test | File |
|------|------|
| Category ceiling blocks Books grant | `AccessControlRuntimeScopeTests.Category_ceiling_intersection_blocks_outside_scope` |
| Global grant blocked under Category ceiling | same test — `denyGlobal` |
| Foundation escalation | `AccessControlFoundationTests.Ceiling_escalation_and_category_scope_policy` |

## Example (task scenario)

Platform ceiling: `order.handle`, Category=Electronics (or Mobile in tests)  
Seller role attempt: `order.handle`, Category=Books → **DENY** at persist time.
