# TB-TMAR-WISHLIST-AMC-001-W4-R1 — Semantic fault repair

## Changes

| Location | Before | After |
|---|---|---|
| `Domain/Aggregates/WishlistItem.Create` | prose `InvalidOperationException` for empty owner/product | `SemanticException` + `WishlistErrorCodes.SessionRequired` / `ProductIdRequired` |
| `Infrastructure/Directories/WishlistDirectory.EnsureActor` | prose `InvalidOperationException` | `SemanticException` + `WishlistErrorCodes.SessionRequired` |

## Stable codes

- `customer.session.required` — Foundation-owned descriptor; Wishlist aliases only (no duplicate registration).
- `customer.wishlist.product_id_required` — Wishlist catalog descriptor already present from W3.

## Domain reference

`Tooba.Wishlist.Domain` → `Tooba.Wishlist.Contracts` (same-module; no cycle).
