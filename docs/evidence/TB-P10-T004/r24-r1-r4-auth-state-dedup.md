# TB-P10-T004-R24-R1-R4 — Auth state dedup

One module-level Storefront session cache in `storefront-identity-api.ts`.

- Concurrent `loadStorefrontSession` callers share one in-flight `/api/auth/me`.
- After 200 or 401, later consumers (desktop + mobile header, cart, checkout policy) reuse the cache.
- `invalidateStorefrontSession` runs only on login so the next resolve is one new `/me`.
- No query-library refetch-on-focus. No per-component fetcher.

Focused tests: concurrent consumers = 1 fetch; invalidate = one new fetch.
