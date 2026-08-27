# 08 — Runtime order action scope

Task: TB-P06-T024-R1

## Implementation

**File:** `FulfillmentEndpoints` — `EnsureSellerOrderHandleScopeAsync` (private helper before seller fulfillment mutations)

### Logic

1. Load effective access for actor + seller owner scope.
2. Collect enabled `order.handle` permissions (not denied by ceiling).
3. No handle permission → `403 seller.order.handle.denied`.
4. Global handle scope → allow.
5. Category handle scope → build allowed category set.
6. Load order lines; batch-resolve missing snapshots.
7. **Whole-order safety:** `order.Lines.All(line => category authorized)` required.
8. If any line outside scope → `403 seller.order.handle.scope_denied` («اقدام کل‌سفارش خارج از محدودهٔ مجاز است.»)

## Scenarios

| Case | Result |
|------|--------|
| Mobile-only order + Mobile handle scope | ALLOW (all lines Mobile) |
| Books-only order + Mobile handle scope | DENY — not all lines authorized |
| Mixed order + Mobile-only handle | DENY — whole-order action blocked |
| Global handle scope | ALLOW all seller lines |

## Policy note

Mixed-order fulfillment is **denied** for category-scoped handlers unless every line matches — explicit safe default; line-partial fulfillment not exposed in this slice.

## Relation to list/detail

List/detail may show partial mixed orders; handle requires full authorization — stricter than view.

## Test coverage

Direct fulfillment HTTP E2E not in Host.Tests slice; scope logic mirrors list category resolution. Authorization grants tested in `AccessControlRuntimeScopeTests` + foundation Mobile/Books SpiceDB policy tests.
