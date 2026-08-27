# 11 — Localized SEO metadata (TB-P06-T016)

| Field | Home | PDP | Blogs |
|---|---|---|---|
| title | fa/en strings | product-driven + locale | locale-aware |
| description | fa/en strings | product-driven | locale-aware |
| canonical | `canonicalForLocale` | same | same |
| alternates.languages | `buildLocaleAlternates` | same | same |
| openGraph.locale | `openGraphLocaleFor` (`fa_IR` / `en_US`) | same | same |

Structured data / breadcrumb URLs remain product/content owned; public links now locale-prefixed via `LocalizedLink` / helpers where rendered.
