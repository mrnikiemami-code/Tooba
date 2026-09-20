# SEO rendering baseline — TB-TMAR-FE-BASELINE

## Invariants

1. Canonical product/category/article/store **primary text content** must not depend solely on post-hydration client fetch.
2. Route `generateMetadata` / static `metadata` must remain server-capable.
3. JSON-LD / structured data helpers (`storefront-page-seo`) must remain SSR-safe where used.
4. Canonical URL + alternates must stay locale/store aware (`lib/i18n/routing`) — unlimited-locale safe, not FA/EN-only.
5. Sliders/rails/visual effects are not content authority.
6. Loading optimizations must not remove crawlable primary content.
7. **FE-SEO-001**: New storefront implementation must not make primary indexable content client-only without explicit architecture approval.

## Existing mechanical coverage

- `npm run test:critical-storefront` (home/pdp/listing/category-plp/surface).
- Narrow architecture characterization: `lib/architecture/seo-rendering.guard.test.ts` asserts home/cart metadata modules remain importable server-side patterns (no brittle HTML snapshots).

## Lock

Recorded in `docs/architecture/TMAR-architecture-locks.md` as **FE-SEO-001**.
