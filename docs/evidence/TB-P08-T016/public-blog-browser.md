# TB-P08-T016 — Public Blog routes

## Code / contract (worker)

- Routes: `/fa|en/blogs/{slug}`, `/blogs/category/{slug}`, `/blogs/author/{slug}`
- Locale + slug; no cross-language fallback; sitemap/canonical clients covered by FE routing tests
- FE blogs-public-routing + blogs-copy + content-api tests pass
- BE ContentArticlePublicRouting / ContentTaxonomyPublicRouting in Content filter suite

## Browser (parent)

- [ ] Published fa/en article pages render; media/body safe
- [ ] Category/author public pages
- [ ] Unpublished/scheduled visibility correct
- [ ] Preview does not pollute public indexing
