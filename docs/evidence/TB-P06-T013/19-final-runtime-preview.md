# 19 — Final runtime preview (TB-P06-T013)

Task: `TB-P06-T013`

## Tooba

| URL | Observed |
|---|---|
| `http://127.0.0.1:5088/health/live` | 200 |
| `http://127.0.0.1:5088/health/ready` | 200 |
| `GET /v1/content/articles` | 200 — Published page (seed) |
| `GET /v1/content/articles/guide-online-shopping` | 200 — Body, Locale, SeoTitle, Category present |
| `http://127.0.0.1:3000/blogs` | Live listing + featured slider |
| `http://127.0.0.1:3000/blogs/guide-online-shopping` | Detail + body |
| `http://127.0.0.1:3000/admin/content` | Admin DataGrid create/publish/unpublish |
| `http://127.0.0.1:3000/` | Home articles rail → `/blogs` links; no fake like |

## Shopeiva reference

| URL | Role |
|---|---|
| `http://127.0.0.1:3001/blogs` | Locked structure reference |

## Runtime notes

- Host Development SingleStore (`tooba_alpha`) preferred for Content seed + AdminPanelAccess tenant.
- Do not leave `Tooba__Edition=Marketplace` set in the shell when running Host unit tests.
- Keep Backend + Tooba Frontend + Shopeiva running after Result where possible.

## Captures

See `15-browser-side-by-side.md` and `captures/*.png`.
