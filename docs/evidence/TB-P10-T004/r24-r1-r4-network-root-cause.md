# TB-P10-T004-R24-R1-R4 — Network root cause (R3)

R3 recorded `auth/me=96`, `merge=3`, `14` anonymous `/api/auth/me` 401s. None of those were `setInterval` polling.

## Why auth/me = 96

`loadStorefrontSession` fetched `/api/auth/me` on every caller with `cache: "no-store"` and no in-flight dedupe.

Per full page mount the same document created several independent callers:

- desktop `StorefrontAccountMenu`
- mobile `StorefrontAccountMenu` (always mounted, drawer hidden)
- header badge `loadStorefrontCart` → `isStorefrontAuthenticated` → raw `/api/auth/me`
- cart page `loadStorefrontCart` plus `requiresCheckoutLogin` (another `/me`)
- `AUTH_CHANGED_EVENT` / `CART_CHANGED_EVENT` fan-out after login, logout, merge, and ATC
- React StrictMode / Next.js remount doubling some effects
- R3 A–R visited Home, Cart, Shipping, Payment, mobile, and fault-prep pages

96 is mount/refresh multiplication, not an interval.

## Why merge = 3

One login transition called merge from three places at once:

1. `storefront-login.tsx` after OTP
2. header `loadStorefrontCart` after `notifyAuthChanged` (guest secret still present)
3. cart/shipping `loadStorefrontCart` / `ensureStorefrontCart` on the same transition

R3 therefore posted merge three times around two intended logins (first login + re-login), not three distinct clean transitions.

## Why 14 anonymous 401s

After logout every remount treated unknown session as a fresh probe. Desktop+mobile menus, badge, and cart each called `/api/auth/me`. Expected 401 was treated as a miss, not as known anonymous state. Remaining A–R anonymous navigations multiplied those probes. Not a retry-on-401 loop and not idle polling.

## Repair

Canonical in-memory session cache + in-flight dedupe; `markStorefrontSessionAnonymous` after logout; merge in-flight + per-user transition lock.
