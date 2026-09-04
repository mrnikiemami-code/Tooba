# TB-P08-T014 — Preview security

- Admin route: `/admin/content/articles/{id}/preview` + `GET /v1/admin/content/articles/{id}/preview`.
- Requires `content.view` (fail-closed via ContentAdminAccess).
- Not public `?preview=true`; not sitemap; robots noindex/nofollow/noarchive.
- Does not flip Draft to Published.
