# TB-P10-T004-R13 — Active Cart rotation

On successful Order commit:

1. Persist committed checkout proof (source cartId + source secret).
2. `clearCartSession()` — active pointer only.
3. Next `ensureStorefrontCart` / AddToCart `POST /v1/storefront/cart` → new Active Cart, new secret.

`ensureStorefrontCart` also rotates if stored current Cart is Converted/Cancelled/Expired or GET 401/403/404. No `cart.rejected` retry.

`parseCartResponse` / `persistActiveCartSession` refuse to write a non-Active Cart as the shopping session.

`loadStorefrontCart` returns null and detaches when the stored Cart is not Active (back/forward does not revive Converted as mutable).

Host still rejects mutation on Converted (`EnsureActive`). Normal client never POSTs there after this repair.
