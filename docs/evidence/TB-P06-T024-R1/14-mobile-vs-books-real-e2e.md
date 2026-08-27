# 14 — Mobile vs Books real E2E

Task: TB-P06-T024-R1

## Scope of «E2E» in this evidence

| Layer | Method | Status |
|-------|--------|--------|
| Authorization + order DB integration | Host.Tests with Testcontainers PostgreSQL | **PROVEN** |
| Live checkout + real Catalog categories | Not automated in this slice | **Deferred to runtime** |
| Browser UI walkthrough | No screenshots captured | **Deferred** |

## Integration test scenario (real persistence)

**Test:** `AccessControlRuntimeScopeTests.Seller_order_list_and_detail_respect_category_scope`

### Entities (in-test)

| Entity | ID pattern | Name |
|--------|------------|------|
| Category Mobile | `e5e5e5e5-…` | موبایل |
| Category Books | `f6f6f6f6-…` | کتاب |
| Seller | `a1a1a1a1-…` | — |
| Employee | `d4d4d4d4-…` | — |
| Role | «Mobile Order Operator» | `order.view` + `order.handle` @ Mobile |

### Orders (CheckoutGroup.Submit → real Order DB)

| Order | Lines |
|-------|-------|
| SO-MOBILE | Mobile snapshot |
| SO-BOOKS | Books snapshot |
| SO-MIXED | Mobile + Books snapshots |

### Assertions

| Check | Result |
|-------|--------|
| Effective access shows Mobile name | PASS |
| List includes Mobile + Mixed | PASS |
| List excludes Books-only | PASS |
| Mixed authorized lines = Mobile only | PASS |
| Books-only has zero authorized lines | PASS |

## Live E2E checklist (runtime verification)

Use legitimate APIs/UI only:

1. Ensure Catalog has Mobile + Books categories (or seed equivalents).
2. Create offers/products under each category.
3. Place Mobile order, Books order, mixed order via storefront checkout.
4. Admin: set seller ceiling if needed; create Seller role with ScopeEditor → select real Mobile category.
5. Assign employee; open Seller Orders — Mobile visible, Books hidden.
6. Direct Books detail URL → 403.
7. Mobile fulfillment action → allow; Books → deny.

**No direct DB mutation** for authorization proofs.

## Foundation layer (SpiceDB policy)

`AccessControlFoundationTests.Ceiling_escalation_and_category_scope_policy` — Mobile ALLOW / Books DENY at authorization adapter after tuple sync.
