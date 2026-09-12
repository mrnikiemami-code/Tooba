# TB-P10-T004-R13 — Network

- No new polling added. Result page keeps existing 1.5s / 20s hard-stop poll for transient online statuses only.
- Payment page: one checkout GET + methods + optional wallet-quote. No ownership retry loop.
- Cart rotation: one GET of stale/converted id, then one POST `/v1/storefront/cart` when shopping. No `cart.rejected` retry.
- No repeated 401 storm: missing proof fails closed once as 403 access-denied.
