# TB-P07-T041-R1 — Bounded client list review

Re-checked T041 client-side classifications:

| Surface | Classification | Rationale |
|---------|----------------|-----------|
| Settlement balances | SMALL_BOUNDED_CLIENT_SAFE | Per-seller / admin all-sellers still small operational set; not open-ended transactional feed |
| Promotions | SMALL_BOUNDED_CLIENT_SAFE | Catalog of promo definitions, not unbounded order stream |
| Attribute Definitions | SMALL_BOUNDED_CLIENT_SAFE | Schema catalog |
| Category Schema attrs | SMALL_BOUNDED_CLIENT_SAFE | Per-category bounded schema |
| Gift Cards | SMALL_BOUNDED_CLIENT_SAFE | Admin inventory of issued cards in current demo/ops scale |

No classification change in R1. If any later grows without a hard bound, move to server GridQuery in a dedicated task.

`BoundedListGridQueryEngine` may only serve these (or tests), never the nine NON_TRIVIAL production `/query` paths.
