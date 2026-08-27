# 05 — Admin content API (TB-P06-T013)

Task: `TB-P06-T013`

Group: `/v1/admin/content` — all routes require `AdminPanelAccess.RequireAuthorizedAsync`.

## Endpoints

| Method | Path | Behavior |
|---|---|---|
| GET | `/articles` | Paged all statuses (`page`, `pageSize`) |
| GET | `/articles/{id}` | Full `AdminArticleSnapshot` or 404 |
| POST | `/articles` | Create Draft (`CreateArticleBody`) |
| PUT | `/articles/{id}` | Update editorial fields |
| POST | `/articles/{id}/publish` | Draft → Published |
| POST | `/articles/{id}/unpublish` | Published → Draft |

## Body contracts

Create/Update accept: Slug (create), Title, Excerpt, Body, CoverMediaAssetId, AuthorDisplayName, Tags, IsFeatured, PublishDate (create), Locale, SeoTitle, SeoDescription, Category.

## Auth

- Same admin gate as other `/v1/admin/*` panels (session + SpiceDB / dev bypass policy via `AdminPanelAccess`).
- Unauthorized/forbidden surfaced as platform HTTP error JSON (`title`, `errorCode`).
- Missing article on lifecycle → `content.article.missing` 404.
