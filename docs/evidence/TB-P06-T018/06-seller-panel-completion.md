# 06 — Seller panel completion (TB-P06-T018)

## Selected Seller gaps closed

### 1) Navigation honesty

Hidden from primary nav (not advertised as live / “به‌زودی” in primary chrome):

- `/vendor-panel/customers`
- `/vendor-panel/coupons`
- `/vendor-panel/reviews`
- `/vendor-panel/tickets`
- `/vendor-panel/gift-cards`

Live primary nav remains for Host-backed operational routes: dashboard, products, orders, fulfillments, returns, analytics, wallet, **settings**.

Deep-link capability shells may remain for deferred routes so bookmarks stay honest.

### 2) Settings — live operational page

- `/vendor-panel/settings` binds to **seller dashboard API** operational read model.
- Shows live operational context the seller already has authority to see.
- **No fake business profile save** / no invented store-edit mutation.
- Business profile edit intentionally deferred.

### 3) Dashboard quick actions

- Settings included as a live quick action.
- Stub N/A tile for settings removed.

## Explicit non-claims

- No seller customers CRM module.
- No coupons/discounts CRUD (Promotion/Pricing owner not opened this wave).
- No seller review moderation/response UI.
- No tickets / gift-cards.
- No fake charts or invent counts beyond existing live seller API fields.

## Ownership / isolation

- Existing seller Actor + SellerParty context headers unchanged.
- No request-supplied identity authority change.
- SpiceDB seller isolation preserved (frontend-only presentation wave).
