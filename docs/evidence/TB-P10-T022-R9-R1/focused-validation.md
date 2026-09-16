# Focused Validation — TB-P10-T022-R9-R1

## Guards

```text
node --experimental-strip-types --test \
  app/storefront/storefront-ssr-perf-r9r1.guard.test.ts \
  app/storefront/storefront-landing.guard.test.ts \
  app/admin/landing-pages/admin-store-pages-r9.guard.test.ts
→ 13 pass / 0 fail

node --test docs/ai/recovery-staleness.guard.test.mjs
→ 4 pass / 0 fail

npm run test:critical-storefront
→ 20 pass / 0 fail

git diff --check (task files) → clean (CRLF warnings only)
```

## Coverage

- request resolver React `cache` dedupe
- metadata/page share canonical resolver (no raw `loadPublishedLandingPage` on route)
- Host Status filter + ComposeProductCardsAsync embed
- Store+locale+page cache tags + Admin `/api/storefront/revalidate`
- locks 325–327
- recovery SoT Last Implementation = TB-P10-T022-R9-R1
