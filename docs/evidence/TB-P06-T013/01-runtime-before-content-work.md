# 01 — Runtime before content work (TB-P06-T013)

## Predecessor

| Field | Value |
|---|---|
| Task | TB-P06-T012 (Settlement & Payout foundation) — Architect ACCEPTED |
| Prior Content slice | TB-P05-T026-R2 home-rail foundation only |
| Branch | `main` |
| Pipeline | BRIDGE-WAKE-V1 / `tooba-main` |

## Baseline before T013

| Check | Result |
|---|---|
| Content schema | `content.articles` exists (InitialContent) |
| Public blog routes | MISSING (`/blogs`, `/blogs/[slug]` not present) |
| Admin content UI | MISSING (nav item absent / not live) |
| Domain fields | Title/Excerpt/Slug/Cover/Author/Tags/Featured/Status only — no Body/Locale/SEO/Category |
| Public HTTP | No `GET /v1/content/articles` or `/{slug}` |
| Admin HTTP | No `/v1/admin/content/*` CRUD |
| Home rail | `latestArticles` via `ListPublishedForHomeAsync` only |

## Notes

- Content was commercial-incomplete: home carousel cards without article body or SEO metadata.
- Shopeiva blog listing/detail existed as reference; Tooba had not ported those routes.
- Fake home-article heart/like interaction existed on storefront article cards prior to gap closure in this task.
