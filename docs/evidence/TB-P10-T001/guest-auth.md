# TB-P10-T001 — Guest / auth

- Guest cart: create + secret in sessionStorage; header `X-Tooba-Guest-Secret`.
- CartId alone is insufficient (backend `EnsureAccess`).
- Authenticated cart APIs exist in module (`CreateAuthenticatedAsync`) but storefront composer currently presents guest access path — no silent merge invented in T001.
- Refresh continuity: same tab sessionStorage retains cartId/secret.
- No cart GUIDs shown in UI labels.
