# Focused Validation — TB-P10-T022-R9

## Host

`dotnet test Tooba.Host.Tests --filter StoreLanding|StorePagesFoundation`

- Passed: 24
- Covers: PageType default Landing, explicit Home, reserved `landing` slug, public resolve Landing-only after SetHome, atomic Home replacement demotion, restore default without Catalog mutation, SEO/NoIndex sitemap exclusion, H1 fallback

## Frontend guards

- `admin-store-pages-r9.guard.test.ts` — PASS (4)
- `admin-landing-pages.guard.test.ts` — PASS
- `admin-nav-integrity.test.ts` — PASS (صفحات فروشگاه)
- `reserved-slugs.test.ts` — PASS (`landing` reserved)

## Recovery / diff

- `node --test docs/ai/recovery-staleness.guard.test.mjs` — PASS (4)
- `git diff --check` — no whitespace errors (CRLF warnings only)

## Critical storefront

- `npm run test:critical-storefront` — PASS (20)

## Notes

- FE SSR for `/landing/{slug}` is slow under concurrent load (~90–120s first compile/render); Host public resolve returns 200 with PageType=Landing quickly.
