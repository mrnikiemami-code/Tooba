# Runtime — TB-P10-T022-R9

- Host: `http://127.0.0.1:5088` — health `{"status":"ok"}`
- FE: `http://127.0.0.1:3000`
- Migration: `20260916093000_AddStorePageTypeAndSeo` applied via Catalog migrations on Host start
- Public list: `GET /v1/storefront/pages` returns indexable Landings
- Public resolve: `GET /v1/storefront/pages/landing-demo?locale=fa` → 200, `pageType: Landing`, SEO fields present

## Scenarios exercised

| Scenario | Result |
|----------|--------|
| Menu «صفحات فروشگاه» | PASS (screenshot) |
| Grid AppDataGrid + type/index/home cols | PASS |
| Create page type control | PASS |
| Home current indicator | PASS |
| Set as Home / restore default copy | PASS |
| SEO panel + snippet preview | PASS |
| `/landing/{slug}` Host resolve | PASS |
| `/` custom or default home | PASS (shot; selection-dependent) |
| Sitemap includes Landing, excludes NoIndex | PASS (unit + code) |
| Catalog untouched on restore | PASS (unit) |

Report: `runtime-report.json`
