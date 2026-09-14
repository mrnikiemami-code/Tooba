# Page model

Canonical aggregate: `Tooba.Catalog.Domain.StoreLandingPage`.

| Field | Notes |
| --- | --- |
| PageId | Guid (UuidV7) |
| Store scope | Catalog DB / commerce context (no client StoreId) |
| Locale | fa \| en |
| Title / Slug | required; slug normalized |
| Status | Draft \| Published |
| TemplateKey | `default` reserved |
| SeoTitle / SeoDescription | identity only |
| CreatedAt / UpdatedAt | UTC |

No HTML/CSS/JS fields. No PageSection relation.

**Admin UI:** deferred. Backend `/v1/admin/pages*` + focused tests are sufficient. No throwaway Admin builder UI.

Publication: Create → Draft; `PUT .../status` Published/Draft. Draft public = 404.
