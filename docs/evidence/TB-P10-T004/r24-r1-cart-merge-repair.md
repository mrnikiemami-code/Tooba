# TB-P10-T004-R24-R1 — Cart merge repair

Case A — only anonymous cart: adopt guest; keep guest `cartId` and shipping draft.

Case B — leftover empty authenticated Active + guest with lines: abandon leftover, adopt guest (same cartId through Shipping).

Case B′ — leftover authenticated with lines: merge guest lines into leftover; guest abandoned.

Case C — repeated login: merge only while `guestSecret` is present; after persist, secret is cleared; no double qty.

FE:

- `GET /v1/storefront/cart/current` is canonical authenticated Active cart.
- Empty current + pending guestSecret → one merge.
- Authenticated path does not `POST /v1/storefront/cart` guest create.
- Merge persists only Active carts.

`GET /v1/storefront/cart/current` is mapped before `{cartId}`.
