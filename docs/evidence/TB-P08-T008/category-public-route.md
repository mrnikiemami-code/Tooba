# category-public-route

Canonical public routes (repo convention `/blogs`, locale-prefixed):

- `/fa/blogs/category/{slug}`
- `/en/blogs/category/{slug}`

Lookup: active Content category by persisted LanguageCode + slug; no cross-language fallback.
Articles listed via `categorySlug` + locale with server paging (`/v1/content/articles`).
API: `GET /v1/content/categories/{slug}?locale=`
