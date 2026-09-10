# Cart → Shipping handoff

- Cart CTA → `/shipping` (`data-testid=cart-checkout-cta`).
- `/checkout` redirects to `/shipping`.
- Shipping loads authoritative cart via `POST /v1/storefront/shipping/projection` (guest secret + ownership).
- Empty/missing cart → localized error; totals never trusted from client.
