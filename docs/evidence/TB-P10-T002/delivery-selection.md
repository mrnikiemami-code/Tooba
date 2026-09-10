# Delivery selection

- Date cards from backend horizon starting at minimum; earlier dates disabled/rejected.
- Time select from backend windows; forged earlier date → 400 `shipping.delivery.too_early`.
- Selection persisted in `cart_shipping_drafts` and snapshotted on commit.
