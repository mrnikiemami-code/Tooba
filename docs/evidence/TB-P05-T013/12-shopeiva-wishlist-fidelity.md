# TB-P05-T013 — Shopeiva Wishlist fidelity

## Reuse decision

The implementation preserves Shopeiva's Persian RTL storefront and customer-panel shells, spacing, cards, navigation, blue commerce CTA treatment and rose heart language. It does not introduce a parallel Wishlist design system. Minimal connection code binds the existing PDP/card hearts and customer Wishlist route to one live provider.

## Evidence map

- `03-customer-wishlist-desktop.png` (`1440x900`): customer-panel shell, three deterministic saved products and live count.
- `04-customer-wishlist-empty.png` (`1440x900`): honest empty actor state and route back to products.
- `05-pdp-wishlist-toggle.png` (`1440x900`): seeded linen PDP shows the active rose `حذف از علاقه‌مندی` state.
- `06-product-card-wishlist-state.png` (`1440x900`): listing search shows the saved linen card and unsaved neighboring product simultaneously.
- `07-wishlist-live-price-availability.png` (`1440x900`): cards render current Storefront amount, purchase availability and the linen product's real `4/3` Published review aggregate.
- `08-wishlist-remove-action.png` (`1440x900`): visible heart removal completed against an isolated actor and refreshed to zero.
- `09-wishlist-mobile-390x844.png` (`390x844`): real responsive customer Wishlist at the exact requested mobile viewport.

## Capture integrity

All images were captured from normal local Development processes with installed Google Chrome in headless standard-browser mode. Data came through the real Next application and Host API using deterministic Development records. No request interception, mocked payload, disabled browser security, DOM editing, image editing or fabricated state was used.
