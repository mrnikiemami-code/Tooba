# seo-sitemap

Category SEO reuses category SeoTitle/SeoDescription + existing Next metadata helpers.
Author SEO derived from DisplayName/ShortBio; profile media via DAM URLs when present.

Sitemap (`src/frontend/app/sitemap.ts`):
- categories listed per locale when Active (canonicalPath)
- authors listed per locale path when Active
- no fake cross-language hreflang pairs for taxonomy entries
