# Runtime — TB-P10-T022-R9-R1

Host `:5088` + FE `:3000` (Next.js 15.5.23).

## A. Cold Landing

| Metric | Value |
|--------|-------|
| FE log | `GET /landing/landing-demo 200 in 5485ms` |
| Notes | Includes Next compile of `/landing/[slug]` (~4.1s) |

## B. Warm Landing ×3

| Request | FE log |
|---------|--------|
| warm1 | `200 in 251ms` |
| warm2 | `200 in 325ms` |
| warm3 | `200 in 248ms` |

After revalidate: `676ms` then `205 / 203 / 218 / 203ms`.

## C. Home

| Metric | Value |
|--------|-------|
| `GET /` (follow redirects) | `200` in **1949ms**, default home |
| Notes | Canonical Shopeiva home still uses `/home`; custom Home uses embedded shell |

## D. Other locale

| Metric | Value |
|--------|-------|
| `x-tooba-locale: en` on `/landing/landing-demo` | `404` (page published for `fa` only) |

## E. Metadata / SEO intact

From warm Landing HTML:

- title: `صفحهٔ فرود دمو`
- canonical: `/fa/landing/landing-demo`
- og:title present
- `application/ld+json` present
- `data-testid="store-page-primary-h1"` present
- `data-testid="landing-route"` present
- robots/OG wired via `buildStorePageMetadata`

## F. Invalidation

`POST /api/storefront/revalidate` → `200`  
`{"ok":true,"tags":["storefront-pages:default","storefront-appearance","storefront-home-selection:default","storefront-page:default:fa:landing-demo","storefront-page:default:fa:home"]}`  
Subsequent Landing SSR recovered to ~200ms.

## G. Store isolation

Cache tags always include `storeScope` (`storefront-page:{store}:{locale}:{slug}`, `storefront-pages:{store}`). Admin invalidation posts scoped tags only.

## Host after repair

| Call | Time |
|------|------|
| cold page resolve (with embedded shell) | ~8852ms first compose |
| warm page resolve | **42ms** |
| health | ~26ms |
