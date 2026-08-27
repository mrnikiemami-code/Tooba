# 10 — Category-scoped order access foundation

Task: TB-P06-T024

| Layer | Status |
|-------|--------|
| Authorization / policy | LIVE |
| Order-query runtime filtering | DEFERRED |

## Scenario proven (policy)

Seller A employee E with role grants:

- `order.view` + `order.handle`
- `ScopeKind = Category`, `ScopeResourceId = Mobile`

| Check | Decision |
|-------|----------|
| `category/{Mobile}#handle_orders` | ALLOW |
| `category/{Books}#handle_orders` | DENY |

Evidence: `AccessControlFoundationTests.Ceiling_escalation_and_category_scope_policy`  
(InMemory authz after `SyncUserCapabilityTuplesAsync`).

## Implementation points

- Grant storage: `RolePermission` Category + resource id
- Tuple sync: `AccessControlDirectory.SyncUserCapabilityTuplesAsync` for order.* + Category
- Schema: `category.handler` / `handle_orders` in `authorization-foundation.zed`

## Explicit deferral

Full seller Order list/detail query filtering by Category scope across Order module is **not** claimed as integrated.  
No cross-module SQL JOIN added. Future gateway should call SpiceDB category checks at use-case boundary.
