# 07 — Localized sitemap proof (TB-P06-T016-R1)

## Purpose

Prove `sitemap.xml` emits locale-prefixed URLs for both `fa` and `en`, includes xhtml hreflang alternates, and does **not** emit unprefixed `/products` (or other public) duplicates.

Machine source: `_acceptance-proof.json` → `sitemap`

## Probe summary

| Field | Value |
|---|---|
| `GET http://127.0.0.1:3000/sitemap.xml` | **200** |
| `hasFa` | **true** |
| `hasEn` | **true** |
| `hasUnprefixedProducts` | **false** |
| `entryCount` | **30** (~30 locale-prefixed entries) |

## Sample entries (from probe `sample`)

```xml
<url>
  <loc>http://127.0.0.1:3000/fa</loc>
  <xhtml:link rel="alternate" hreflang="fa-IR" href="http://127.0.0.1:3000/fa" />
  <xhtml:link rel="alternate" hreflang="en" href="http://127.0.0.1:3000/en" />
</url>
<url>
  <loc>http://127.0.0.1:3000/en</loc>
  <xhtml:link rel="alternate" hreflang="fa-IR" href="http://127.0.0.1:3000/fa" />
  <xhtml:link rel="alternate" hreflang="en" href="http://127.0.0.1:3000/en" />
</url>
<url>
  <loc>http://127.0.0.1:3000/fa/products</loc>
  <xhtml:link rel="alternate" hreflang="fa-IR" href="http://127.0.0.1:3000/fa/products" />
  <xhtml:link rel="alternate" hreflang="en" href="http://127.0.0.1:3000/en/products" />
</url>
```

## Requirements map

| Requirement | Observed |
|---|---|
| Locale-prefixed `<loc>` | Yes (`/fa`, `/en`, `/fa/products`, …) |
| Both locales present | `hasFa` + `hasEn` |
| No unprefixed `/products` duplicates | `hasUnprefixedProducts = false` |
| xhtml hreflang on entries | Present in sample |
| Frontend-owned | `app/sitemap.ts` (no backend SEO module change) |
| Panels/APIs excluded | Not in public sitemap set |

## Entry budget

~30 entries covers Home + listing hubs + product/article page-1 indexables × locales under the existing sitemap generator caps (≤50 products / ≤50 articles × locales, plus static hubs). Exact count at proof time: **30**.

## Verdict

Sitemap is locale-prefixed, bilingual, and free of unprefixed public product duplicates.
