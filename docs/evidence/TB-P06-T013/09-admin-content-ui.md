# 09 — Admin content UI (TB-P06-T013)

Task: `TB-P06-T013`

## Routes / files

| Path | File |
|---|---|
| `/admin/content` | `src/frontend/app/admin/content/page.tsx` |
| Screen | `AdminContentScreen` in `admin-screens.tsx` |
| Nav | `admin-shell.tsx` → «محتوا / بلاگ» `live: true` |

## Behavior

- Professional DataGrid of admin articles (title, slug, status, category, dates).
- Create draft form (slug/title/excerpt/body/author/category/SEO).
- Publish / Unpublish actions via `/v1/admin/content/articles/{id}/publish|unpublish`.
- Loads via `loadAdminContentArticles` / create helpers in `content-api.ts`.
- Uses existing admin session / actor prep patterns.

## Status

**LIVE** — minimum commercial CMS for articles (list + create + publish lifecycle).
