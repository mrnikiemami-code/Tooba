# 10 — Single-store fulfillment isolation (TB-P06-T009)

## Edition context

- Fulfillment uses same tenant/commerce context as other modules via `ICurrentCommerceContext`.
- Schema `fulfillment` is per-tenant PostgreSQL database (module DbContext pattern).

## Single-store checkout

- Checkout with one seller → one `SellerOrder` → one `FulfillmentUnit`.
- Same handoff and lifecycle as marketplace; no separate code path.

## Test fixture

- `OutboxTestContextFactory.SingleStore("store-fulfill", "tenant-fulfill")` in foundation tests.
- `OutboxTestContextFactory.SingleStore("store-multi", "tenant-multi")` for multi-seller case.

## Boundary

- Fulfillment data never joins Order tables at query time.
- Store/tenant isolation enforced by connection resolver + commerce context, not SQL cross-schema joins.
