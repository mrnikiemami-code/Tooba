# 16 — Scope runtime tests

Task: TB-P06-T024-R1

## Backend — AccessControlFoundationTests

| Test | Covers |
|------|--------|
| `AccessControl_module_boundary_static_checks` | Module boundary, `/scope-resources/categories`, `/me/capabilities` routes |
| `Ceiling_escalation_and_category_scope_policy` | Ceiling, escalation deny, Mobile/Books SpiceDB policy |

## Backend — AccessControlRuntimeScopeTests

| Test | Covers |
|------|--------|
| `Unknown_category_scope_is_rejected` | Fake category ID → `access.scope.unknown_resource` |
| `Category_ceiling_intersection_blocks_outside_scope` | Books outside Mobile ceiling; global grant under category ceiling |
| `Seller_order_list_and_detail_respect_category_scope` | Mobile list allow, Books filter, mixed line leakage, effective display name |

## Supporting test doubles

- `FakeCatalogLookupGateway` — categories, names, access-control list APIs
- `FakeAccessControlDirectory` — used in other Host tests where applicable

## Frontend

No dedicated `access-control` or `order-scope` test file added in this repair. Related:

- `story-capabilities.test.ts` — story action matrix (orthogonal)
- `panel-nav-integrity.test.ts` — nav live-only filter (**1 failure** at time of log — see `18-final-validation.md`)

## Requirement matrix (task §Q)

| Requirement | Backend test |
|-------------|--------------|
| Real Category selector persistence | Implicit via runtime tests + endpoints static check |
| Fake Category rejected | `Unknown_category_scope_is_rejected` |
| Scope outside ceiling rejected | `Category_ceiling_intersection_blocks_outside_scope` |
| Mobile list allow / Books filter | `Seller_order_list_and_detail_respect_category_scope` |
| Books detail denied | Same (zero authorized lines) |
| Mixed no leakage | Same |
| Scoped ceiling intersection | `Category_ceiling_intersection_blocks_outside_scope` |
| Product/Brand real scope | Endpoint wiring; no dedicated runtime rejection test |
| Nav hidden on view deny | Shell logic — no automated FE test in slice |
| Direct endpoint deny | Fulfillment handle — logic tested indirectly via composer patterns |
| Tenant isolation | SellerPartyId checks in composer (existing) |

## Full suite log

See `dotnet-test-full.log` / `dotnet-test-rebuild.log` in this folder.
