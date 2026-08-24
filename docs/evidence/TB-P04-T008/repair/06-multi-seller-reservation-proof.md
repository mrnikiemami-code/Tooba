# Multi-seller reservation isolation

Same Catalog product, two Offers (`offer1` stock 2, `offer2` stock 5) in `CartFoundationTests`:

- Add offer1 qty 2 (max).
- Add offer2 qty 1 (separate reservation).
- Over-max on offer1 throws; offer2 line remains.
- Decrease offer1 restores availability on offer1 only.

One Seller Offer's hold does not release the other Seller's inventory.
