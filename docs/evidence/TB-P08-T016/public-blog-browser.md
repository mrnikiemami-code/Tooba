# TB-P08-T016 — Public Blog routes

## Code / contract (worker)

- Routes: `/fa|en/blogs/{slug}`, `/blogs/category/{slug}`, `/blogs/author/{slug}`
- Locale + slug; no cross-language fallback; sitemap/canonical clients covered by FE routing tests
- FE blogs-public-routing + blogs-copy + content-api tests pass
- BE ContentArticlePublicRouting / ContentTaxonomyPublicRouting in Content filter suite

## Browser / HTTP (parent)

- [x] Published fa/en article pages render; media/body safe  
  Observed: `http://127.0.0.1:3000/fa/blogs` and detail slug **200**. EN article created via API (`01a06ac1-135f-7000-b45f-7089ee3d1add`); EN public page HTTP not separately logged.
- [ ] Category/author public pages  
  Not exercised this session.
- [ ] Unpublished/scheduled visibility correct  
  Covered by API schedule/publish smoke; public browser matrix for unpublished/scheduled not separately logged.
- [ ] Preview does not pollute public indexing  
  Not exercised this session.
