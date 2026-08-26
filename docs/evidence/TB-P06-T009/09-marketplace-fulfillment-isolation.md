# 09 — Marketplace fulfillment isolation (TB-P06-T009)

## Model

- One checkout may contain multiple `SellerOrder` records (one per seller).
- Payment success event carries all `SellerOrderIds`.
- Fulfillment creates **one `FulfillmentUnit` per SellerOrder**, not per checkout.

## Isolation keys

| Scope | Filter field |
|---|---|
| Seller list | `seller_party_id` |
| Customer view | `checkout_id` (returns all seller fulfillments for checkout) |
| Uniqueness | `seller_order_id` (unique index) |

## Test coverage

- `Multiple_shipments_and_seller_scoped_listing_work_on_postgres`:
  - Checkout with seller A + seller B → 2 fulfillments
  - `ListForSellerAsync(sellerA)` returns only A's fulfillment
  - `ListForSellerAsync(sellerB)` returns only B's fulfillment

## No cross-seller leakage

- Seller GET/mutate endpoints verify `snapshot.SellerPartyId == authorized sellerPartyId`.
- Admin endpoints see all fulfillments (read-only in this task).
