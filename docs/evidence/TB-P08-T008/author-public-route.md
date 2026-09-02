# author-public-route

Canonical public routes:

- `/fa/blogs/author/{slug}`
- `/en/blogs/author/{slug}`

Author identity is global Content-owned (not duplicated per locale).
Articles on the page are filtered to the active route language only.
API: `GET /v1/content/authors/{slug}?locale=` (locale shapes canonical path only).
