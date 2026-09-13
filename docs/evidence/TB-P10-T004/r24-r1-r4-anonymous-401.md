# TB-P10-T004-R24-R1-R4 — Anonymous 401

After canonical logout:

- `markStorefrontSessionAnonymous` caches unauthenticated state.
- Listeners of `AUTH_CHANGED_EVENT` call `loadStorefrontSession` and do not probe `/api/auth/me`.
- Idle on anonymous Home: `authMe401` 4 → 4 (zero growth).
- Full `page.goto` remounts JS and may perform one necessary session resolve (expected 401). That is allowed.
- R4 total anonymous `/api/auth/me` 401 = 8 across the whole A–J + Q run (R3 = 14). No retry-on-401 loop.

`/api/auth/me` still returns real 401 for anonymous callers. No fake authenticated payload.
