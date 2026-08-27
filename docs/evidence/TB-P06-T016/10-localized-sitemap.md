# 10 — Localized sitemap (TB-P06-T016)

Source: `src/frontend/app/sitemap.ts`

| Requirement | Implementation |
|---|---|
| Locale-prefixed URLs | `localePath(locale, internal)` for each of `LOCALES` |
| Static indexables | Home, products, blogs, merch hubs |
| Products | Listing page 1 (≤50) → `/products/{slug}` × locales |
| Articles | Content articles page 1 (≤50) → `/blogs/{slug}` × locales |
| hreflang | `alternates.languages` fa-IR + en per entry |
| No unprefixed duplicates | Unprefixed public paths not emitted |
| Panels/APIs | Excluded |

Sitemap is frontend-owned; no backend SEO module change.
