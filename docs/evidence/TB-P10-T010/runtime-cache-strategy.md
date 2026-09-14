# Runtime / cache

- Page lookup key: `store-landing-page:{scope}:{locale}:{slug}` (IMemoryCache). Published TTL 2m; miss/draft 15s.
- Home key: `store-landing-home:{scope}`.
- Invalidate on create/update/status/home write (previous and new slug).
- Lookup is unique-index SingleOrDefault, not a table scan.
- Adding a Page is a DB write; no Next.js page generation and no frontend rebuild/redeploy.
- Extension: replace IMemoryCache with distributed cache using the same key + invalidation points.
