# Root Cause — TB-P10-T022-R9-R1

## Primary application stall (90–120s / ~180s captures)

1. **Duplicate Host work on Landing SSR**  
   `generateMetadata` and the page body each called `loadPublishedLandingPage` (and related loaders), so one document paid for two page resolves plus overlapping home/listing work.

2. **Unconditional heavy `/v1/storefront/home` on Landing**  
   Landing render context always loaded full storefront home. `GetHomeAsync` builds home by calling `GetListingAsync` → `BuildProductCardsAsync`, which composes **every** published product (N sequential `ComposeProductAsync` calls). Under load this saturates Host (observed 70s–timeout) while page resolve alone stayed fast (~16–65 ms).

3. **Always-on full products listing fallback**  
   Context also fetched `/v1/storefront/products?sort=newest` even when Host already resolved ProductCollection item ids — doubling Catalog composition cost.

4. **FE saturation amplifier**  
   Concurrent hung `/home` + listing requests inflated WorkingSet and Next SSR wall time (R9 capture: `GET /landing/landing-demo 200 in 179990ms`) while Host page resolve remained healthy.

## Not root cause

- Host `GET /v1/storefront/pages/{slug}` itself (indexed locale+slug; ~16–65 ms warm).
- Missing timeouts / spinner / SEO disable (rejected anti-patterns).

## Repair direction

- React `cache()` canonical resolver shared by metadata + page (LOCK-SF-325).
- Host embeds section-scoped product cards + categories/brands/articles/reviews on public page resolve.
- FE Landing SSR uses embedded shell; does **not** call `/home` or full listing when embed covers section needs.
- Store+locale+page cache tags + Admin revalidate (LOCK-SF-326).
