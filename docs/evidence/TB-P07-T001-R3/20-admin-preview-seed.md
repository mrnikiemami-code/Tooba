# TB-P07-T001-R3 — Admin preview seed

## What was seeded (Development bootstrap)

Idempotent enrichment in `ProductWorkspaceDevelopmentBootstrap.EnsureAdminR3PreviewSeedAsync`:

| Item | Slug / id | Status |
| --- | --- | --- |
| Live linen shirt gallery | `workspace-live-shirt` → productId `01a030d1-4056-7000-baf1-99951569bc6b` | Published + **5** media refs (`aaaaaaaa`…`eeeeeeee`) with FA alt text |
| Draft scarf | `admin-r3-draft-scarf` → «شال پیش‌نویس R3» | Draft |
| Archived hat | `admin-r3-archived-hat` → «کلاه بایگانی R3» | Archived |

Verified live via `GET /v1/admin/products` + `GET /v1/admin/products/{id}/media` with Admin `dev-context` actor, and UI:

- Products grid (`02-products.png`): draft + archived R3 rows with localized status badges.
- Shirt workspace Media tab (`22-product-workspace-shirt.png`): gallery shows 4+ media previews; DOM alts include «نمای پشت»، «جزئیات یقه»، «جزئیات آستین»، «روی مانکن»; `brokenImages=0`.

## Notes

- Binary upload remains deferred; presentation uses deterministic SVG `GET /v1/storefront/media/{id}`.
- Order variety and AC members rely on prior commercial / AccessControl Development seeds already on the tenant.
