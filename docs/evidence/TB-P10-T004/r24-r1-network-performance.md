# TB-P10-T004-R24-R1 — Network / performance

- One `/api/auth/me` per header mount + event refresh (AUTH_CHANGED). No auth polling.
- Header cart: one `loadStorefrontCart` per CART_CHANGED / mount. No cart polling.
- Merge runs once after login (guestSecret present) and is not repeated on every navigation after secret clear.
- Address save is explicit PUT, not duplicated on projection GET.
- Authenticated pages do not POST a new guest cart.
