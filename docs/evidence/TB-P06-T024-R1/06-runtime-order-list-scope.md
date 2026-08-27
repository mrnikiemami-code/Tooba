# 06 — Runtime order list scope

Task: TB-P06-T024-R1

## Implementation

**File:** `SellerPanelComposer.ListOrdersAsync` (+ dashboard `GetDashboardAsync` via `FilterOrdersByScopeAsync`)

Flow:

1. `ResolveOrderViewScopeAsync(sellerPartyId, actorUserId)` — reads effective `order.view` grants from Access Control.
2. If denied → empty list.
3. If global scope → all seller orders (up to 200).
4. If category scope → filter orders where **any line** has category in allowed set.
5. Line category = `CategoryIdSnapshot ?? batchResolve(CatalogVariantId)`.
6. `lineCount` in list DTO uses `CountAuthorizedLines` — not total lines for scoped actors.

## Mobile-vs-Books scenario (integration test)

**Test:** `AccessControlRuntimeScopeTests.Seller_order_list_and_detail_respect_category_scope`

Employee role «Mobile Order Operator» with `order.view` scoped to Mobile category:

| Order | Lines | Visible in list? |
|-------|-------|------------------|
| SO-MOBILE | Mobile | YES |
| SO-BOOKS | Books | NO |
| SO-MIXED | Mobile + Books | YES (partial visibility) |

## Count / leakage policy

- Books-only order excluded entirely — no row, no count.
- Mixed order appears once; `lineCount` reflects authorized lines only (1 for Mobile scope).
- Dashboard open/paid counts use same `FilterOrdersByScopeAsync` — no aggregate leakage from hidden orders.

## Backend authority

Filtering occurs in Host composer before DTO mapping — **not** frontend filtering.

## T024 delta

TB-P06-T024 evidence `10-category-scoped-order-access-foundation.md` marked order-query filtering **DEFERRED**. This repair implements runtime list filtering in `SellerPanelComposer`.
