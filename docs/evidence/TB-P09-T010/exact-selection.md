# Exact selection

- `pack_selected` requires non-empty selections and homogeneous packable lines.
- Empty `pack_selected` → `fulfillment.bulk.incompatible`.
- Oversized qty → `fulfillment.selection.qty_exceeded`.
- `mark_packed` with no selections packs remaining eligible qty only (explicit pack-all).
- FE sends `{orderLineId, quantity}` from the qty input; no sibling expansion.
