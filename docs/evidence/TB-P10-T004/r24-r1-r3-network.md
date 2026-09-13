# TB-P10-T004-R24-R1-R3 — Network

From `_r24-r1-r3-runtime-raw.json` (Host `:5088`, FE `:3000`).

| Signal | Count | Notes |
| --- | --- | --- |
| `/auth/me` | 96 | mount / AUTH+CART refresh on Home, Cart, Shipping, Payment, mobile. No `setInterval` on auth/me |
| `/cart/merge` | 3 | login + re-login, not a loop |
| `/auth/logout` | 1 | single canonical logout |
| `/shipping/commit` | 2 | one real 200, one injected 409 |
| `/api/auth/me` 401 | 14 | anonymous window after logout (header/cart/menu remount). Not polling |

No cart polling. 409 is the Q fault inject. 404s are post-commit current-cart rotation.
