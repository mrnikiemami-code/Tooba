# TB-P10-T004-R24-R1 — Cart continuity discovery

Session/Auth stores identity only. Cart is server-side `ShoppingCart`.

| Stage | Anonymous Cart ID | Authenticated Cart ID | Cart Status | Lines | CustomerId | Mutation source | Expected? |
| --- | --- | --- | --- | --- | --- | --- | --- |
| A anonymous before login | guest G | none | Active | N | none | ATC / guest POST | yes |
| B OTP complete | G still in session + guestSecret | leftover L may exist | Active | G=N, L often 0 | user | `/api/auth/otp-complete` | identity only |
| C merge/adopt | G | if L empty + G has lines → abandon L, adopt G | Active | N | user | `MergeAnonymousAfterLoginAsync` | yes after R24-R1 |
| D authenticated lookup | — | adopted G (not empty L) | Active | N | user | `GET /v1/storefront/cart/current` | yes |
| E redirect Shipping | same G | same G | Active | N | user | FE `loadStorefrontCart` | yes |
| F Shipping load | same G | same G | Active | N | user | projection by current cartId | yes |
| G address save | same G | same G | Active | N | user | shipping selection | yes |
| H continue-to-payment | same G | same G | Active until COMMIT | N | user | `shipping/commit` | yes |
| I Converted | G | G Converted | Converted | N snapshot | user | atomic checkout COMMIT only | yes |

Exact empty-cart path before repair:

1. Demo user `09111111111` had leftover empty Active authenticated carts from prior runtimes.
2. Merge copied/switched onto that leftover empty cart while Shipping still projected the guest cartId (two IDs).
3. FE `ensureStorefrontCart` after 403/404 posted a new empty guest cart and shadowed the merged cart.
4. `loadAuthenticatedCurrentCart` persisted leftover empty `cartId` and cleared `guestSecret`, so merge could not run.
5. Header badge followed session `cartId` (empty leftover). Shipping React `projection` stayed on guest lines.

Repair: abandon empty leftover + adopt guest; authenticated FE prefers `/cart/current` and merges while guestSecret remains; do not POST guest cart when authenticated; Shipping reads `loadStorefrontCart()`.
