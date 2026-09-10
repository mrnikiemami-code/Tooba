# TB-P10-T001 — Performance

- One Cart projection serves header badge, mini-cart, and `/cart` (no per-line product price N+1 from FE).
- Host `StorefrontCartComposer.PresentAsync` batches Catalog variants/products/media/policies.
- Recommendations reuse home payload already loaded for shell — no extra recommendation microservice.
- ATC / mutate paths refresh via `CART_CHANGED_EVENT` rather than polling.
