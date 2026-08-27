# 09 — Seller order UI scope proof

Task: TB-P06-T024-R1

## Principle

Seller Orders UI consumes **backend-scoped** API responses. No client-side category filtering added.

## Data path

```
/vendor-panel/orders  →  Seller panel API  →  SellerPanelComposer.ListOrdersAsync
/vendor-panel/orders/[id]  →  GetOrderAsync
```

Composer applies `ResolveOrderViewScopeAsync` + line filtering before returning DTOs.

## UI behavior (scoped employee)

| Surface | Expected |
|---------|----------|
| Order list rows | Only orders with ≥1 authorized category line |
| Line count column | Authorized line count only |
| Order detail | Authorized lines + scoped totals |
| Fulfillment actions | Backend 403 if whole-order handle unsafe |

## Visual contract

- No Shopeiva layout/CSS changes to Seller Orders screens in this repair.
- Scope enforcement is data/shape change only (fewer rows/lines), not new chrome.

## Proof type

| Layer | Evidence |
|-------|----------|
| Backend integration | `AccessControlRuntimeScopeTests.Seller_order_list_and_detail_respect_category_scope` |
| UI wiring | Existing vendor orders pages unchanged structurally |
| Live browser | Deferred — see `15-browser-proof.md` |

## Dashboard

`GetDashboardAsync` uses same filter — open/paid KPIs cannot include hidden Books orders.
